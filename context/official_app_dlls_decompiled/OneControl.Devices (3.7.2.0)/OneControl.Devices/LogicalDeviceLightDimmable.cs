using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Interfaces;
using OneControl.Devices.Remote;

namespace OneControl.Devices
{
	public class LogicalDeviceLightDimmable : LogicalDevice<LogicalDeviceLightDimmableStatus, LogicalDeviceLightDimmableStatusExtended, ILogicalDeviceLightDimmableCapability>, ILogicalDeviceRemoteLightDimmableDirect, ILogicalDeviceRemoteLightDimmable, ILogicalDeviceLightDimmable, ILightDimmable, IAutoOff, ILogicalDeviceSwitchableLight, ILogicalDeviceSwitchable, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ISwitchableDevice, ILogicalDeviceSwitchableReadonly, ILogicalDeviceLight, ILogicalDeviceWithStatus, ILogicalDeviceWithStatus<LogicalDeviceLightDimmableStatus>, ILogicalDeviceWithStatusUpdate<LogicalDeviceLightDimmableStatus>, ILogicalDeviceWithStatusExtended<LogicalDeviceLightDimmableStatusExtended>, ILogicalDeviceWithStatusExtended, ILogicalDeviceLightSpeedInterval, ILogicalDeviceDimmableBrightness, IDimmableBrightness, ILogicalDeviceAutoOffSwitchable, ILogicalDeviceAutoOff, ILogicalDeviceCommandable<DimmableLightMode>, ILogicalDeviceCommandable, ICommandable<DimmableLightMode>, ILogicalDeviceCommandable<LogicalDeviceLightSpeedInterval>, ICommandable<LogicalDeviceLightSpeedInterval>, ILogicalDeviceDimmableBrightnessRemote, ILogicalDeviceRemote, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink, ILogicalDeviceRemoteLightDimmableRemote
	{
		private const string LogTag = "LogicalDeviceLightDimmable";

		protected ILogicalDeviceCommandRunnerIdsCan IdsCanCommandRunner;

		private CancellationTokenSource _commandCancelSource = new CancellationTokenSource();

		private readonly CancellationToken _commandCancelToken;

		protected uint NumberOfCommandsRunning;

		private object _lockForceUseCommandStatus = new object();

		public ILogicalDevicePidByte SimulateOnOffStyleLightPid;

		private LogicalDeviceLightDimmableStatus _shownStatus = new LogicalDeviceLightDimmableStatus();

		protected RemoteCommandControl RemoteCommandControl = new RemoteCommandControl();

		protected ILogicalDeviceCommandControlRunner BasicCommandRunner = new LogicalDeviceCommandControlRunner();

		private static readonly Dictionary<LogicalDeviceLightSpeedInterval, ushort> _lightSpeedIntervalCycleTimeDict = new Dictionary<LogicalDeviceLightSpeedInterval, ushort>
		{
			{
				LogicalDeviceLightSpeedInterval.Slow,
				2447
			},
			{
				LogicalDeviceLightSpeedInterval.Medium,
				1055
			},
			{
				LogicalDeviceLightSpeedInterval.Fast,
				220
			}
		};

		public virtual uint MSCommandTimeout => 5000u;

		public virtual uint MSSessionTimeout => 0u;

		public override bool IsLegacyDeviceHazardous => false;

		public RemoteOnline RemoteOnline { get; protected set; }

		public RemoteDimmer RemoteDimmer { get; protected set; }

		public RemoteDimmerMode RemoteDimmerMode { get; protected set; }

		public RemoteDimmerCycleTime1 RemoteDimmerCycleTime1 { get; private set; }

		public RemoteDimmerCycleTime2 RemoteDimmerCycleTime2 { get; private set; }

		public RemoteDimmerSleepMinutes RemoteDimmerCycleSleepMinutes { get; private set; }

		public DimmableLightMode Mode { get; protected set; }

		public byte Brightness { get; protected set; }

		public byte MaxBrightness { get; protected set; } = 128;


		public byte AutoOffDurationMinutes { get; protected set; } = byte.MaxValue;


		public TimeSpan? MaxAutoOffDurationMinutes
		{
			get
			{
				if (!base.DeviceCapability.IsExtendedStatusSupported)
				{
					return null;
				}
				return base.DeviceStatusExtended.SavedSleepTimer;
			}
		}

		public int CycleTime1 { get; protected set; }

		public int CycleTime2 { get; protected set; }

		public bool DimEnabled => Mode != DimmableLightMode.Off;

		public bool IsAutoOffDurationEnabled => Mode != DimmableLightMode.Off;

		public bool CycleTimeEnabled
		{
			get
			{
				if (Mode != DimmableLightMode.Blink)
				{
					return Mode == DimmableLightMode.Swell;
				}
				return true;
			}
		}

		public bool ConfiguredAsSwitchedLight => base.DeviceCapability.SimulatedOnOffStyleLight == SimulatedOnOffStyleLightCapability.Enabled;

		public bool On => Mode != DimmableLightMode.Off;

		public bool IsCurrentlyOn
		{
			get
			{
				if (DeviceStatus.HasData)
				{
					return DeviceStatus.Mode != DimmableLightMode.Off;
				}
				return false;
			}
		}

		public bool IsMasterSwitchControllable
		{
			get
			{
				if (base.DeviceCapability.AllLightsGroupBehavior != 0)
				{
					return base.DeviceCapability.AllLightsGroupBehavior == AllLightsGroupBehaviorCapability.FeatureSupportedAndEnabled;
				}
				return !IsSecurityLight;
			}
		}

		public SwitchTransition SwitchInTransition
		{
			get
			{
				if (!On == (DeviceStatus.Mode == DimmableLightMode.Off) || NumberOfCommandsRunning == 0)
				{
					return SwitchTransition.No;
				}
				return SwitchTransition.Transitioning;
			}
		}

		public SwitchUsage UsedFor => SwitchUsage.Light;

		public override bool IsRemoteAccessAvailable => DeviceService.RemoteManager?.IsRemoteAccessAvailable(this, RemoteDimmer?.Channel) ?? false;

		public IRemoteChannelDefOnline RemoteOnlineChannel => RemoteOnline?.Channel;

		public bool IsSecurityLight => base.LogicalId.FunctionName.IsSecurityLight();

		public byte DimmableBrightness
		{
			get
			{
				if (!IsDimmableBrightnessEnabled)
				{
					return 1;
				}
				return MaxBrightness;
			}
		}

		public bool IsDimmableBrightnessEnabled
		{
			get
			{
				if (ActiveConnection != 0)
				{
					return Mode != DimmableLightMode.Off;
				}
				return false;
			}
		}

		public bool SpeedIntervalEnabled
		{
			get
			{
				DimmableLightMode mode = Mode;
				if (mode <= DimmableLightMode.On || mode - 2 > DimmableLightMode.On)
				{
					return false;
				}
				return true;
			}
		}

		public LogicalDeviceLightSpeedInterval SpeedInterval
		{
			get
			{
				int cycleTime = Math.Max(CycleTime1, CycleTime2);
				ushort nearest = Enumerable.First(Enumerable.OrderBy(_lightSpeedIntervalCycleTimeDict.Values, (ushort x) => Math.Abs((long)x - (long)cycleTime)));
				return Enumerable.FirstOrDefault(_lightSpeedIntervalCycleTimeDict, (KeyValuePair<LogicalDeviceLightSpeedInterval, ushort> x) => x.Value == nearest).Key;
			}
		}

		public LogicalDeviceLightDimmable(ILogicalDeviceId logicalDeviceId, ILogicalDeviceLightDimmableCapability dimmableCapability, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceLightDimmableStatus(), new LogicalDeviceLightDimmableStatusExtended(), dimmableCapability, service, isFunctionClassChangeable)
		{
			SimulateOnOffStyleLightPid = new LogicalDevicePidByte(this, Pid.SimulateOnOffStyleLight.ConvertToPid(), LogicalDeviceSessionType.Diagnostic);
			RemoteOnline = new RemoteOnline(this, RemoteChannels);
			RemoteDimmer = new RemoteDimmer(this, RemoteCommandControl, RemoteChannels);
			RemoteDimmerMode = new RemoteDimmerMode(this, RemoteCommandControl, RemoteChannels);
			RemoteDimmerCycleTime1 = new RemoteDimmerCycleTime1(this, RemoteCommandControl, RemoteChannels);
			RemoteDimmerCycleTime2 = new RemoteDimmerCycleTime2(this, RemoteCommandControl, RemoteChannels);
			RemoteDimmerCycleSleepMinutes = new RemoteDimmerSleepMinutes(this, RemoteCommandControl, RemoteChannels);
			IdsCanCommandRunner = new LogicalDeviceCommandRunnerIdsCan(this)
			{
				CommandProcessingTime = MSCommandTimeout,
				SessionKeepAliveTime = MSSessionTimeout
			};
			_commandCancelToken = _commandCancelSource.Token;
		}

		public override void OnDeviceStatusChanged()
		{
			lock (_lockForceUseCommandStatus)
			{
				SyncWithStatus(DeviceStatus, NumberOfCommandsRunning == 0, sendChangedNotifications: false);
			}
			LightSendNotifyPropertyChangeMessages();
			base.OnDeviceStatusChanged();
		}

		private void LightSendNotifyPropertyChangeMessages()
		{
			NotifyPropertyChanged("Mode");
			NotifyPropertyChanged("On");
			NotifyPropertyChanged("IsCurrentlyOn");
			NotifyPropertyChanged("DimEnabled");
			NotifyPropertyChanged("IsAutoOffDurationEnabled");
			NotifyPropertyChanged("CycleTimeEnabled");
			NotifyPropertyChanged("IsDimmableBrightnessEnabled");
			NotifyPropertyChanged("DimmableBrightness");
			NotifyPropertyChanged("SpeedIntervalEnabled");
			NotifyPropertyChanged("SpeedInterval");
			NotifyPropertyChanged("MaxBrightness");
			NotifyPropertyChanged("AutoOffDurationMinutes");
			NotifyPropertyChanged("MaxAutoOffDurationMinutes");
			NotifyPropertyChanged("CycleTime1");
			NotifyPropertyChanged("CycleTime2");
			NotifyPropertyChanged("Brightness");
		}

		public override void UpdateDeviceOnline(bool online)
		{
			base.UpdateDeviceOnline(online);
			NotifyPropertyChanged("IsDimmableBrightnessEnabled");
			NotifyPropertyChanged("DimmableBrightness");
		}

		public override void UpdateRemoteAccessAvailable()
		{
			base.UpdateRemoteAccessAvailable();
			NotifyPropertyChanged("IsDimmableBrightnessEnabled");
			NotifyPropertyChanged("DimmableBrightness");
		}

		public override void OnDeviceCapabilityChanged()
		{
			base.OnDeviceStatusChanged();
			NotifyPropertyChanged("ConfiguredAsSwitchedLight");
		}

		protected void SyncWithStatus(LogicalDeviceLightDimmableStatus status, bool syncAll, bool sendChangedNotifications = true)
		{
			if (syncAll)
			{
				if (Mode != status.Mode)
				{
					Mode = status.Mode;
				}
				if (MaxBrightness != status.MaxBrightness)
				{
					MaxBrightness = status.MaxBrightness;
				}
				if (AutoOffDurationMinutes != status.Duration)
				{
					AutoOffDurationMinutes = status.Duration;
				}
				if (CycleTime1 != status.CycleTime1)
				{
					CycleTime1 = status.CycleTime1;
				}
				if (CycleTime2 != status.CycleTime2)
				{
					CycleTime2 = status.CycleTime2;
				}
			}
			if (Brightness != status.Brightness)
			{
				Brightness = status.Brightness;
			}
			if (sendChangedNotifications)
			{
				LightSendNotifyPropertyChangeMessages();
			}
		}

		public virtual async Task<CommandResult> SendCommandAsync(LogicalDeviceLightDimmableCommand command)
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceLightDimmable", DeviceName + " SendCommandAsync ignored as logical device has been disposed");
				return CommandResult.ErrorOther;
			}
			CommandResult commandResult = CommandResult.ErrorOther;
			try
			{
				if (ActiveConnection == LogicalDeviceActiveConnection.Remote)
				{
					commandResult = await BasicCommandRunner.SendCommandAsync((CancellationToken cancelToken) => SendRemoteCommandAsync(command, cancelToken), _commandCancelSource.Token, this, CheckCommandExecution, CommandSendOption.CancelCurrentCommand);
				}
				else
				{
					ILogicalDeviceSourceDirect primaryDeviceSourceDirect = DeviceService.GetPrimaryDeviceSourceDirect(this);
					IDirectCommandLightDimmable directManager = primaryDeviceSourceDirect as IDirectCommandLightDimmable;
					commandResult = ((directManager == null) ? (await IdsCanCommandRunner.SendCommandAsync(command, _commandCancelToken, CheckCommandExecution)) : (await BasicCommandRunner.SendCommandAsync((CancellationToken cancelToken) => directManager.SendDirectCommandLightDimmable(this, command, cancelToken), _commandCancelSource.Token, this, CheckCommandExecution, CommandSendOption.CancelCurrentCommand)));
				}
				TaggedLog.Debug("LogicalDeviceLightDimmable", $"SendCommandAsync for {command} with result {commandResult}");
			}
			finally
			{
				lock (_lockForceUseCommandStatus)
				{
					if (NumberOfCommandsRunning != 0)
					{
						NumberOfCommandsRunning--;
					}
					SyncWithStatus(DeviceStatus, NumberOfCommandsRunning == 0);
					NotifyPropertyChanged("SwitchInTransition");
				}
			}
			return commandResult;
			CommandControl CheckCommandExecution(ILogicalDevice logicalDevice)
			{
				switch (command.Command)
				{
				case DimmableLightCommand.On:
				case DimmableLightCommand.Restore:
					if (DeviceStatus.Mode == DimmableLightMode.Off)
					{
						return CommandControl.WaitAndResend;
					}
					return CommandControl.Completed;
				case DimmableLightCommand.Blink:
				case DimmableLightCommand.Swell:
					if (DeviceStatus.Mode != command.Command.ConvertToMode() || DeviceStatus.MaxBrightness != command.MaxBrightness || DeviceStatus.Duration != command.Duration || DeviceStatus.CycleTime1 != command.CycleTime1 || DeviceStatus.CycleTime2 != command.CycleTime2)
					{
						return CommandControl.WaitAndResend;
					}
					return CommandControl.Completed;
				case DimmableLightCommand.Settings:
					if (DeviceStatus.MaxBrightness != command.MaxBrightness || DeviceStatus.Duration != command.Duration)
					{
						return CommandControl.WaitAndResend;
					}
					return CommandControl.Completed;
				default:
					if (DeviceStatus.Mode != command.Command.ConvertToMode())
					{
						return CommandControl.WaitAndResend;
					}
					return CommandControl.Completed;
				}
			}
		}

		public async Task<CommandResult> SendRemoteCommandAsync(LogicalDeviceLightDimmableCommand command, CancellationToken cancelToken)
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceLightDimmable", DeviceName + " SendDirectCommandAsync ignored as logical device has been disposed");
				return CommandResult.ErrorOther;
			}
			if (cancelToken.IsCancellationRequested)
			{
				return CommandResult.Canceled;
			}
			if (!IsRemoteAccessAvailable)
			{
				return CommandResult.ErrorRemoteNotAvailable;
			}
			try
			{
				RemoteDimmer remoteDimmer = RemoteDimmer;
				if (ActiveConnection != LogicalDeviceActiveConnection.Remote || remoteDimmer == null)
				{
					return CommandResult.ErrorRemoteNotAvailable;
				}
				if (remoteDimmer.IsCommandOneShotSupported)
				{
					return await remoteDimmer.SendRemoteCommandAsync(command, cancelToken);
				}
				if (command.Command == DimmableLightCommand.Restore)
				{
					return await remoteDimmer.SendCommandDimmableOn();
				}
				if (command.Command == DimmableLightCommand.Settings)
				{
					return CommandResult.ErrorRemoteOperationNotSupported;
				}
				RemoteDimmerMode remoteDimmerMode = RemoteDimmerMode;
				if (remoteDimmerMode == null)
				{
					return CommandResult.ErrorRemoteNotAvailable;
				}
				DimmableLightMode newMode = command.Command.ConvertToMode();
				if (newMode != DeviceStatus.Mode)
				{
					switch (newMode)
					{
					case DimmableLightMode.Off:
						await remoteDimmerMode.SendDimmerModeCommand(RemoteDimmableMode.Off);
						break;
					case DimmableLightMode.On:
						await remoteDimmerMode.SendDimmerModeCommand(RemoteDimmableMode.Dimmer);
						break;
					case DimmableLightMode.Blink:
						await remoteDimmerMode.SendDimmerModeCommand(RemoteDimmableMode.Blink);
						break;
					case DimmableLightMode.Swell:
						await remoteDimmerMode.SendDimmerModeCommand(RemoteDimmableMode.Swell);
						break;
					}
				}
				if (cancelToken.IsCancellationRequested)
				{
					return CommandResult.Canceled;
				}
				if (command.MaxBrightness != DeviceStatus.MaxBrightness && newMode != 0)
				{
					float maxBrightnessPercent = (float)(int)command.MaxBrightness / 255f;
					await remoteDimmer.SendCommandDimmableMaxBrightnessPercent(maxBrightnessPercent);
				}
				if (cancelToken.IsCancellationRequested)
				{
					return CommandResult.Canceled;
				}
				if (command.Duration != DeviceStatus.Duration && RemoteDimmerCycleSleepMinutes != null)
				{
					await RemoteDimmerCycleSleepMinutes.SendCommandDuration(command.Duration);
				}
				if (cancelToken.IsCancellationRequested)
				{
					return CommandResult.Canceled;
				}
				if (newMode == DimmableLightMode.Blink || newMode == DimmableLightMode.Swell)
				{
					if (command.CycleTime1 != DeviceStatus.CycleTime1)
					{
						RemoteDimmerCycleTime1 remoteDimmerCycleTime = RemoteDimmerCycleTime1;
						if (remoteDimmerCycleTime != null)
						{
							await remoteDimmerCycleTime.SendCommandCycleTime(command.CycleTime1);
						}
					}
					if (cancelToken.IsCancellationRequested)
					{
						return CommandResult.Canceled;
					}
					if (command.CycleTime2 != DeviceStatus.CycleTime2 && RemoteDimmerCycleTime2 != null)
					{
						await RemoteDimmerCycleTime2.SendCommandCycleTime(command.CycleTime2);
					}
				}
			}
			catch (TimeoutException)
			{
				return CommandResult.ErrorCommandTimeout;
			}
			catch (OperationCanceledException)
			{
				return CommandResult.Canceled;
			}
			catch (Exception ex3)
			{
				TaggedLog.Error("LogicalDeviceLightDimmable", $"SendDirectCommandAsync failed for {this}: {ex3.Message}");
				return CommandResult.ErrorOther;
			}
			return CommandResult.Completed;
		}

		public void SendLightCommandRun(DimmableLightMode newMode, byte newOnMaxBrightness, byte newOnDuration, int newBlinkSwellCycleTime1, int newBlinkSwellCycleTime2)
		{
			SendCommandAsync(new LogicalDeviceLightDimmableCommand(newMode, newOnMaxBrightness, newOnDuration, newBlinkSwellCycleTime1, newBlinkSwellCycleTime2)).ContinueWith(delegate(Task<CommandResult> result)
			{
				TaggedLog.Debug("LogicalDeviceLightDimmable", "SendLightCommandRun " + result?.Exception?.Message);
			}, TaskContinuationOptions.OnlyOnFaulted);
		}

		public async Task<CommandResult> SendCommandOffAsync(bool waitForCurrentStatus)
		{
			if (ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				return CommandResult.ErrorDeviceOffline;
			}
			if (waitForCurrentStatus)
			{
				try
				{
					await WaitForDeviceStatusToHaveDataAsync((int)MSCommandTimeout, _commandCancelToken);
				}
				catch (TimeoutException)
				{
					return CommandResult.ErrorCommandTimeout;
				}
				catch (OperationCanceledException)
				{
					return CommandResult.Canceled;
				}
				catch
				{
					return CommandResult.ErrorOther;
				}
			}
			return await SendCommandAsync(new LogicalDeviceLightDimmableCommand(DimmableLightMode.Off, MaxBrightness, AutoOffDurationMinutes, CycleTime1, CycleTime2));
		}

		public async Task<CommandResult> SendCommandOnAsync(byte newOnBrightness, byte newOnDuration, bool waitForCurrentStatus)
		{
			if (ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				return CommandResult.ErrorDeviceOffline;
			}
			if (waitForCurrentStatus)
			{
				try
				{
					await WaitForDeviceStatusToHaveDataAsync((int)MSCommandTimeout, _commandCancelToken);
				}
				catch (TimeoutException)
				{
					return CommandResult.ErrorCommandTimeout;
				}
				catch (OperationCanceledException)
				{
					return CommandResult.Canceled;
				}
				catch
				{
					return CommandResult.ErrorOther;
				}
			}
			return await SendCommandAsync(new LogicalDeviceLightDimmableCommand(DimmableLightMode.On, newOnBrightness, newOnDuration, CycleTime1, CycleTime2));
		}

		public async Task<CommandResult> SendRestoreCommandAsync()
		{
			return await SendCommandAsync(LogicalDeviceLightDimmableCommand.MakeRestoreCommand());
		}

		public async Task<CommandResult> SendSettingsCommandAsync(byte newOnBrightness, byte newOnDuration)
		{
			if (newOnBrightness == 0)
			{
				return CommandResult.CanceledWithSameCommand;
			}
			return await SendCommandAsync(LogicalDeviceLightDimmableCommand.MakeSettingsCommand(newOnBrightness, newOnDuration));
		}

		public Task SetSimulateOnOffStyleLightAsync(SimulatedOnOffStyleLightCapability onOffStyleLightCapability, CancellationToken cancellationToken)
		{
			if (onOffStyleLightCapability == SimulatedOnOffStyleLightCapability.Enabled || onOffStyleLightCapability == SimulatedOnOffStyleLightCapability.Disabled)
			{
				return SimulateOnOffStyleLightPid.WriteAsync((byte)onOffStyleLightCapability, cancellationToken);
			}
			throw new InvalidDataException("SimulatedOnOffStyleLightCapability must be either Enabled or Disabled");
		}

		public async Task<bool> ToggleAsync(bool restore = true)
		{
			if (On)
			{
				return await TurnOffAsync().ConfigureAwait(false);
			}
			return await TurnOnAsync(restore).ConfigureAwait(false);
		}

		public async Task<bool> TurnOnAsync(bool restore = true)
		{
			try
			{
				if (restore && ConfiguredAsSwitchedLight)
				{
					TaggedLog.Debug("LogicalDeviceLightDimmable", "TurnOnAsync treating restore request as an 'on' because the dimmable light is configured to behave as a switched light.");
					restore = false;
				}
				return await (restore ? SendRestoreCommandAsync() : SendCommandOnAsync(100, 0, waitForCurrentStatus: true)) == CommandResult.Completed;
			}
			catch
			{
				return false;
			}
		}

		public async Task<bool> TurnOffAsync()
		{
			return await SendCommandOffAsync(waitForCurrentStatus: true).ConfigureAwait(false) == CommandResult.Completed;
		}

		public override void OnLogicalIdChanged()
		{
			NotifyPropertyChanged("IsSecurityLight");
			NotifyPropertyChanged("IsMasterSwitchControllable");
			base.OnLogicalIdChanged();
		}

		public override void Dispose(bool disposing)
		{
			_commandCancelSource.TryCancelAndDispose();
			_commandCancelSource = null;
			RemoteOnline?.TryDispose();
			RemoteOnline = null;
			RemoteDimmer?.TryDispose();
			RemoteDimmer = null;
			RemoteDimmerMode?.TryDispose();
			RemoteDimmerMode = null;
			RemoteDimmerCycleTime1?.TryDispose();
			RemoteDimmerCycleTime1 = null;
			RemoteDimmerCycleTime2?.TryDispose();
			RemoteDimmerCycleTime2 = null;
			RemoteDimmerCycleSleepMinutes?.TryDispose();
			RemoteDimmerCycleSleepMinutes = null;
			RemoteCommandControl?.TryDispose();
			RemoteCommandControl = null;
			IdsCanCommandRunner?.TryDispose();
			BasicCommandRunner?.TryDispose();
			base.Dispose(disposing);
		}

		public async Task<CommandResult> SendDimmableBrightnessCommandAsync(byte brightness)
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceLightDimmable", DeviceName + " SendDimmableBrightnessCommandAsync ignored as logical device has been disposed");
				return CommandResult.ErrorOther;
			}
			if (brightness == 0)
			{
				return CommandResult.CanceledWithSameCommand;
			}
			try
			{
				await WaitForDeviceStatusToHaveDataAsync((int)MSCommandTimeout, _commandCancelToken);
			}
			catch (TimeoutException)
			{
				return CommandResult.ErrorCommandTimeout;
			}
			catch (OperationCanceledException)
			{
				return CommandResult.Canceled;
			}
			catch
			{
				return CommandResult.ErrorOther;
			}
			return await SendCommandAsync(new LogicalDeviceLightDimmableCommand(Mode, brightness, AutoOffDurationMinutes, CycleTime1, CycleTime2));
		}

		public Task<CommandResult> SetAutoOffDurationAsync(byte newDuration)
		{
			return SetAutoOffDurationAsync(newDuration, forceTimerReset: true);
		}

		public async Task<CommandResult> SetAutoOffDurationAsync(byte newDuration, bool forceTimerReset)
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceLightDimmable", DeviceName + " SetAutoOffDurationAsync ignored as logical device has been disposed");
				return CommandResult.ErrorOther;
			}
			try
			{
				byte newMaxBrightness = MaxBrightness;
				if (forceTimerReset)
				{
					byte b = (((newMaxBrightness & 1) == 0) ? ((byte)1) : ((byte)0));
					newMaxBrightness = (byte)((MaxBrightness & 0xFEu) | b);
				}
				await WaitForDeviceStatusToHaveDataAsync((int)MSCommandTimeout, _commandCancelToken);
				SendLightCommandRun(Mode, newMaxBrightness, newDuration, CycleTime1, CycleTime2);
				return CommandResult.Completed;
			}
			catch (TimeoutException)
			{
				return CommandResult.ErrorCommandTimeout;
			}
			catch (OperationCanceledException)
			{
				return CommandResult.Canceled;
			}
			catch
			{
				return CommandResult.ErrorOther;
			}
		}

		public async Task<CommandResult> PerformCommand(DimmableLightMode mode)
		{
			try
			{
				int cycleTime1 = CycleTime1;
				int cycleTime2 = CycleTime2;
				if (Mode != mode && CycleTime1 != CycleTime2)
				{
					int value;
					cycleTime2 = (value = Enumerable.First(_lightSpeedIntervalCycleTimeDict).Value);
					cycleTime1 = value;
				}
				await WaitForDeviceStatusToHaveDataAsync((int)MSCommandTimeout, _commandCancelToken);
				SendLightCommandRun(mode, MaxBrightness, AutoOffDurationMinutes, cycleTime1, cycleTime2);
				return CommandResult.Completed;
			}
			catch (TimeoutException)
			{
				return CommandResult.ErrorCommandTimeout;
			}
			catch (OperationCanceledException)
			{
				return CommandResult.Canceled;
			}
			catch
			{
				return CommandResult.ErrorOther;
			}
		}

		public async Task<CommandResult> SetSpeedIntervalAsync(LogicalDeviceLightSpeedInterval speedInterval)
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceLightDimmable", DeviceName + " SetAutoOffDurationAsync ignored as logical device has been disposed");
				return CommandResult.ErrorOther;
			}
			try
			{
				ushort cycleTime = _lightSpeedIntervalCycleTimeDict.TryGetWithCustomDefaultValue(speedInterval, Enumerable.First(_lightSpeedIntervalCycleTimeDict).Value);
				await WaitForDeviceStatusToHaveDataAsync((int)MSCommandTimeout, _commandCancelToken);
				SendLightCommandRun(Mode, MaxBrightness, AutoOffDurationMinutes, cycleTime, cycleTime);
				return CommandResult.Completed;
			}
			catch (TimeoutException)
			{
				return CommandResult.ErrorCommandTimeout;
			}
			catch (OperationCanceledException)
			{
				return CommandResult.Canceled;
			}
			catch
			{
				return CommandResult.ErrorOther;
			}
		}

		public async Task<CommandResult> PerformCommand(LogicalDeviceLightSpeedInterval speedInterval)
		{
			return await SetSpeedIntervalAsync(speedInterval);
		}

		NETWORK_STATUS ILogicalDeviceIdsCan.get_LastReceivedNetworkStatus()
		{
			return base.LastReceivedNetworkStatus;
		}

		TRemoteChannelDef ILogicalDeviceRemote.GetRemoteChannelForChannelId<TRemoteChannelDef>(string channelId)
		{
			return GetRemoteChannelForChannelId<TRemoteChannelDef>(channelId);
		}
	}
}
