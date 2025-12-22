using System;
using System.ComponentModel;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using Serilog;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayGenerator<TRelayBasicStatus, TRelayCommandFactory, TCapability> : LogicalDeviceRelayHBridgeMomentary<TRelayBasicStatus, TRelayCommandFactory, TCapability>, ILogicalDeviceGeneratorRelay, ILogicalDeviceGenerator, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, IGenerator, IHourMeter where TRelayBasicStatus : class, ILogicalDeviceRelayHBridgeStatus, new()where TRelayCommandFactory : ILogicalDeviceRelayHBridgeCommandFactory, new()where TCapability : ILogicalDeviceRelayCapability
	{
		private const string RelayNotifyPropertySourceKey = "RelayPropertySource";

		private NotifyPropertyChangedProxySource _relayPropertyChangedSource;

		private const string HourMeterNotifyPropertySourceKey = "HourMeterPropertySource";

		private IHourMeter _hourMeter;

		private NotifyPropertyChangedProxySource _hourMeterPropertyChangedSource;

		private readonly LogicalDevicePidProxyTimeSpan _hourMeterMaintenancePeriodSecPid = new LogicalDevicePidProxyTimeSpan();

		private readonly LogicalDevicePidProxyTimeSpan _hourMeterLastMaintenanceTimePid = new LogicalDevicePidProxyTimeSpan();

		public bool IsTemperatureSupported => false;

		public bool IsTemperatureSensorValid => false;

		public float TemperatureFahrenheit => FixedPointUnsignedBigEndian8X8.ToFloat(32768);

		public bool IsBatteryVoltageSupported => false;

		public float BatteryVoltage => 0f;

		public bool IsQuietHoursSupported => false;

		public bool QuietHoursActive => false;

		public ILogicalDevicePidBool GeneratorQuietHoursEnable { get; protected set; }

		public ILogicalDevicePidTimeSpan GeneratorQuietHoursStartTimePid { get; protected set; }

		public ILogicalDevicePidTimeSpan GeneratorQuietHoursEndTimePid { get; protected set; }

		public bool IsAutoRunSupported => false;

		public bool IsAutoRunOnTempSupported => false;

		public bool IsAutoRunOnTempAvailable => false;

		public ILogicalDevicePidFixedPoint GeneratorAutoStartLowVoltagePid { get; protected set; }

		public ILogicalDevicePidFixedPointBool GeneratorAutoStartHiTempCelicusPid { get; protected set; }

		public ILogicalDevicePidTimeSpan GeneratorAutoRunDurationMinutesPid { get; protected set; }

		public ILogicalDevicePidTimeSpan GeneratorAutoRunMinOffTimeMinutesPid { get; protected set; }

		public bool IsHourMeterSupported => true;

		[NotifyPropertyChangedProxy("HourMeterPropertySource", "ActiveConnection", "HasHourMeter")]
		public bool HasHourMeter
		{
			get
			{
				if (GetHourMeter() != null)
				{
					return GetHourMeter().ActiveConnection != LogicalDeviceActiveConnection.Offline;
				}
				return false;
			}
		}

		[NotifyPropertyChangedProxy("HourMeterPropertySource", "Error")]
		public bool Error => GetHourMeter()?.Error ?? false;

		[NotifyPropertyChangedProxy("HourMeterPropertySource", "MaintenancePastDue")]
		public bool MaintenancePastDue => GetHourMeter()?.MaintenancePastDue ?? false;

		[NotifyPropertyChangedProxy("HourMeterPropertySource", "MaintenanceDue")]
		public bool MaintenanceDue => GetHourMeter()?.MaintenanceDue ?? false;

		[NotifyPropertyChangedProxy("HourMeterPropertySource", "Running")]
		public bool Running => GetHourMeter()?.Running ?? false;

		[NotifyPropertyChangedProxy("HourMeterPropertySource", "OperatingSeconds")]
		public ulong OperatingSeconds => GetHourMeter()?.OperatingSeconds ?? 0;

		public ILogicalDevicePidTimeSpan HourMeterMaintenancePeriodSecPid
		{
			get
			{
				GetHourMeter();
				return _hourMeterMaintenancePeriodSecPid;
			}
		}

		public ILogicalDevicePidTimeSpan HourMeterLastMaintenanceTimePid
		{
			get
			{
				GetHourMeter();
				return _hourMeterLastMaintenanceTimePid;
			}
		}

		public ILogicalDevicePidMap<string> CumminsOnanGeneratorFaultCode { get; protected set; }

		public GeneratorType GeneratorType => GeneratorType.Generic;

		[NotifyPropertyChangedProxy("RelayPropertySource", "RelayEnergized", "GeneratorCurrentState")]
		[NotifyPropertyChangedProxy("RelayPropertySource", "ActiveConnection", "GeneratorCurrentState")]
		[NotifyPropertyChangedProxy("HourMeterPropertySource", "Running", "GeneratorCurrentState")]
		public GeneratorState GeneratorCurrentState
		{
			get
			{
				if (ActiveConnection == LogicalDeviceActiveConnection.Offline)
				{
					return GeneratorState.Offline;
				}
				IHourMeter hourMeter = GetHourMeter();
				if (hourMeter == null)
				{
					if (base.RelayEnergized != RelayHBridgeEnergized.Relay2)
					{
						return GeneratorState.Off;
					}
					return GeneratorState.Running;
				}
				if (hourMeter.Running)
				{
					if (base.RelayEnergized != RelayHBridgeEnergized.Relay1)
					{
						return GeneratorState.Running;
					}
					return GeneratorState.Stopping;
				}
				if (base.RelayEnergized != RelayHBridgeEnergized.Relay2)
				{
					return GeneratorState.Off;
				}
				return GeneratorState.Starting;
			}
		}

		public bool IsGeneratorOnePressCommandsSupported => false;

		public LogicalDeviceRelayGenerator(ILogicalDeviceId logicalDeviceId, TCapability capability, ILogicalDeviceService service = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, capability, service, isFunctionClassChangeable)
		{
			CumminsOnanGeneratorFaultCode = new LogicalDevicePidMapStub<string>(0uL, GeneratorFaultCodeMapToString, this, PID.CUMMINS_ONAN_GENERATOR_FAULT_CODE);
			_relayPropertyChangedSource = new NotifyPropertyChangedProxySource(this, base.OnPropertyChanged, this, "RelayPropertySource");
			GeneratorQuietHoursEnable = new LogicalDevicePidStub(this, PID.GENERATOR_QUIET_HOURS_ENABLED, 2uL);
			GeneratorQuietHoursStartTimePid = new LogicalDevicePidStub(this, PID.GENERATOR_QUIET_HOURS_START_TIME, 0uL);
			GeneratorQuietHoursEndTimePid = new LogicalDevicePidStub(this, PID.GENERATOR_QUIET_HOURS_END_TIME, 0uL);
			GeneratorAutoStartLowVoltagePid = new LogicalDevicePidStub(this, PID.GENERATOR_AUTO_START_LOW_VOLTAGE, 0uL);
			GeneratorAutoStartHiTempCelicusPid = new LogicalDevicePidStub(this, PID.GENERATOR_AUTO_START_HI_TEMP_C, 0uL);
			GeneratorAutoRunDurationMinutesPid = new LogicalDevicePidStub(this, PID.GENERATOR_AUTO_RUN_DURATION_MINUTES, 0uL);
			GeneratorAutoRunMinOffTimeMinutesPid = new LogicalDevicePidStub(this, PID.GENERATOR_AUTO_RUN_MIN_OFF_TIME_MINUTES, 0uL);
		}

		public IHourMeter GetHourMeter()
		{
			if (_hourMeter != null)
			{
				return _hourMeter;
			}
			_hourMeterPropertyChangedSource?.Dispose();
			_hourMeterPropertyChangedSource = null;
			_hourMeter = LogicalDeviceHourMeter.FindAssociatedHourMeter(this);
			if (_hourMeter != null)
			{
				Log.Debug("{LogicalDeviceRelayGenerator} with hour meter {_hourMeter}", this, _hourMeter);
				_hourMeterPropertyChangedSource = new NotifyPropertyChangedProxySource(_hourMeter, base.OnPropertyChanged, this, "HourMeterPropertySource");
			}
			if (_hourMeterMaintenancePeriodSecPid.DevicePid != _hourMeter?.HourMeterMaintenancePeriodSecPid)
			{
				_hourMeterMaintenancePeriodSecPid.DevicePid = _hourMeter?.HourMeterMaintenancePeriodSecPid;
				NotifyPropertyChanged("HourMeterMaintenancePeriodSecPid");
			}
			if (_hourMeterLastMaintenanceTimePid.DevicePid != _hourMeter?.HourMeterLastMaintenanceTimePid)
			{
				_hourMeterLastMaintenanceTimePid.DevicePid = _hourMeter?.HourMeterLastMaintenanceTimePid;
				NotifyPropertyChanged("HourMeterLastMaintenanceTimePid");
			}
			return _hourMeter;
		}

		protected static string GeneratorFaultCodeMapToString(ulong code)
		{
			return "";
		}

		public override void Dispose(bool disposing)
		{
			_hourMeterMaintenancePeriodSecPid.TryDispose();
			_hourMeterLastMaintenanceTimePid.TryDispose();
			try
			{
				_hourMeterPropertyChangedSource?.Dispose();
			}
			catch
			{
			}
			_hourMeterPropertyChangedSource = null;
			try
			{
				_relayPropertyChangedSource?.Dispose();
			}
			catch
			{
			}
			_relayPropertyChangedSource = null;
			base.Dispose(disposing);
		}

		public Task<CommandResult> SendGeneratorOnePressOnCommandAsync()
		{
			return Task.FromResult(CommandResult.ErrorOther);
		}

		public Task<CommandResult> SendGeneratorOnePressOffCommandAsync()
		{
			return Task.FromResult(CommandResult.ErrorOther);
		}
	}
}
