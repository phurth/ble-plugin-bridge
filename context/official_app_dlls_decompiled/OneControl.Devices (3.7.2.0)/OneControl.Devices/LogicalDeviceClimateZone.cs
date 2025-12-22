using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Interfaces;
using OneControl.Devices.Remote;

namespace OneControl.Devices
{
	public class LogicalDeviceClimateZone : LogicalDevice<LogicalDeviceClimateZoneStatus, ILogicalDeviceClimateZoneCapability>, ILogicalDeviceClimateZoneDirect, ILogicalDeviceClimateZone, IDevicesActivation, ILogicalDeviceWithStatus<LogicalDeviceClimateZoneStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<LogicalDeviceClimateZoneStatus>, ILogicalDeviceTemperatureMeasurementInside, ILogicalDeviceTemperatureMeasurement, ILogicalDeviceTemperatureMeasurementOutside, ILogicalDeviceCommandable<ClimateZoneHeatMode>, ILogicalDeviceCommandable, ICommandable<ClimateZoneHeatMode>, ILogicalDeviceCommandable<ClimateZoneFanMode>, ICommandable<ClimateZoneFanMode>, ILogicalDeviceCommandable<ClimateZoneHeatSource>, ICommandable<ClimateZoneHeatSource>, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink, ILogicalDeviceClimateZoneRemote, ILogicalDeviceRemote
	{
		private const string LogTag = "LogicalDeviceClimateZone";

		protected RemoteCommandControl RemoteCommandControl = new RemoteCommandControl();

		protected ILogicalDeviceCommandRunnerIdsCan IdsCanCommandRunner;

		private CancellationTokenSource _commandCancelSource = new CancellationTokenSource();

		public const int FixHeatSourceMinimumTimeMsBetweenAttempts = 4000;

		private long _fixHeatSourceLastAttemptedTimestampMs;

		private CancellationTokenSource _remoteCommandCancellationTokenSource;

		private int _remoteCommandRunningCount;

		public virtual uint MSCommandTimeout => 10000u;

		public virtual uint MSSessionTimeout => 15000u;

		public override bool IsLegacyDeviceHazardous => false;

		public RemoteOnline RemoteOnline { get; protected set; }

		public RemoteHvacTemperatureControlFahrenheit RemoteTemperatureControlHigh { get; protected set; }

		public RemoteHvacTemperatureControlFahrenheit RemoteTemperatureControlLow { get; protected set; }

		public RemoteHvacTemperatureFahrenheit RemoteTemperatureInside { get; protected set; }

		public RemoteHvacTemperatureFahrenheit RemoteTemperatureOutside { get; protected set; }

		public RemoteHvacHeatMode RemoteHeatMode { get; protected set; }

		public RemoteHvacFanMode RemoteFanMode { get; protected set; }

		public RemoteHvacHeatSource RemoteHeatSource { get; protected set; }

		public RemoteHvacZoneStatus RemoteZoneStatus { get; protected set; }

		public ClimateZoneHeatMode HeatMode => DeviceStatus.ProgramedCommand.HeatMode;

		public ClimateZoneHeatSource HeatSource => DeviceStatus.ProgramedCommand.HeatSource;

		public ClimateZoneFanMode FanMode => DeviceStatus.ProgramedCommand.FanMode;

		public bool CommandSessionActivated => IdsCanCommandRunner.CommandSessionActivated;

		public bool IsRunningCommands
		{
			get
			{
				if (!IdsCanCommandRunner.IsRunningCommands)
				{
					return _remoteCommandRunningCount > 0;
				}
				return true;
			}
		}

		public override bool IsRemoteAccessAvailable => DeviceService.RemoteManager?.IsRemoteAccessAvailable(this, RemoteOnline?.Channel) ?? false;

		public IRemoteChannelDefOnline RemoteOnlineChannel => RemoteOnline?.Channel;

		public LogicalDeviceExScope TemperatureMeasurementInsideScope { get; } = LogicalDeviceExScope.Device;


		public ITemperatureMeasurement TemperatureMeasurementInside
		{
			get
			{
				float indoorTemperatureFahrenheit = DeviceStatus.IndoorTemperatureFahrenheit;
				if (!DeviceStatus.IsIndoorTemperatureSensorValid || !DeviceStatus.HasData)
				{
					return default(TemperatureMeasurementUnknown);
				}
				return new TemperatureMeasurementFahrenheit(indoorTemperatureFahrenheit);
			}
		}

		public LogicalDeviceExScope TemperatureMeasurementOutsideScope { get; } = LogicalDeviceExScope.Product;


		public ITemperatureMeasurement TemperatureMeasurementOutside
		{
			get
			{
				float outdoorTemperatureFahrenheit = DeviceStatus.OutdoorTemperatureFahrenheit;
				if (!DeviceStatus.IsOutdoorTemperatureSensorValid || !DeviceStatus.HasData)
				{
					return default(TemperatureMeasurementUnknown);
				}
				return new TemperatureMeasurementFahrenheit(outdoorTemperatureFahrenheit);
			}
		}

		public LogicalDeviceClimateZone(ILogicalDeviceId logicalDeviceId, ILogicalDeviceClimateZoneCapability climateZoneCapability, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceClimateZoneStatus(), climateZoneCapability, service, isFunctionClassChangeable)
		{
			RemoteOnline = new RemoteOnline(this, RemoteChannels);
			RemoteTemperatureControlHigh = new RemoteHvacTemperatureControlFahrenheit(this, RemoteHvacTemperatureControlSelector.HighTripTemperatureFahrenheit, RemoteCommandControl, RemoteChannels);
			RemoteTemperatureControlLow = new RemoteHvacTemperatureControlFahrenheit(this, RemoteHvacTemperatureControlSelector.LowTripTemperatureFahrenheit, RemoteCommandControl, RemoteChannels);
			RemoteTemperatureInside = new RemoteHvacTemperatureFahrenheit(this, RemoteHvacTemperatureFahrenheitSelector.InsideTemperatureFahrenheit, RemoteChannels);
			RemoteTemperatureOutside = new RemoteHvacTemperatureFahrenheit(this, RemoteHvacTemperatureFahrenheitSelector.OutsideTemperatureFahrenheit, RemoteChannels);
			RemoteHeatMode = new RemoteHvacHeatMode(this, RemoteCommandControl, RemoteChannels);
			RemoteFanMode = new RemoteHvacFanMode(this, RemoteCommandControl, RemoteChannels);
			RemoteHeatSource = new RemoteHvacHeatSource(this, RemoteCommandControl, RemoteChannels);
			RemoteZoneStatus = new RemoteHvacZoneStatus(this, RemoteChannels);
			IdsCanCommandRunner = new LogicalDeviceCommandRunnerIdsCan(this)
			{
				CommandProcessingTime = MSCommandTimeout,
				SessionKeepAliveTime = MSSessionTimeout
			};
		}

		protected override void UpdateDeviceStatusCompleted(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, int dataLength, int matchingDataLength)
		{
			base.UpdateDeviceStatusCompleted(oldStatusData, statusData, dataLength, matchingDataLength);
			FixHeatSourceIfNeededBasedOnActiveConfiguration(forceFixNow: false);
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
			NotifyPropertyChanged("HeatMode");
			NotifyPropertyChanged("HeatSource");
			NotifyPropertyChanged("FanMode");
			TaggedLog.Information("LogicalDeviceClimateZone", $"{DeviceName} Data Changed {base.LogicalId}: Status = {DeviceStatus}");
		}

		public override void OnDeviceCapabilityChanged()
		{
			base.OnDeviceCapabilityChanged();
			FixHeatSourceIfNeededBasedOnActiveConfiguration(forceFixNow: true);
		}

		private void FixHeatSourceIfNeededBasedOnActiveConfiguration(bool forceFixNow)
		{
			if (ActiveConnection != LogicalDeviceActiveConnection.Direct && ActiveConnection != LogicalDeviceActiveConnection.Cloud)
			{
				return;
			}
			ClimateZoneHeatSource heatSource = DeviceStatus.ProgramedCommand.HeatSource;
			ClimateZoneHeatSource climateZoneHeatSource = MakeSupportedHeatSource(heatSource);
			if (climateZoneHeatSource == heatSource)
			{
				return;
			}
			long num = LogicalDeviceFreeRunningTimer.ElapsedMilliseconds - _fixHeatSourceLastAttemptedTimestampMs;
			if (forceFixNow || num >= 4000)
			{
				TaggedLog.Debug("LogicalDeviceClimateZone", $"{DeviceName} OnDeviceCapabilityChanged Fix Configuration using Command: {climateZoneHeatSource} instead of {heatSource}");
				_fixHeatSourceLastAttemptedTimestampMs = LogicalDeviceFreeRunningTimer.ElapsedMilliseconds;
				SendHeatSourceCommandAsync(climateZoneHeatSource, ClimateZoneCommandOptions.AutoAdjustToSupportedConfiguration).ContinueWith(delegate(Task<CommandResult> result)
				{
					TaggedLog.Debug("LogicalDeviceClimateZone", DeviceName + " - Error OnDeviceCapabilityChanged " + result.Exception?.Message);
				}, TaskContinuationOptions.OnlyOnFaulted);
			}
		}

		public async Task ActivateSession(CancellationToken cancelToken)
		{
			try
			{
				await IdsCanCommandRunner.CommandActivateSession(cancelToken);
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceClimateZone", $"Unable to ActiveSession for {base.LogicalId} with error ${ex.Message}\n{ex.StackTrace}");
			}
		}

		public void DeactivateSession()
		{
			IdsCanCommandRunner.CommandDeactivateSession();
		}

		public ClimateZoneHeatSource MakeSupportedHeatSource(ClimateZoneHeatSource desiredHeatSource)
		{
			switch (desiredHeatSource)
			{
			case ClimateZoneHeatSource.PreferGas:
				if (!base.DeviceCapability.IsGasHeat && base.DeviceCapability.IsHeatPump)
				{
					return ClimateZoneHeatSource.PreferHeatPump;
				}
				break;
			case ClimateZoneHeatSource.PreferHeatPump:
				if (!base.DeviceCapability.IsHeatPump && base.DeviceCapability.IsGasHeat)
				{
					return ClimateZoneHeatSource.PreferGas;
				}
				break;
			}
			return desiredHeatSource;
		}

		public async Task<CommandResult> SendCommandAsync(ClimateZoneHeatMode heatMode, ClimateZoneHeatSource heatSource, ClimateZoneFanMode fanMode, byte lowTripTemperatureFahrenheit, byte highTripTemperatureFahrenheit, ClimateZoneCommandOptions commandOptions = ClimateZoneCommandOptions.AutoAdjustToSupportedConfiguration)
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceClimateZone", DeviceName + " Generator ExecuteCommand ignored as relay has been disposed");
				return CommandResult.ErrorOther;
			}
			if (commandOptions.HasFlag(ClimateZoneCommandOptions.AutoAdjustToSupportedConfiguration))
			{
				heatSource = MakeSupportedHeatSource(heatSource);
			}
			if (ActiveConnection == LogicalDeviceActiveConnection.Remote)
			{
				return await SendRemoteCommandAsync(heatMode, heatSource, fanMode, lowTripTemperatureFahrenheit, highTripTemperatureFahrenheit);
			}
			LogicalDeviceClimateZoneCommand command = new LogicalDeviceClimateZoneCommand(heatMode, heatSource, fanMode, lowTripTemperatureFahrenheit, highTripTemperatureFahrenheit);
			TaggedLog.Debug("LogicalDeviceClimateZone", $"{DeviceName} SendCommandAsync Command: {command.Command}, low temp = {lowTripTemperatureFahrenheit}, high temp = {highTripTemperatureFahrenheit}");
			if (DeviceService.GetPrimaryDeviceSourceDirect(this) is IDirectCommandClimateZone directCommandClimateZone)
			{
				return await directCommandClimateZone.SendDirectCommandClimateZoneAsync(this, command, _commandCancelSource.Token);
			}
			return await IdsCanCommandRunner.SendCommandAsync(command, _commandCancelSource.Token, delegate
			{
				if ((byte)DeviceStatus.ProgramedCommand != (byte)command.Command)
				{
					return CommandControl.WaitAndResend;
				}
				if (DeviceStatus.LowTripTemperatureFahrenheit != lowTripTemperatureFahrenheit)
				{
					return CommandControl.WaitAndResend;
				}
				return (DeviceStatus.HighTripTemperatureFahrenheit != highTripTemperatureFahrenheit) ? CommandControl.WaitAndResend : CommandControl.Completed;
			}, CommandSendOption.CancelCurrentCommand);
		}

		protected Task<CommandResult> SendHeatSourceCommandAsync(ClimateZoneHeatSource heatSource, ClimateZoneCommandOptions commandOptions)
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceClimateZone", $"{DeviceName} HVAC SendHeatSourceCommandAsync {heatSource} ignored it has been disposed");
				return Task.FromResult(CommandResult.ErrorOther);
			}
			return SendCommandAsync(DeviceStatus.ProgramedCommand.HeatMode, heatSource, DeviceStatus.ProgramedCommand.FanMode, DeviceStatus.LowTripTemperatureFahrenheit, DeviceStatus.HighTripTemperatureFahrenheit, commandOptions);
		}

		public async Task<CommandResult> SendRemoteCommandAsync(ClimateZoneHeatMode heatMode, ClimateZoneHeatSource heatSource, ClimateZoneFanMode fanMode, byte lowTripTemperatureFahrenheit, byte highTripTemperatureFahrenheit)
		{
			if (!IsRemoteAccessAvailable || base.IsDisposed)
			{
				return CommandResult.ErrorRemoteNotAvailable;
			}
			CommandResult result = CommandResult.Completed;
			RemoteHvacHeatMode remoteHeatMode = RemoteHeatMode;
			if (ActiveConnection != LogicalDeviceActiveConnection.Remote || remoteHeatMode == null)
			{
				return CommandResult.ErrorRemoteNotAvailable;
			}
			CancellationToken cancellationToken = default(CancellationToken);
			try
			{
				lock (this)
				{
					_remoteCommandRunningCount++;
					_remoteCommandCancellationTokenSource?.TryCancel();
					_remoteCommandCancellationTokenSource = new CancellationTokenSource((int)MSCommandTimeout);
					cancellationToken = _remoteCommandCancellationTokenSource.Token;
				}
				await TaskExtension.TryDelay(500, cancellationToken);
				if (remoteHeatMode.IsCommandOneShotSupported)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						return CommandResult.Canceled;
					}
					await remoteHeatMode.SendRemoteCommandAsync(new LogicalDeviceClimateZoneCommand(heatMode, heatSource, fanMode, lowTripTemperatureFahrenheit, highTripTemperatureFahrenheit), cancellationToken);
				}
				else
				{
					if (!cancellationToken.IsCancellationRequested && heatMode != DeviceStatus.ProgramedCommand.HeatMode)
					{
						result = await RemoteHeatMode.SendHeatModeCommand(heatMode, cancellationToken);
					}
					if (result == CommandResult.Completed && !cancellationToken.IsCancellationRequested && heatSource != DeviceStatus.ProgramedCommand.HeatSource)
					{
						result = await RemoteHeatSource.SendHeatSourceCommand(heatSource, cancellationToken);
					}
					if (result == CommandResult.Completed && !cancellationToken.IsCancellationRequested && fanMode != DeviceStatus.ProgramedCommand.FanMode)
					{
						result = await RemoteFanMode.SendFanModeCommand(fanMode, cancellationToken);
					}
					if (result == CommandResult.Completed && !cancellationToken.IsCancellationRequested && lowTripTemperatureFahrenheit != DeviceStatus.LowTripTemperatureFahrenheit)
					{
						result = await RemoteTemperatureControlLow.SendSetTemperatureCommand(lowTripTemperatureFahrenheit, cancellationToken);
					}
					if (result == CommandResult.Completed && !cancellationToken.IsCancellationRequested && highTripTemperatureFahrenheit != DeviceStatus.HighTripTemperatureFahrenheit)
					{
						result = await RemoteTemperatureControlHigh.SendSetTemperatureCommand(highTripTemperatureFahrenheit, cancellationToken);
					}
				}
				while (result == CommandResult.Completed && !cancellationToken.IsCancellationRequested && (DeviceStatus.ProgramedCommand.HeatMode != heatMode || DeviceStatus.ProgramedCommand.HeatSource != heatSource || DeviceStatus.ProgramedCommand.FanMode != fanMode || DeviceStatus.LowTripTemperatureFahrenheit != lowTripTemperatureFahrenheit || DeviceStatus.HighTripTemperatureFahrenheit != highTripTemperatureFahrenheit))
				{
					await TaskExtension.TryDelay(100, cancellationToken);
				}
			}
			finally
			{
				lock (this)
				{
					if (result == CommandResult.Completed && cancellationToken.IsCancellationRequested)
					{
						result = CommandResult.ErrorCommandTimeout;
					}
					_remoteCommandCancellationTokenSource?.TryCancelAndDispose();
					if (_remoteCommandRunningCount > 0)
					{
						_remoteCommandRunningCount--;
					}
				}
			}
			return result;
		}

		public override void Dispose(bool disposing)
		{
			RemoteOnline?.TryDispose();
			RemoteOnline = null;
			RemoteTemperatureControlHigh?.TryDispose();
			RemoteTemperatureControlHigh = null;
			RemoteTemperatureControlLow?.TryDispose();
			RemoteTemperatureControlLow = null;
			RemoteTemperatureInside?.TryDispose();
			RemoteTemperatureInside = null;
			RemoteTemperatureOutside?.TryDispose();
			RemoteTemperatureOutside = null;
			RemoteHeatMode?.TryDispose();
			RemoteHeatMode = null;
			RemoteFanMode?.TryDispose();
			RemoteFanMode = null;
			RemoteHeatSource?.TryDispose();
			RemoteHeatSource = null;
			RemoteZoneStatus?.TryDispose();
			RemoteZoneStatus = null;
			_commandCancelSource?.CancelAndDispose();
			_commandCancelSource = null;
			RemoteCommandControl?.TryDispose();
			RemoteCommandControl = null;
			IdsCanCommandRunner?.TryDispose();
			base.Dispose(disposing);
		}

		public async Task<CommandResult> PerformCommand(ClimateZoneHeatMode option)
		{
			_ = 1;
			try
			{
				await WaitForDeviceStatusToHaveDataAsync((int)MSCommandTimeout, _commandCancelSource.Token);
				return await SendCommandAsync(option, DeviceStatus.ProgramedCommand.HeatSource, DeviceStatus.ProgramedCommand.FanMode, DeviceStatus.LowTripTemperatureFahrenheit, DeviceStatus.HighTripTemperatureFahrenheit);
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

		public async Task<CommandResult> PerformCommand(ClimateZoneFanMode option)
		{
			_ = 1;
			try
			{
				await WaitForDeviceStatusToHaveDataAsync((int)MSCommandTimeout, _commandCancelSource.Token);
				return await SendCommandAsync(DeviceStatus.ProgramedCommand.HeatMode, DeviceStatus.ProgramedCommand.HeatSource, option, DeviceStatus.LowTripTemperatureFahrenheit, DeviceStatus.HighTripTemperatureFahrenheit);
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

		public async Task<CommandResult> PerformCommand(ClimateZoneHeatSource option)
		{
			_ = 1;
			try
			{
				await WaitForDeviceStatusToHaveDataAsync((int)MSCommandTimeout, _commandCancelSource.Token);
				return await SendCommandAsync(DeviceStatus.ProgramedCommand.HeatMode, option, DeviceStatus.ProgramedCommand.FanMode, DeviceStatus.LowTripTemperatureFahrenheit, DeviceStatus.HighTripTemperatureFahrenheit);
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
