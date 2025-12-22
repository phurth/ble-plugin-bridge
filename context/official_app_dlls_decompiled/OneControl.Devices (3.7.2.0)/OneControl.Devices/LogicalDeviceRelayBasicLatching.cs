using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.RelayBasic.Remote;
using OneControl.Devices.Remote;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayBasicLatching<TRelayBasicStatus, TRelayCommandFactory, TCapability> : LogicalDevice<TRelayBasicStatus, TCapability>, ILogicalDeviceLatchingRelayDirect<TRelayBasicStatus>, ILogicalDeviceLatchingRelay<TRelayBasicStatus>, ILogicalDeviceLatchingRelay, IRelayBasic, ISwitchableDevice, ILogicalDeviceSwitchableReadonly, IDevicesCommon, INotifyPropertyChanged, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, ILogicalDeviceWithStatus<TRelayBasicStatus>, ILogicalDeviceWithStatus, ILogicalDeviceWithStatusUpdate<TRelayBasicStatus>, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink, ILogicalDeviceLatchingRelayRemote<TRelayBasicStatus>, ILogicalDeviceSwitchableRemote, ILogicalDeviceSwitchable, ILogicalDeviceRemote where TRelayBasicStatus : class, ILogicalDeviceRelayBasicStatus, new()where TRelayCommandFactory : ILogicalDeviceRelayBasicCommandFactory, new()where TCapability : ILogicalDeviceRelayCapability
	{
		private const string LogTag = "LogicalDeviceRelayBasicLatching";

		public const uint CommandTimeoutMs = 5000u;

		public static ushort ShowCoachWindowFunctionNameId = ushort.MaxValue;

		protected ILogicalDeviceCommandRunnerIdsCan? IdsCanCommandRunnerDefault;

		protected RemoteCommandControl RemoteCommandControl = new RemoteCommandControl();

		protected ILogicalDeviceCommandControlRunner BasicCommandRunner = new LogicalDeviceCommandControlRunner();

		protected TRelayCommandFactory CommandFactory = new TRelayCommandFactory();

		private CancellationTokenSource _commandCancelSource = new CancellationTokenSource();

		protected uint NumberOfCommandsRunning;

		private readonly object _lockForceUseCommandStatus = new object();

		private bool _on;

		protected ILogicalDevicePidFixedPoint BatteryVoltagePidCan;

		public override bool IsLegacyDeviceHazardous => false;

		public RemoteOnline RemoteOnline { get; protected set; }

		public RemoteRelayBasic<TRelayBasicStatus> RemoteRelayBasic { get; protected set; }

		public RemoteRelayBasicFault<TRelayBasicStatus> RemoteRelayFault { get; protected set; }

		public RemoteRelayBasicUserClearRequired<TRelayBasicStatus> RemoteRelayUserClearRequired { get; protected set; }

		public RemoteRelayBasicUserMessageDtc<TRelayBasicStatus> RemoteRelayUserMessageDtc { get; set; }

		public RemoteRelayBasicPosition<TRelayBasicStatus> RemoteRelayPosition { get; protected set; }

		public RemoteRelayBasicCurrentDraw<TRelayBasicStatus> RemoteRelayCurrent { get; protected set; }

		public ILogicalDeviceRelayBasicCommandFactory CommandFactoryBasic => CommandFactory;

		public bool IsRunningCommands => NumberOfCommandsRunning != 0;

		public override string DeviceName
		{
			get
			{
				if ((ushort)base.LogicalId.FunctionName != ShowCoachWindowFunctionNameId)
				{
					return base.DeviceName;
				}
				int functionInstance = base.LogicalId.FunctionInstance;
				return "Window Shades" + ((functionInstance > 0) ? $" {functionInstance}" : "");
			}
		}

		public bool Off => !On;

		public bool Faulted
		{
			get
			{
				if (DeviceStatus.IsValid)
				{
					return DeviceStatus.IsFaulted;
				}
				return false;
			}
		}

		public bool UserClearRequired
		{
			get
			{
				if (DeviceStatus.IsValid)
				{
					return DeviceStatus.UserClearRequired;
				}
				return false;
			}
		}

		public DTC_ID UserMessageDtc
		{
			get
			{
				if (!DeviceStatus.IsValid)
				{
					return DTC_ID.UNKNOWN;
				}
				return DeviceStatus.UserMessageDtc;
			}
		}

		public bool IsValid
		{
			get
			{
				if (DeviceStatus.IsValid)
				{
					return DeviceStatus.IsValid;
				}
				return false;
			}
		}

		public bool On => _on;

		public bool IsCurrentlyOn
		{
			get
			{
				if (DeviceStatus.IsValid)
				{
					return DeviceStatus.IsOn;
				}
				return false;
			}
		}

		public virtual bool IsMasterSwitchControllable
		{
			get
			{
				if (base.DeviceCapability.AllLightsGroupBehavior != 0)
				{
					return base.DeviceCapability.AllLightsGroupBehavior == AllLightsGroupBehaviorCapability.FeatureSupportedAndEnabled;
				}
				return false;
			}
		}

		public SwitchTransition SwitchInTransition
		{
			get
			{
				if (On == DeviceStatus.IsOn || NumberOfCommandsRunning == 0)
				{
					return SwitchTransition.No;
				}
				return SwitchTransition.Transitioning;
			}
		}

		public virtual SwitchUsage UsedFor
		{
			get
			{
				switch (base.LogicalId.FunctionClass)
				{
				case FUNCTION_CLASS.WATER_HEATER:
				case FUNCTION_CLASS.TANK_HEATER:
					return SwitchUsage.Heater;
				case FUNCTION_CLASS.LIGHT:
					return SwitchUsage.Light;
				case FUNCTION_CLASS.PUMP:
					return SwitchUsage.Pump;
				case FUNCTION_CLASS.VALVE:
					return SwitchUsage.Valve;
				case FUNCTION_CLASS.VENT:
					return SwitchUsage.Vent;
				case FUNCTION_CLASS.FAN:
					return SwitchUsage.Fan;
				default:
					return SwitchUsage.Generic;
				}
			}
		}

		public override bool IsRemoteAccessAvailable => DeviceService.RemoteManager?.IsRemoteAccessAvailable(this, RemoteRelayBasic?.Channel) ?? false;

		public IRemoteChannelDefOnline RemoteOnlineChannel => RemoteOnline?.Channel;

		public virtual LogicalDeviceExScope VoltageMeasurementBatteryPidScope => LogicalDeviceExScope.Product;

		public virtual ILogicalDevicePidFixedPoint VoltageMeasurementBatteryPid => BatteryVoltagePidCan ?? (BatteryVoltagePidCan = new LogicalDevicePidFixedPoint(FixedPointType.UnsignedBigEndian16x16, this, PID.BATTERY_VOLTAGE, LogicalDeviceSessionType.None));

		public bool IsVoltagePidReadSupported => true;

		public LogicalDeviceRelayBasicLatching(ILogicalDeviceId logicalDeviceId, TCapability capability, ILogicalDeviceService? service = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, MakeNewStatus(), capability, service, isFunctionClassChangeable)
		{
			RemoteOnline = new RemoteOnline(this, RemoteChannels);
			RemoteRelayBasic = new RemoteRelayBasic<TRelayBasicStatus>(this, RemoteCommandControl, RemoteChannels);
			RemoteRelayFault = new RemoteRelayBasicFault<TRelayBasicStatus>(this, RemoteCommandControl, RemoteChannels);
			RemoteRelayUserClearRequired = new RemoteRelayBasicUserClearRequired<TRelayBasicStatus>(this, RemoteCommandControl, RemoteChannels);
			RemoteRelayUserMessageDtc = new RemoteRelayBasicUserMessageDtc<TRelayBasicStatus>(this, RemoteChannels);
			RemoteRelayPosition = new RemoteRelayBasicPosition<TRelayBasicStatus>(this, RemoteChannels);
			RemoteRelayCurrent = new RemoteRelayBasicCurrentDraw<TRelayBasicStatus>(this, RemoteChannels);
		}

		public static TRelayBasicStatus MakeNewStatus()
		{
			return new TRelayBasicStatus();
		}

		public override void OnDeviceStatusChanged()
		{
			lock (_lockForceUseCommandStatus)
			{
				if (NumberOfCommandsRunning == 0)
				{
					SyncWithStatus(DeviceStatus);
				}
			}
			OnPropertyChanged("Faulted");
			OnPropertyChanged("UserClearRequired");
			OnPropertyChanged("IsValid");
			OnPropertyChanged("On");
			OnPropertyChanged("IsCurrentlyOn");
			OnPropertyChanged("Off");
			base.OnDeviceStatusChanged();
		}

		public virtual async Task<CommandResult> SendCommandAsync(bool turnOnRelay, bool waitForCurrentStatus)
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceRelayBasicLatching", DeviceName + " Latching Relaying ExecuteCommand ignored as relay has been disposed");
				return CommandResult.ErrorOther;
			}
			if (ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				return CommandResult.ErrorDeviceOffline;
			}
			if (waitForCurrentStatus)
			{
				try
				{
					await WaitForDeviceStatusToHaveDataAsync(5000, _commandCancelSource.Token);
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
			lock (_lockForceUseCommandStatus)
			{
				ILogicalDeviceRelayBasicStatus logicalDeviceRelayBasicStatus = DeviceStatus.CopyStatus();
				logicalDeviceRelayBasicStatus.SetState(turnOnRelay);
				SyncWithStatus(logicalDeviceRelayBasicStatus);
				NumberOfCommandsRunning++;
				NotifyPropertyChanged("IsRunningCommands");
				NotifyPropertyChanged("SwitchInTransition");
			}
			CommandResult commandResult = CommandResult.ErrorOther;
			try
			{
				if (ActiveConnection == LogicalDeviceActiveConnection.Remote)
				{
					commandResult = await BasicCommandRunner.SendCommandAsync((CancellationToken cancelToken) => SendRemoteCommandAsync(turnOnRelay, cancelToken), _commandCancelSource.Token, this, CheckCommandExecution, CommandSendOption.CancelCurrentCommand);
				}
				else
				{
					ILogicalDeviceSourceDirect primaryDeviceSourceDirect = DeviceService.GetPrimaryDeviceSourceDirect(this);
					IDirectCommandSwitch directManager = primaryDeviceSourceDirect as IDirectCommandSwitch;
					if (directManager != null)
					{
						CommandResult commandResult2 = await BasicCommandRunner.SendCommandAsync((CancellationToken cancelToken) => directManager.SendDirectCommandRelayBasicSwitch(this, turnOnRelay, cancelToken), _commandCancelSource.Token, this, CheckCommandExecution, CommandSendOption.CancelCurrentCommand);
						commandResult = commandResult2;
					}
					else if (IdsCanCommandRunnerDefault != null)
					{
						ILogicalDeviceRelayBasicCommand dataPacket = (turnOnRelay ? CommandFactoryBasic.MakeRelayOnCommand() : CommandFactoryBasic.MakeRelayOffCommand());
						CommandResult commandResult2 = await IdsCanCommandRunnerDefault!.SendCommandAsync(dataPacket, _commandCancelSource.Token, CheckCommandExecution);
						commandResult = commandResult2;
					}
					else
					{
						commandResult = CommandResult.ErrorRemoteOperationNotSupported;
					}
				}
			}
			catch (Exception arg)
			{
				TaggedLog.Error("LogicalDeviceRelayBasicLatching", $"Error sending RelayBasicLatching command: {arg}");
			}
			finally
			{
				lock (_lockForceUseCommandStatus)
				{
					if (NumberOfCommandsRunning != 0)
					{
						NumberOfCommandsRunning--;
					}
					if (NumberOfCommandsRunning == 0)
					{
						SyncWithStatus(DeviceStatus);
					}
					NotifyPropertyChanged("IsRunningCommands");
					NotifyPropertyChanged("SwitchInTransition");
				}
			}
			return commandResult;
			CommandControl CheckCommandExecution(ILogicalDevice logicalDevice)
			{
				if (ActiveConnection != LogicalDeviceActiveConnection.Remote && DeviceStatus.UserClearRequired)
				{
					commandResult = CommandResult.ErrorCommandNotAllowed;
					throw new Exception(DeviceName + " Relay faulted when trying to be set");
				}
				if (DeviceStatus.IsOn == turnOnRelay)
				{
					return CommandControl.Completed;
				}
				return CommandControl.WaitAndResend;
			}
		}

		public async Task<CommandResult> SendRemoteCommandAsync(bool turnOnRelay, CancellationToken cancelToken)
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceRelayBasicLatching", DeviceName + " SendDirectCommandAsync ignored as logical device has been disposed");
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
			return await RemoteRelayBasic.SendCommandSetSwitch(turnOnRelay);
		}

		protected override void DebugUpdateDeviceStatusChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, uint dataLength, string optionalText = "")
		{
			TaggedLog.Information("LogicalDeviceRelayBasicLatching", $"Data Changed {base.LogicalId}: Relay = {DeviceStatus.IsOn}, Faults = {DeviceStatus.IsFaulted} {optionalText}");
		}

		protected void SyncWithStatus(ILogicalDeviceRelayBasicStatus status)
		{
			if (_on != status.IsOn)
			{
				_on = status.IsOn;
				OnPropertyChanged("On");
				OnPropertyChanged("Off");
			}
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
			return await SendCommandAsync(turnOnRelay: true, waitForCurrentStatus: false).ConfigureAwait(false) == CommandResult.Completed;
		}

		public async Task<bool> TurnOffAsync()
		{
			return await SendCommandAsync(turnOnRelay: false, waitForCurrentStatus: false).ConfigureAwait(false) == CommandResult.Completed;
		}

		public Task<float> ReadVoltageMeasurementAsync(CancellationToken cancellationToken)
		{
			return LogicalDeviceVoltageExtension.ReadVoltageMeasurementAsync(VoltageMeasurementBatteryPid, cancellationToken);
		}

		public override void Dispose(bool disposing)
		{
			RemoteOnline?.TryDispose();
			RemoteOnline = null;
			RemoteRelayBasic?.TryDispose();
			RemoteRelayBasic = null;
			RemoteRelayFault?.TryDispose();
			RemoteRelayFault = null;
			RemoteRelayUserClearRequired?.TryDispose();
			RemoteRelayUserClearRequired = null;
			RemoteRelayUserMessageDtc?.TryDispose();
			RemoteRelayUserMessageDtc = null;
			RemoteRelayPosition?.TryDispose();
			RemoteRelayPosition = null;
			RemoteRelayCurrent?.TryDispose();
			RemoteRelayCurrent = null;
			_commandCancelSource?.CancelAndDispose();
			_commandCancelSource = null;
			RemoteCommandControl?.TryDispose();
			RemoteCommandControl = null;
			BasicCommandRunner?.TryDispose();
			base.Dispose(disposing);
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
