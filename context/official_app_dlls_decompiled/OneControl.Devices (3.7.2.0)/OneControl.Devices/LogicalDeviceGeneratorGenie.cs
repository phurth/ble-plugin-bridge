using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Interfaces;
using OneControl.Devices.Remote;
using Serilog;

namespace OneControl.Devices
{
	public class LogicalDeviceGeneratorGenie : LogicalDevice<LogicalDeviceGeneratorGenieStatus, ILogicalDeviceGeneratorGenieCapability>, ILogicalDeviceGeneratorGenieDirect, ILogicalDeviceGeneratorGenie, ILogicalDeviceGenerator, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, IGenerator, IHourMeter, ILogicalDeviceWithCapability<ILogicalDeviceGeneratorGenieCapability>, ILogicalDeviceWithStatus<LogicalDeviceGeneratorGenieStatus>, ILogicalDeviceWithStatus, ILogicalDeviceWithStatusUpdate<LogicalDeviceGeneratorGenieStatus>, ILogicalDeviceCommandable<AutoStartLowVoltageMode>, ILogicalDeviceCommandable, ICommandable<AutoStartLowVoltageMode>, ILogicalDeviceCommandable<AutoStartDurationMode>, ICommandable<AutoStartDurationMode>, ILogicalDeviceCommandable<AutoStartOffTimeMode>, ICommandable<AutoStartOffTimeMode>, ILogicalDeviceSwitchable, ISwitchableDevice, ILogicalDeviceSwitchableReadonly, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink, ILogicalDeviceGeneratorGenieRemote, ILogicalDeviceRemote, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement
	{
		private const string LogTag = "LogicalDeviceGeneratorGenie";

		protected RemoteCommandControl RemoteCommandControl = new RemoteCommandControl();

		private CancellationTokenSource _commandCancelSource = new CancellationTokenSource();

		public const ulong GeneratorAutoStartHiTempCelsiusFixedPointValueBitmask = 4294967295uL;

		public const ulong GeneratorAutoStartHiTempCelsiusDisabled = 2147483648uL;

		public const float GeneratorAutoStartHiTempCelsiusDefaultDifferental = 3f;

		private const uint DebugUpdateDeviceStatusChangedThrottleTimeMs = 20000u;

		private readonly Stopwatch _debugUpdateDeviceStatusChangedThrottleTimer = Stopwatch.StartNew();

		private uint _debugUpdateDeviceStatusChangedThrottleCount;

		private const string HourMeterNotifyPropertySourceKey = "HourMeterPropertySource";

		private IHourMeter _hourMeter;

		private NotifyPropertyChangedProxySource _hourMeterPropertyChangedSource;

		private ILogicalDevicePidFixedPoint _batteryVoltagePidCan;

		public RemoteOnline RemoteOnline { get; protected set; }

		public RemoteGeneratorState RemoteState { get; protected set; }

		public RemoteGeneratorOnePressSwitch RemoteOnePressSwitch { get; protected set; }

		public RemoteGeneratorTemperatureFahrenheit RemoteTemperature { get; protected set; }

		public RemoteGeneratorVoltage RemoteVoltage { get; protected set; }

		public RemoteGeneratorQuietHoursActive RemoteQuietHoursActive { get; protected set; }

		public bool IsTemperatureSupported => DeviceStatus.IsTemperatureSupported;

		public bool IsTemperatureSensorValid => DeviceStatus.IsTemperatureSensorValid;

		public float TemperatureFahrenheit => DeviceStatus.TemperatureFahrenheit;

		public bool IsBatteryVoltageSupported => true;

		public float BatteryVoltage => DeviceStatus.BatteryVoltage;

		public bool IsVoltagePidReadSupported => true;

		public bool IsQuietHoursSupported => true;

		public bool QuietHoursActive => DeviceStatus.QuietHoursActive;

		public ILogicalDevicePidBool GeneratorQuietHoursEnable { get; protected set; }

		public ILogicalDevicePidTimeSpan GeneratorQuietHoursStartTimePid { get; protected set; }

		public ILogicalDevicePidTimeSpan GeneratorQuietHoursEndTimePid { get; protected set; }

		public bool IsAutoRunSupported => true;

		public bool IsAutoRunOnTempSupported => base.DeviceCapability.IsAutoStartOnTempDifferentalSupported;

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
				if (deviceManager.FindLogicalDevices((ILogicalDeviceClimateZone logicalDevice) => (logicalDevice.LogicalId.FunctionName != FUNCTION_NAME.UNKNOWN && (logicalDevice.ActiveConnection == LogicalDeviceActiveConnection.Direct || logicalDevice.ActiveConnection == LogicalDeviceActiveConnection.Cloud)) ? true : false).Count <= 0)
				{
					return false;
				}
				return IsAutoRunOnTempSupported;
			}
		}

		public ILogicalDevicePidFixedPoint GeneratorAutoStartLowVoltagePid { get; protected set; }

		public ILogicalDevicePidTimeSpan GeneratorAutoRunDurationMinutesPid { get; protected set; }

		public ILogicalDevicePidTimeSpan GeneratorAutoRunMinOffTimeMinutesPid { get; protected set; }

		public ILogicalDevicePidFixedPointBool GeneratorAutoStartHiTempCelicusPid { get; protected set; }

		public ILogicalDevicePidTimeSpan HourMeterLastMaintenanceTimePid { get; }

		public ILogicalDevicePidTimeSpan HourMeterMaintenancePeriodSecPid { get; }

		public ILogicalDevicePidMap<string> CumminsOnanGeneratorFaultCode
		{
			get
			{
				if (base.DeviceCapability.GeneratorType != GeneratorType.CumminsOnan)
				{
					return CumminsOnanGeneratorFaultCodeStub;
				}
				return CumminsOnanGeneratorFaultCodeReal;
			}
		}

		public ILogicalDevicePidMap<string> CumminsOnanGeneratorFaultCodeReal { get; protected set; }

		public ILogicalDevicePidMap<string> CumminsOnanGeneratorFaultCodeStub { get; protected set; }

		public GeneratorType GeneratorType => base.DeviceCapability.GeneratorType;

		[NotifyPropertyChangedProxy("HourMeterPropertySource", "ActiveConnection", "HasHourMeter")]
		public bool HasHourMeter => (GetHourMeter()?.ActiveConnection ?? LogicalDeviceActiveConnection.Offline) != LogicalDeviceActiveConnection.Offline;

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

		public bool IsHourMeterSupported => true;

		public GeneratorState GeneratorCurrentState
		{
			get
			{
				if (ActiveConnection == LogicalDeviceActiveConnection.Offline)
				{
					return GeneratorState.Offline;
				}
				return DeviceStatus.State;
			}
		}

		public bool IsGeneratorOnePressCommandsSupported => true;

		public LogicalDeviceExScope VoltageMeasurementBatteryPidScope => LogicalDeviceExScope.Product;

		public virtual ILogicalDevicePidFixedPoint VoltageMeasurementBatteryPid => _batteryVoltagePidCan ?? (_batteryVoltagePidCan = new LogicalDevicePidFixedPoint(FixedPointType.UnsignedBigEndian16x16, this, PID.BATTERY_VOLTAGE, LogicalDeviceSessionType.None));

		public override bool IsRemoteAccessAvailable => DeviceService.RemoteManager?.IsRemoteAccessAvailable(this, RemoteOnline?.Channel) ?? false;

		public IRemoteChannelDefOnline RemoteOnlineChannel => RemoteOnline?.Channel;

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

		protected string GeneratorFaultCodeMapToString(ulong code)
		{
			if (base.DeviceCapability.GeneratorType == GeneratorType.CumminsOnan)
			{
				if (code <= 58)
				{
					switch (code)
					{
					case 2uL:
						return "Oil pressure cutoff switch did not open. Add oil if low, drain oil if high.";
					case 4uL:
						return "Cranking exceeded 30 seconds without engine starting. Check fuel, LPG, spark plug, air filter, fuel fittings, replace oil.";
					}
					ulong num = code - 12;
					if (num <= 46)
					{
						switch (num)
						{
						case 0uL:
							return "Generator is unable to maintain rated voltage. See dealer for service.";
						case 1uL:
							return "Generator is unable to maintain rated voltage. Reduce load to resolve.";
						case 2uL:
							return "Engine governor is unable to maintain rated frequency. See dealer for service.";
						case 3uL:
							return "Engine governor is unable to maintain rated frequency. Reduce load to resolve.";
						case 7uL:
							return "Sensed an open or shorted circuit. See dealer for service.";
						case 10uL:
							return "Duration of operation is near full - duty cycle beyond design limit. Reduce load, check air filter, and check exhaust system.";
						case 15uL:
							return "Unable to sense output voltage. See dealer for service.";
						case 17uL:
							return "Voltage across the battery system is greater than 19V. Check battery.";
						case 19uL:
							return "Engine Speed is > 3400 RPM. See dealer for service.";
						case 20uL:
							return "Cranking speed is < 180RPM for more than 2 seconds.  Check battery and replace engine oil.";
						case 23uL:
							return "EEPROM error occurred during self test. See dealer for service.";
						case 24uL:
							return "Engine stopped without receiving a command.  Check fuel, LPG, Spark Plugs, Air Filter, Mechanical Damage.";
						case 25uL:
							return "The Frequency / RPM ratio is wrong. See dealer for service.";
						case 26uL:
							return "Low power factor loads  Reduce load and check appliances";
						case 29uL:
							return "Unable to sense field or output voltage. See dealer for service.";
						case 30uL:
							return "ROM error occurred during self - test. See dealer for service.";
						case 31uL:
							return "RAM error occurred during self - test. See dealer for service.";
						case 33uL:
							return "Unable to sense quadrature frequency. See dealer for service.";
						case 35uL:
							return "Unable to sense ignition. See dealer for service.";
						case 36uL:
							return "Unable to sense field voltage. See dealer for service.";
						case 39uL:
							return "General Microprocessor malfunction. See dealer for service.";
						case 40uL:
							return "Open or short circuit in the fuel injector. See dealer for service.";
						case 42uL:
							return "Open or short circuit in the MAT sender. See dealer for service.";
						case 44uL:
							return "Open or short circuit in the MAP sender. See dealer for service.";
						case 45uL:
							return "Priming exceeded 3 minutes  Check control switch";
						case 46uL:
							return "Exhaust Gas Temperature(EGT) reached 650C(1202F) for 2 seconds when the generator was running. See dealer for service.";
						}
					}
				}
				else
				{
					switch (code)
					{
					case 81uL:
						return "EGT fell below 60C(140F) for 2 minutes when the generator was running or the EGT is not above 60C(140F) within 2 minutes of starting. See dealer for service.";
					case 82uL:
						return "EGT reads 1000C(1832F) for 1 second when the generator was running or during startup. See dealer for service.";
					case 65534uL:
						return "Generator circuit breaker is OFF, or tripped due to short circuit or overload. Check breaker";
					case 65535uL:
						return "Generator is operating in Engine Run Only(ERO) mode due to a faulty generator. See dealer for service.";
					}
				}
				return "";
			}
			return "";
		}

		public LogicalDeviceGeneratorGenie(ILogicalDeviceId logicalDeviceId, ILogicalDeviceGeneratorGenieCapability generatorGenieCapability, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceGeneratorGenieStatus(), generatorGenieCapability, service, isFunctionClassChangeable)
		{
			HourMeterLastMaintenanceTimePid = new LogicalDevicePidTimeSpan(LogicalDevicePidTimeSpanPrecision.UInt32Seconds, this, PID.LAST_MAINTENANCE_TIME_SEC, LogicalDeviceSessionType.Diagnostic);
			HourMeterMaintenancePeriodSecPid = new LogicalDevicePidTimeSpan(LogicalDevicePidTimeSpanPrecision.UInt32Seconds, this, PID.MAINTENANCE_PERIOD_SEC, LogicalDeviceSessionType.Diagnostic);
			RemoteOnline = new RemoteOnline(this, RemoteChannels);
			RemoteState = new RemoteGeneratorState(this, RemoteChannels);
			RemoteOnePressSwitch = new RemoteGeneratorOnePressSwitch(this, RemoteCommandControl, RemoteChannels);
			RemoteTemperature = new RemoteGeneratorTemperatureFahrenheit(this, RemoteChannels);
			RemoteVoltage = new RemoteGeneratorVoltage(this, RemoteChannels);
			RemoteQuietHoursActive = new RemoteGeneratorQuietHoursActive(this, RemoteChannels);
			CumminsOnanGeneratorFaultCodeReal = new LogicalDevicePidMap<string>(GeneratorFaultCodeMapToString, this, PID.CUMMINS_ONAN_GENERATOR_FAULT_CODE, LogicalDeviceSessionType.None);
			CumminsOnanGeneratorFaultCodeStub = new LogicalDevicePidMapStub<string>(0uL, GeneratorFaultCodeMapToString, this, PID.CUMMINS_ONAN_GENERATOR_FAULT_CODE);
			GeneratorQuietHoursEnable = new LogicalDevicePidBool(this, PID.GENERATOR_QUIET_HOURS_ENABLED, LogicalDeviceSessionType.Diagnostic);
			GeneratorQuietHoursStartTimePid = new LogicalDevicePidTimeSpan(LogicalDevicePidTimeSpanPrecision.OneDayOfMinutes, this, PID.GENERATOR_QUIET_HOURS_START_TIME, LogicalDeviceSessionType.Diagnostic);
			GeneratorQuietHoursEndTimePid = new LogicalDevicePidTimeSpan(LogicalDevicePidTimeSpanPrecision.OneDayOfMinutes, this, PID.GENERATOR_QUIET_HOURS_END_TIME, LogicalDeviceSessionType.Diagnostic);
			GeneratorAutoStartLowVoltagePid = new LogicalDevicePidFixedPoint(FixedPointType.UnsignedBigEndian16x16, this, PID.GENERATOR_AUTO_START_LOW_VOLTAGE, LogicalDeviceSessionType.Diagnostic);
			GeneratorAutoRunDurationMinutesPid = new LogicalDevicePidTimeSpan(LogicalDevicePidTimeSpanPrecision.UInt16Minutes, this, PID.GENERATOR_AUTO_RUN_DURATION_MINUTES, LogicalDeviceSessionType.Diagnostic);
			GeneratorAutoRunMinOffTimeMinutesPid = new LogicalDevicePidTimeSpan(LogicalDevicePidTimeSpanPrecision.UInt16Minutes, this, PID.GENERATOR_AUTO_RUN_MIN_OFF_TIME_MINUTES, LogicalDeviceSessionType.Diagnostic);
			GeneratorAutoStartHiTempCelicusPid = new LogicalDevicePidFixedPointBool(FixedPointType.UnsignedBigEndian16x16, this, PID.GENERATOR_AUTO_START_HI_TEMP_C, LogicalDeviceSessionType.Diagnostic, delegate(FixedPointType fpConversion, ulong ulongValue)
			{
				ulong num2 = ulongValue & 0xFFFFFFFFu;
				bool flag = ((num2 != 0L && num2 != 2147483648u) ? true : false);
				Log.Debug($"PID Value of '{num2:x}' converted to {flag}");
				return flag;
			}, delegate(FixedPointType fpConversion, bool boolValue)
			{
				ulong num = (boolValue ? fpConversion.ToFixedPointAsULong(3f) : 2147483648u);
				Log.Debug($"PID Value of {boolValue} converted to '{num:x}' ");
				return num;
			});
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
			OnPropertyChanged("GeneratorCurrentState");
			OnPropertyChanged("IsTemperatureSupported");
			OnPropertyChanged("IsTemperatureSensorValid");
			OnPropertyChanged("TemperatureFahrenheit");
			OnPropertyChanged("IsBatteryVoltageSupported");
			OnPropertyChanged("BatteryVoltage");
			OnPropertyChanged("QuietHoursActive");
			OnPropertyChanged("On");
			OnPropertyChanged("IsCurrentlyOn");
		}

		protected override void DebugUpdateDeviceStatusChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, uint dataLength, string optionalText = "")
		{
			if (_debugUpdateDeviceStatusChangedThrottleCount != 0 && _debugUpdateDeviceStatusChangedThrottleTimer.ElapsedMilliseconds < 20000)
			{
				_debugUpdateDeviceStatusChangedThrottleCount++;
				return;
			}
			base.DebugUpdateDeviceStatusChanged(oldStatusData, statusData, dataLength, $" found {_debugUpdateDeviceStatusChangedThrottleCount} changes over {20000u}ms");
			_debugUpdateDeviceStatusChangedThrottleTimer.Restart();
			_debugUpdateDeviceStatusChangedThrottleCount = 1u;
		}

		public override void OnDeviceOnlineChanged()
		{
			base.OnDeviceOnlineChanged();
			OnPropertyChanged("GeneratorCurrentState");
			OnPropertyChanged("IsAutoRunOnTempAvailable");
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
				Log.Debug($"{this} with hour meter {_hourMeter}");
				_hourMeterPropertyChangedSource = new NotifyPropertyChangedProxySource(_hourMeter, base.OnPropertyChanged, this, "HourMeterPropertySource");
			}
			return _hourMeter;
		}

		public override void Dispose(bool disposing)
		{
			RemoteOnline?.TryDispose();
			RemoteOnline = null;
			RemoteState?.TryDispose();
			RemoteState = null;
			RemoteOnePressSwitch?.TryDispose();
			RemoteOnePressSwitch = null;
			RemoteTemperature?.TryDispose();
			RemoteTemperature = null;
			RemoteVoltage?.TryDispose();
			RemoteVoltage = null;
			RemoteQuietHoursActive?.TryDispose();
			RemoteQuietHoursActive = null;
			_commandCancelSource?.CancelAndDispose();
			_commandCancelSource = null;
			try
			{
				_hourMeterPropertyChangedSource?.Dispose();
			}
			catch
			{
			}
			_hourMeterPropertyChangedSource = null;
			base.Dispose(disposing);
		}

		public async Task<CommandResult> SendGeneratorOnePressOnCommandAsync()
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceGeneratorGenie", DeviceName + " Generator ExecuteCommand ignored as relay has been disposed");
				return CommandResult.ErrorOther;
			}
			if (ActiveConnection == LogicalDeviceActiveConnection.Remote)
			{
				return await RemoteOnePressSwitch.SendCommandSetSwitch(value: true);
			}
			LogicalDeviceGeneratorGenieCommand logicalDeviceGeneratorGenieCommand = new LogicalDeviceGeneratorGenieCommand(GeneratorGenieCommand.On);
			if (DeviceService.GetPrimaryDeviceSourceDirect(this) is IDirectCommandGeneratorGenie directCommandGeneratorGenie)
			{
				return await directCommandGeneratorGenie.SendDirectCommandGeneratorGenie(this, logicalDeviceGeneratorGenieCommand.ToGeneratorGenieCommand, CancellationToken.None);
			}
			return CommandResult.ErrorRemoteOperationNotSupported;
		}

		public async Task<CommandResult> SendGeneratorOnePressOffCommandAsync()
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceGeneratorGenie", DeviceName + " Generator ExecuteCommand ignored as relay has been disposed");
				return CommandResult.ErrorOther;
			}
			if (ActiveConnection == LogicalDeviceActiveConnection.Remote)
			{
				return await RemoteOnePressSwitch.SendCommandSetSwitch(value: false);
			}
			LogicalDeviceGeneratorGenieCommand logicalDeviceGeneratorGenieCommand = new LogicalDeviceGeneratorGenieCommand(GeneratorGenieCommand.Off);
			if (DeviceService.GetPrimaryDeviceSourceDirect(this) is IDirectCommandGeneratorGenie directCommandGeneratorGenie)
			{
				return await directCommandGeneratorGenie.SendDirectCommandGeneratorGenie(this, logicalDeviceGeneratorGenieCommand.ToGeneratorGenieCommand, CancellationToken.None);
			}
			return CommandResult.ErrorRemoteOperationNotSupported;
		}

		public Task<float> ReadVoltageMeasurementAsync(CancellationToken cancellationToken)
		{
			return LogicalDeviceVoltageExtension.ReadVoltageMeasurementAsync(VoltageMeasurementBatteryPid, cancellationToken);
		}

		public async Task<CommandResult> PerformCommand(AutoStartLowVoltageMode option)
		{
			return await TryWritePid(() => GeneratorAutoStartLowVoltagePid.WriteFloatAsync(option, CancellationToken.None));
		}

		public async Task<CommandResult> PerformCommand(AutoStartDurationMode option)
		{
			return await TryWritePid(() => GeneratorAutoRunDurationMinutesPid.WriteAsync(TimeSpan.FromMinutes((int)option), CancellationToken.None));
		}

		public async Task<CommandResult> PerformCommand(AutoStartOffTimeMode option)
		{
			return await TryWritePid(() => GeneratorAutoRunMinOffTimeMinutesPid.WriteAsync(TimeSpan.FromMinutes((int)option), CancellationToken.None));
		}

		private async Task<CommandResult> TryWritePid(Func<Task> writePidFuncAsync)
		{
			try
			{
				await writePidFuncAsync();
			}
			catch (PhysicalDeviceNotFoundException)
			{
				return CommandResult.ErrorDeviceOffline;
			}
			catch (LogicalDevicePidException ex2) when (ex2 is LogicalDevicePidValueWriteNotSupportedException || ex2 is LogicalDevicePidNotSupportedException)
			{
				return CommandResult.ErrorRemoteOperationNotSupported;
			}
			catch (LogicalDevicePidCanceledException)
			{
				return CommandResult.Canceled;
			}
			catch (LogicalDevicePidTimeoutException)
			{
				return CommandResult.ErrorCommandTimeout;
			}
			catch (Exception)
			{
				return CommandResult.ErrorOther;
			}
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
