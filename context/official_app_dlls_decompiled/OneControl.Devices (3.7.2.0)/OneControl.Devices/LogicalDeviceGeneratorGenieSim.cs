using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Interfaces;
using Serilog;

namespace OneControl.Devices
{
	public sealed class LogicalDeviceGeneratorGenieSim : LogicalDevice<LogicalDeviceGeneratorGenieStatus, ILogicalDeviceGeneratorGenieCapability>, ILogicalDeviceGeneratorGenie, ILogicalDeviceGenerator, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, IGenerator, IHourMeter, ILogicalDeviceWithCapability<ILogicalDeviceGeneratorGenieCapability>, ILogicalDeviceWithStatus<LogicalDeviceGeneratorGenieStatus>, ILogicalDeviceWithStatus, ILogicalDeviceWithStatusUpdate<LogicalDeviceGeneratorGenieStatus>, ILogicalDeviceCommandable<AutoStartLowVoltageMode>, ILogicalDeviceCommandable, ICommandable<AutoStartLowVoltageMode>, ILogicalDeviceCommandable<AutoStartDurationMode>, ICommandable<AutoStartDurationMode>, ILogicalDeviceCommandable<AutoStartOffTimeMode>, ICommandable<AutoStartOffTimeMode>, ILogicalDeviceSwitchable, ISwitchableDevice, ILogicalDeviceSwitchableReadonly, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceSimulated
	{
		private const string LogTag = "LogicalDeviceGeneratorGenieSim";

		private const int operatingIncrementingSeconds = 60;

		private const int timerCallbackPeriod = 60000;

		private bool generatorRunning;

		private Timer operatingSecondsSimulationTimer;

		public override LogicalDeviceActiveConnection ActiveConnection => LogicalDeviceActiveConnection.Direct;

		public override bool ActiveSession => CommandSessionActivated;

		public bool IsTemperatureSupported => true;

		public bool IsTemperatureSensorValid => DeviceStatus.IsTemperatureSensorValid;

		public float TemperatureFahrenheit => DeviceStatus.TemperatureFahrenheit;

		public bool IsBatteryVoltageSupported => true;

		public float BatteryVoltage => DeviceStatus.BatteryVoltage;

		public bool IsVoltagePidReadSupported => true;

		public bool IsQuietHoursSupported => true;

		public bool QuietHoursActive => false;

		public ILogicalDevicePidBool GeneratorQuietHoursEnable { get; }

		public ILogicalDevicePidTimeSpan GeneratorQuietHoursStartTimePid { get; }

		public ILogicalDevicePidTimeSpan GeneratorQuietHoursEndTimePid { get; }

		public bool IsAutoRunSupported => true;

		public bool IsAutoRunOnTempSupported => true;

		public bool IsAutoRunOnTempAvailable
		{
			get
			{
				if (!IsAutoRunOnTempSupported)
				{
					return false;
				}
				ILogicalDeviceManager deviceManager = DeviceService.DeviceManager;
				if (deviceManager == null)
				{
					return false;
				}
				if (deviceManager.FindLogicalDevices((ILogicalDeviceClimateZone logicalDevice) => (logicalDevice.LogicalId.FunctionName != FUNCTION_NAME.UNKNOWN && logicalDevice.ActiveConnection == LogicalDeviceActiveConnection.Direct) ? true : false).Count <= 0)
				{
					return false;
				}
				return IsAutoRunOnTempSupported;
			}
		}

		public ILogicalDevicePidFixedPoint GeneratorAutoStartLowVoltagePid { get; }

		public ILogicalDevicePidFixedPointBool GeneratorAutoStartHiTempCelicusPid { get; }

		public ILogicalDevicePidTimeSpan GeneratorAutoRunDurationMinutesPid { get; }

		public ILogicalDevicePidTimeSpan GeneratorAutoRunMinOffTimeMinutesPid { get; }

		public ILogicalDevicePidMap<string> CumminsOnanGeneratorFaultCode { get; }

		public GeneratorType GeneratorType => GeneratorType.Generic;

		public bool HasHourMeter => true;

		public bool Error => false;

		public bool MaintenancePastDue => false;

		public bool MaintenanceDue => false;

		public bool Running => DeviceStatus.State == GeneratorState.Running;

		public ulong OperatingSeconds { get; set; } = 9000uL;


		public ILogicalDevicePidTimeSpan HourMeterMaintenancePeriodSecPid { get; } = new LogicalDevicePidSimTimeSpan(LogicalDevicePidTimeSpanPrecision.UInt32Seconds, PID.MAINTENANCE_PERIOD_SEC, 0uL);


		public ILogicalDevicePidTimeSpan HourMeterLastMaintenanceTimePid { get; } = new LogicalDevicePidSimTimeSpan(LogicalDevicePidTimeSpanPrecision.UInt32Seconds, PID.LAST_MAINTENANCE_TIME_SEC, 0uL);


		public bool IsHourMeterSupported => true;

		public GeneratorState GeneratorCurrentState => DeviceStatus.State;

		public bool IsGeneratorOnePressCommandsSupported => true;

		public LogicalDeviceExScope VoltageMeasurementBatteryPidScope => LogicalDeviceExScope.Product;

		public ILogicalDevicePidFixedPoint VoltageMeasurementBatteryPid { get; } = new LogicalDevicePidSimFixedPoint(FixedPointType.UnsignedBigEndian16x16, PID.BATTERY_VOLTAGE, 13f);


		public bool CommandSessionActivated { get; private set; }

		public bool On => GeneratorCurrentState != GeneratorState.Off;

		public bool IsCurrentlyOn
		{
			get
			{
				if (DeviceStatus.HasData)
				{
					return DeviceStatus.State != GeneratorState.Off;
				}
				return false;
			}
		}

		public bool IsMasterSwitchControllable => false;

		public SwitchTransition SwitchInTransition => SwitchTransition.Unknown;

		public SwitchUsage UsedFor => SwitchUsage.Generator;

		private static string GeneratorFaultCodeMapToString(ulong code)
		{
			return "";
		}

		public LogicalDeviceGeneratorGenieSim(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService service)
			: base(logicalDeviceId, new LogicalDeviceGeneratorGenieStatus(), (ILogicalDeviceGeneratorGenieCapability)new LogicalDeviceGeneratorGenieCapability(ClimateZoneCapabilityFlag.GasFurnace | ClimateZoneCapabilityFlag.AirConditioner | ClimateZoneCapabilityFlag.MultiSpeedFan), service, isFunctionClassChangeable: false)
		{
			CumminsOnanGeneratorFaultCode = new LogicalDevicePidMapStub<string>(0uL, GeneratorFaultCodeMapToString, this, PID.CUMMINS_ONAN_GENERATOR_FAULT_CODE);
			GeneratorQuietHoursEnable = new LogicalDevicePidSimBool(PID.GENERATOR_QUIET_HOURS_ENABLED);
			GeneratorQuietHoursStartTimePid = new LogicalDevicePidSimTimeSpan(LogicalDevicePidTimeSpanPrecision.OneDayOfMinutes, PID.GENERATOR_QUIET_HOURS_START_TIME, 0uL);
			GeneratorQuietHoursEndTimePid = new LogicalDevicePidSimTimeSpan(LogicalDevicePidTimeSpanPrecision.OneDayOfMinutes, PID.GENERATOR_QUIET_HOURS_END_TIME, 0uL);
			GeneratorAutoStartLowVoltagePid = new LogicalDevicePidSimFixedPoint(FixedPointType.SignedBigEndian16x16, PID.GENERATOR_AUTO_START_LOW_VOLTAGE, 0uL);
			GeneratorAutoStartHiTempCelicusPid = new LogicalDevicePidSimFixedPointBool(FixedPointType.UnsignedBigEndian16x16, PID.GENERATOR_AUTO_START_HI_TEMP_C);
			GeneratorAutoRunDurationMinutesPid = new LogicalDevicePidSimTimeSpan(LogicalDevicePidTimeSpanPrecision.UInt16Minutes, PID.GENERATOR_AUTO_RUN_DURATION_MINUTES, 0uL);
			GeneratorAutoRunMinOffTimeMinutesPid = new LogicalDevicePidSimTimeSpan(LogicalDevicePidTimeSpanPrecision.UInt16Minutes, PID.GENERATOR_AUTO_RUN_MIN_OFF_TIME_MINUTES, 0uL);
			DeviceStatus.SetBatteryVoltage(12.2f);
			DeviceStatus.SetTemperature(71.5f);
			DeviceStatus.SetState(GeneratorState.Off);
			operatingSecondsSimulationTimer = new Timer(delegate
			{
				if (generatorRunning)
				{
					OperatingSeconds += 60uL;
					OnDeviceStatusChanged();
				}
			}, null, 0, 60000);
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
			Log.Information($"Data Changed {base.LogicalId}: Generator = {DeviceStatus.State}");
			OnPropertyChanged("Running");
			OnPropertyChanged("GeneratorCurrentState");
			OnPropertyChanged("IsTemperatureSupported");
			OnPropertyChanged("IsTemperatureSensorValid");
			OnPropertyChanged("TemperatureFahrenheit");
			OnPropertyChanged("IsBatteryVoltageSupported");
			OnPropertyChanged("BatteryVoltage");
			OnPropertyChanged("OperatingSeconds");
			OnPropertyChanged("On");
			OnPropertyChanged("IsCurrentlyOn");
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			operatingSecondsSimulationTimer?.TryDispose();
			operatingSecondsSimulationTimer = null;
		}

		private void SetGeneratorState(GeneratorState state)
		{
			DeviceStatus.SetState(state);
			OnDeviceStatusChanged();
			if (state == GeneratorState.Running)
			{
				generatorRunning = true;
			}
			else
			{
				generatorRunning = false;
			}
		}

		public async Task<CommandResult> SendGeneratorOnePressOnCommandAsync()
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceGeneratorGenieSim", DeviceName + " Generator ExecuteCommand ignored as relay has been disposed");
				return CommandResult.ErrorOther;
			}
			if (GeneratorCurrentState != 0)
			{
				return CommandResult.ErrorOther;
			}
			SetGeneratorState(GeneratorState.Priming);
			await Task.Delay(2000);
			if (GeneratorCurrentState != 0)
			{
				SetGeneratorState(GeneratorState.Starting);
				await Task.Delay(500);
			}
			if (GeneratorCurrentState != 0)
			{
				SetGeneratorState(GeneratorState.Running);
			}
			return CommandResult.Completed;
		}

		public async Task<CommandResult> SendGeneratorOnePressOffCommandAsync()
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceGeneratorGenieSim", DeviceName + " Generator ExecuteCommand ignored as relay has been disposed");
				return CommandResult.ErrorOther;
			}
			if (GeneratorCurrentState == GeneratorState.Off)
			{
				return CommandResult.Completed;
			}
			SetGeneratorState(GeneratorState.Stopping);
			await Task.Delay(1000);
			SetGeneratorState(GeneratorState.Off);
			return CommandResult.Completed;
		}

		public Task<float> ReadVoltageMeasurementAsync(CancellationToken cancellationToken)
		{
			return LogicalDeviceVoltageExtension.ReadVoltageMeasurementAsync(VoltageMeasurementBatteryPid, cancellationToken);
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

		public async Task<CommandResult> PerformCommand(AutoStartLowVoltageMode option)
		{
			await GeneratorAutoStartLowVoltagePid.WriteFloatAsync(option, CancellationToken.None);
			return CommandResult.Completed;
		}

		public async Task<CommandResult> PerformCommand(AutoStartDurationMode option)
		{
			await GeneratorAutoRunDurationMinutesPid.WriteAsync(TimeSpan.FromMinutes((int)option), CancellationToken.None);
			return CommandResult.Completed;
		}

		public async Task<CommandResult> PerformCommand(AutoStartOffTimeMode option)
		{
			await GeneratorAutoRunMinOffTimeMinutesPid.WriteAsync(TimeSpan.FromMinutes((int)option), CancellationToken.None);
			return CommandResult.Completed;
		}

		public Task<bool> ToggleAsync(bool restore)
		{
			if (On)
			{
				return TurnOffAsync();
			}
			return TurnOnAsync(restore);
		}

		public Task<bool> TurnOnAsync(bool restore)
		{
			return SendGeneratorOnePressOnCommandAsync().ContinueWith((Task<CommandResult> t) => t.Result == CommandResult.Completed);
		}

		public Task<bool> TurnOffAsync()
		{
			return SendGeneratorOnePressOffCommandAsync().ContinueWith((Task<CommandResult> t) => t.Result == CommandResult.Completed);
		}
	}
}
