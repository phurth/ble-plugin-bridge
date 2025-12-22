using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Interfaces;
using Serilog;

namespace OneControl.Devices
{
	public class LogicalDeviceClimateZoneSim : LogicalDevice<LogicalDeviceClimateZoneStatus, ILogicalDeviceClimateZoneCapability>, ILogicalDeviceClimateZone, IDevicesActivation, ILogicalDeviceWithStatus<LogicalDeviceClimateZoneStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<LogicalDeviceClimateZoneStatus>, ILogicalDeviceTemperatureMeasurementInside, ILogicalDeviceTemperatureMeasurement, ILogicalDeviceTemperatureMeasurementOutside, ILogicalDeviceCommandable<ClimateZoneHeatMode>, ILogicalDeviceCommandable, ICommandable<ClimateZoneHeatMode>, ILogicalDeviceCommandable<ClimateZoneFanMode>, ICommandable<ClimateZoneFanMode>, ILogicalDeviceCommandable<ClimateZoneHeatSource>, ICommandable<ClimateZoneHeatSource>, ILogicalDeviceSimulated
	{
		private const string LogTag = "LogicalDeviceClimateZoneSim";

		private readonly CancellationTokenSource _simTaskCancelSource = new CancellationTokenSource();

		private readonly LogicalDeviceClimateZoneStatus _simStatus = new LogicalDeviceClimateZoneStatus();

		public override bool IsLegacyDeviceHazardous => false;

		public override LogicalDeviceActiveConnection ActiveConnection => LogicalDeviceActiveConnection.Direct;

		public override bool ActiveSession => CommandSessionActivated;

		public ClimateZoneHeatMode HeatMode => DeviceStatus.ProgramedCommand.HeatMode;

		public ClimateZoneHeatSource HeatSource => DeviceStatus.ProgramedCommand.HeatSource;

		public ClimateZoneFanMode FanMode => DeviceStatus.ProgramedCommand.FanMode;

		public bool IsRunningCommands => false;

		public bool CommandSessionActivated { get; protected set; }

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

		public LogicalDeviceClimateZoneSim(ILogicalDeviceId logicalDeviceId, LogicalDeviceClimateZoneStatus startingZoneStatus, ILogicalDeviceClimateZoneCapability climateZoneCapability, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, startingZoneStatus, climateZoneCapability, service, isFunctionClassChangeable)
		{
			LogicalDeviceClimateZoneSim logicalDeviceClimateZoneSim = this;
			_simStatus.Update(startingZoneStatus.Data, (int)startingZoneStatus.MaxSize);
			CancellationToken simTaskCancelToken = _simTaskCancelSource.Token;
			Task.Run(async delegate
			{
				while (!simTaskCancelToken.IsCancellationRequested)
				{
					logicalDeviceClimateZoneSim.UpdateDeviceStatus(logicalDeviceClimateZoneSim._simStatus.Data, logicalDeviceClimateZoneSim._simStatus.Size);
					logicalDeviceClimateZoneSim.NotifyPropertyChanged("HeatMode");
					logicalDeviceClimateZoneSim.NotifyPropertyChanged("HeatSource");
					logicalDeviceClimateZoneSim.NotifyPropertyChanged("FanMode");
					await TaskExtension.TryDelay(250, simTaskCancelToken);
				}
			}, simTaskCancelToken);
		}

		public async Task<CommandResult> SendCommandAsync(ClimateZoneHeatMode heatMode, ClimateZoneHeatSource heatSource, ClimateZoneFanMode fanMode, byte lowTripTemperatureFahrenheit, byte highTripTemperatureFahrenheit, ClimateZoneCommandOptions commandOptions = ClimateZoneCommandOptions.AutoAdjustToSupportedConfiguration)
		{
			await Task.Delay(500);
			ClimateZoneCommand commandStatus = new ClimateZoneCommand(heatMode, heatSource, fanMode);
			_simStatus.SetCommandStatus(commandStatus);
			_simStatus.SetLowTripTemperatureFahrenheit(lowTripTemperatureFahrenheit);
			_simStatus.SetHighTripTemperatureFahrenheit(highTripTemperatureFahrenheit);
			Log.Debug($"LogicalDeviceClimateZoneSim (SendCommandAsync): {_simStatus} ");
			UpdateDeviceStatus(_simStatus.Data, _simStatus.Size);
			return CommandResult.Completed;
		}

		public Task ActivateSession(CancellationToken cancelToken)
		{
			CommandSessionActivated = true;
			return Task.FromResult(0);
		}

		public void DeactivateSession()
		{
			CommandSessionActivated = false;
		}

		public override void Dispose(bool disposing)
		{
			_simTaskCancelSource.TryCancelAndDispose();
			base.Dispose(disposing);
		}

		public Task<CommandResult> PerformCommand(ClimateZoneHeatMode option)
		{
			return Task.Run(delegate
			{
				ClimateZoneCommand commandStatus = new ClimateZoneCommand(option, DeviceStatus.ProgramedCommand.HeatSource, DeviceStatus.ProgramedCommand.FanMode);
				_simStatus.SetCommandStatus(commandStatus);
				return CommandResult.Completed;
			});
		}

		public Task<CommandResult> PerformCommand(ClimateZoneFanMode option)
		{
			return Task.Run(delegate
			{
				ClimateZoneCommand commandStatus = new ClimateZoneCommand(DeviceStatus.ProgramedCommand.HeatMode, DeviceStatus.ProgramedCommand.HeatSource, option);
				_simStatus.SetCommandStatus(commandStatus);
				return CommandResult.Completed;
			});
		}

		public Task<CommandResult> PerformCommand(ClimateZoneHeatSource option)
		{
			return Task.Run(delegate
			{
				ClimateZoneCommand commandStatus = new ClimateZoneCommand(DeviceStatus.ProgramedCommand.HeatMode, option, DeviceStatus.ProgramedCommand.FanMode);
				_simStatus.SetCommandStatus(commandStatus);
				return CommandResult.Completed;
			});
		}
	}
}
