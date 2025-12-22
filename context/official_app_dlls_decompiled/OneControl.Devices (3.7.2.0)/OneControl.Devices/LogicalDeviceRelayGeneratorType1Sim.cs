using System;
using System.ComponentModel;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayGeneratorType1Sim : LogicalDeviceRelayHBridgeMomentaryType1Sim, ILogicalDeviceGeneratorRelay, ILogicalDeviceGenerator, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, IGenerator, IHourMeter, ILogicalDeviceSimulated
	{
		private const string RelayNotifyPropertySourceKey = "RelayPropertySource";

		private NotifyPropertyChangedProxySource _relayPropertyChangedSource;

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

		public bool IsHourMeterSupported { get; protected set; }

		public bool HasHourMeter { get; protected set; }

		public bool Error => false;

		public bool MaintenancePastDue => false;

		public bool MaintenanceDue => false;

		public bool Running
		{
			get
			{
				if (!HasHourMeter || base.RelayEnergized != RelayHBridgeEnergized.Relay2)
				{
					return false;
				}
				return true;
			}
		}

		public ulong OperatingSeconds
		{
			get
			{
				if (!HasHourMeter)
				{
					return 0uL;
				}
				return 15120uL;
			}
		}

		public ILogicalDevicePidTimeSpan HourMeterMaintenancePeriodSecPid { get; } = new LogicalDevicePidSimTimeSpan(LogicalDevicePidTimeSpanPrecision.UInt32Seconds, PID.MAINTENANCE_PERIOD_SEC, 0uL);


		public ILogicalDevicePidTimeSpan HourMeterLastMaintenanceTimePid { get; } = new LogicalDevicePidSimTimeSpan(LogicalDevicePidTimeSpanPrecision.UInt32Seconds, PID.LAST_MAINTENANCE_TIME_SEC, 0uL);


		public ILogicalDevicePidMap<string> CumminsOnanGeneratorFaultCode { get; protected set; }

		public GeneratorType GeneratorType => GeneratorType.Generic;

		[NotifyPropertyChangedProxy("RelayPropertySource", "RelayEnergized", "GeneratorCurrentState")]
		public GeneratorState GeneratorCurrentState
		{
			get
			{
				if (Running)
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

		public LogicalDeviceRelayGeneratorType1Sim(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService service, bool hasHourMeter)
			: base(logicalDeviceId, service)
		{
			IsHourMeterSupported = hasHourMeter;
			HasHourMeter = hasHourMeter;
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

		protected static string GeneratorFaultCodeMapToString(ulong code)
		{
			return "";
		}

		public override void Dispose(bool disposing)
		{
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
