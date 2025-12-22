using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.TemperatureSensor
{
	public class LogicalDeviceTemperatureSensorSim : LogicalDeviceTemperatureSensor, ILogicalDeviceSimulated, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		public const string LogTag = "LogicalDeviceTemperatureSensorSim";

		private const int TickIntervalMs = 3000;

		private const int TickIntervalDelayMs = 250;

		private const int StatusLength = 8;

		private const int FlagsIndex = 4;

		private const float TempHighLimit = 24f;

		private const float TempLowLimit = 18f;

		private const float BatteryHighLimit = 3f;

		private const float BatteryLowLimit = 0.1f;

		private const int BatteryPercentHighLimit = 100;

		private const int BatteryPercentLowLimit = 0;

		private readonly LogicalDeviceTemperatureSensorStatus _simStatus = new LogicalDeviceTemperatureSensorStatus();

		private readonly BackgroundOperation _simulator;

		private byte[] _statusData = new byte[8] { 18, 0, 192, 10, 0, 0, 0, 0 };

		private float _tempChangeValue = 0.1f;

		private float _batteryChangeValue = 0.1f;

		private int _batteryPercentValue = 1;

		private const int SimulatedActivityDelayMs = 1500;

		private bool IsTemperatureLowAlertTriggerDisabled;

		private bool IsTemperatureHighAlertTriggerDisabled;

		private float TemperatureLowAlertTriggerValueCelsius = -18f;

		private float TemperatureHighAlertTriggerValueCelsius = 27f;

		private bool _isOnline = true;

		private const uint DelayStatusChangedLogMessageThrottleTimeMs = 60000u;

		private readonly Stopwatch _delayStatusChangedLogMessageThrottleTimer = Stopwatch.StartNew();

		public override LogicalDeviceActiveConnection ActiveConnection
		{
			get
			{
				if (!IsOnline)
				{
					return LogicalDeviceActiveConnection.Offline;
				}
				return LogicalDeviceActiveConnection.Direct;
			}
		}

		public virtual bool IsOnline
		{
			get
			{
				return _isOnline;
			}
			protected set
			{
				if (SetBackingField(ref _isOnline, value, "IsOnline"))
				{
					UpdateDeviceOnline(value);
				}
			}
		}

		public LogicalDeviceTemperatureSensorSim(LogicalDeviceId deviceId, LogicalDeviceTemperatureSensorCapability capability, ILogicalDeviceService deviceService)
			: base(deviceId, capability, deviceService)
		{
			RequestModePid = new LogicalDevicePidSimByte(PID.ACC_REQUEST_MODE, 1);
			_simStatus.Update(_statusData, 8);
			UpdateDeviceStatus(_simStatus.Data, _simStatus.Size);
			_simulator = new BackgroundOperation((BackgroundOperation.BackgroundOperationFunc)SimulatorAsync);
			_simulator.Start();
		}

		public override void UpdateDeviceOnline(bool online)
		{
			IsOnline = online;
		}

		private async Task SimulatorAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (ActiveConnection == LogicalDeviceActiveConnection.Direct)
				{
					_simStatus.Update(_statusData, 8);
					await TaskExtension.TryDelay(3000, cancellationToken);
					UpdateTemperature(_simStatus);
					await TaskExtension.TryDelay(250, cancellationToken);
					UpdateVoltage(_simStatus);
					await TaskExtension.TryDelay(250, cancellationToken);
					UpdateBatteryChargePercentage(_simStatus);
					UpdateDeviceStatus(_simStatus.Data, _simStatus.Size);
				}
			}
		}

		private void UpdateTemperature(LogicalDeviceTemperatureSensorStatus simStatus)
		{
			float num = base.TemperatureCelsius + _tempChangeValue;
			if (num > 24f)
			{
				num = 24f;
				_tempChangeValue = -0.1f;
			}
			else if (num < 18f)
			{
				num = 18f;
				_tempChangeValue = 0.1f;
			}
			simStatus.SetTemperatureCelsius(num);
			if (simStatus.TemperatureCelsius < TemperatureLowAlertTriggerValueCelsius)
			{
				if (!simStatus.IsTemperatureLow && !IsTemperatureLowAlertTriggerDisabled)
				{
					simStatus.SetTemperatureLowAlert(active: true, (byte)(simStatus.TemperatureLowAlertCount + 1));
				}
				else
				{
					simStatus.SetTemperatureHighAlert(active: true, (byte)simStatus.TemperatureLowAlertCount);
				}
				simStatus.SetTemperatureHighAlert(active: false, (byte)simStatus.TemperatureHighAlertCount);
				simStatus.SetTemperatureInRangeAlert(active: false, (byte)simStatus.TemperatureInRangeAlertCount);
			}
			else if (simStatus.TemperatureCelsius >= TemperatureLowAlertTriggerValueCelsius && simStatus.TemperatureCelsius <= TemperatureHighAlertTriggerValueCelsius)
			{
				if (!simStatus.IsTemperatureInRange && !IsTemperatureLowAlertTriggerDisabled && !IsTemperatureHighAlertTriggerDisabled)
				{
					simStatus.SetTemperatureInRangeAlert(active: true, (byte)(simStatus.TemperatureInRangeAlertCount + 1));
				}
				else
				{
					simStatus.SetTemperatureHighAlert(active: true, (byte)simStatus.TemperatureInRangeAlertCount);
				}
				simStatus.SetTemperatureLowAlert(active: false, (byte)simStatus.TemperatureLowAlertCount);
				simStatus.SetTemperatureHighAlert(active: false, (byte)simStatus.TemperatureHighAlertCount);
			}
			else if (simStatus.TemperatureCelsius > TemperatureHighAlertTriggerValueCelsius)
			{
				if (!simStatus.IsTemperatureHigh && !IsTemperatureHighAlertTriggerDisabled)
				{
					simStatus.SetTemperatureHighAlert(active: true, (byte)(simStatus.TemperatureHighAlertCount + 1));
				}
				else
				{
					simStatus.SetTemperatureHighAlert(active: true, (byte)simStatus.TemperatureHighAlertCount);
				}
				simStatus.SetTemperatureLowAlert(active: false, (byte)simStatus.TemperatureLowAlertCount);
				simStatus.SetTemperatureInRangeAlert(active: false, (byte)simStatus.TemperatureInRangeAlertCount);
			}
		}

		private void UpdateVoltage(LogicalDeviceTemperatureSensorStatus simStatus)
		{
			float num = base.SensorBatteryVoltage + _batteryChangeValue;
			if (num < 0.1f)
			{
				num = 0.1f;
				_batteryChangeValue = 0.1f;
			}
			else if (num > 3f)
			{
				num = 3f;
				_batteryChangeValue = -0.1f;
			}
			simStatus.SetSensorBatteryVoltage(num);
		}

		private void UpdateBatteryChargePercentage(LogicalDeviceTemperatureSensorStatus simStatus)
		{
			int num = base.SensorBatteryChargePercent + _batteryPercentValue;
			if (num < 0)
			{
				num = 0;
				_batteryPercentValue = 1;
			}
			else if (num > 100)
			{
				num = 100;
				_batteryPercentValue = -1;
			}
			simStatus.SetSensorBatteryVoltage(num);
			if (simStatus.BatteryChargePercent <= 25)
			{
				if (!simStatus.IsBatteryLow)
				{
					simStatus.SetLowBatteryAlert(active: true, (byte)(simStatus.LowBatteryAlertCount + 1));
				}
				else
				{
					simStatus.SetLowBatteryAlert(active: true, (byte)simStatus.LowBatteryAlertCount);
				}
			}
			else
			{
				simStatus.SetLowBatteryAlert(active: false, (byte)simStatus.LowBatteryAlertCount);
			}
		}

		public override async Task<float?> TryGetTemperatureHighAlertTriggerValueAsync(TemperatureScale scale, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await Task.Delay(1500, cancellationToken);
			if (IsTemperatureHighAlertTriggerDisabled)
			{
				return null;
			}
			float temperatureHighAlertTriggerValueCelsius = TemperatureHighAlertTriggerValueCelsius;
			return scale switch
			{
				TemperatureScale.Fahrenheit => (float)Temperature.ConvertToScale((decimal)temperatureHighAlertTriggerValueCelsius, TemperatureScale.Celsius, scale), 
				TemperatureScale.Celsius => temperatureHighAlertTriggerValueCelsius, 
				TemperatureScale.Kelvin => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
				_ => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
			};
		}

		public override async Task<float?> TryGetTemperatureLowAlertTriggerValueAsync(TemperatureScale scale, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await Task.Delay(1500, cancellationToken);
			if (IsTemperatureLowAlertTriggerDisabled)
			{
				return null;
			}
			float temperatureLowAlertTriggerValueCelsius = TemperatureLowAlertTriggerValueCelsius;
			return scale switch
			{
				TemperatureScale.Fahrenheit => (float)Temperature.ConvertToScale((decimal)temperatureLowAlertTriggerValueCelsius, TemperatureScale.Celsius, scale), 
				TemperatureScale.Celsius => temperatureLowAlertTriggerValueCelsius, 
				TemperatureScale.Kelvin => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
				_ => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
			};
		}

		public override async Task TrySetTemperatureHighAlertTriggerValueAsync(float? value, TemperatureScale scale, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await Task.Delay(1500, cancellationToken);
			if (!value.HasValue)
			{
				IsTemperatureHighAlertTriggerDisabled = true;
				return;
			}
			IsTemperatureHighAlertTriggerDisabled = false;
			float num = (TemperatureHighAlertTriggerValueCelsius = scale switch
			{
				TemperatureScale.Fahrenheit => (float)Temperature.ConvertToScale((decimal)value.Value, scale, TemperatureScale.Celsius), 
				TemperatureScale.Celsius => value.Value, 
				TemperatureScale.Kelvin => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
				_ => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
			});
		}

		public override async Task TrySetTemperatureLowAlertTriggerValueAsync(float? value, TemperatureScale scale, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await Task.Delay(1500, cancellationToken);
			if (!value.HasValue)
			{
				IsTemperatureLowAlertTriggerDisabled = true;
				return;
			}
			IsTemperatureLowAlertTriggerDisabled = false;
			float num = (TemperatureLowAlertTriggerValueCelsius = scale switch
			{
				TemperatureScale.Fahrenheit => (float)Temperature.ConvertToScale((decimal)value.Value, scale, TemperatureScale.Celsius), 
				TemperatureScale.Celsius => value.Value, 
				TemperatureScale.Kelvin => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
				_ => throw new ArgumentOutOfRangeException("scale", scale, "Only Fahrenheit and Celsius are supported."), 
			});
		}

		protected override void DebugUpdateDeviceStatusChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, uint dataLength, string optionalText = "")
		{
			if (_delayStatusChangedLogMessageThrottleTimer.ElapsedMilliseconds >= 60000)
			{
				base.DebugUpdateDeviceStatusChanged(oldStatusData, statusData, dataLength, $"Skipped debug messages over {60000u}ms");
				_delayStatusChangedLogMessageThrottleTimer.Restart();
			}
		}

		public override Task RequestTransitionToSleepMode(CancellationToken cancellationToken)
		{
			TaggedLog.Information("LogicalDeviceTemperatureSensorSim", $"Request Transition to Sleep Mode Ignored for Simulated device: {this}");
			return Task.CompletedTask;
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_simulator.Stop();
		}
	}
}
