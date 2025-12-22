using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceTankSensorCapability : LogicalDeviceCapability, ILogicalDeviceTankSensorCapability, ILogicalDeviceCapability, INotifyPropertyChanged
	{
		private const byte SensorFluidTypeBitmask = 1;

		private const byte SensorPrecisionTypeBitmask = 2;

		private const byte SupportsTankAlertsBitmask = 4;

		private const byte SupportsBatteryLevelBitmask = 8;

		private const byte SupportsTankCapacityBitmask = 16;

		private const byte SupportsTankHeightOrientationBitmask = 32;

		private SensorFluidType SensorFluidType
		{
			get
			{
				if ((RawValue & 1) != 1)
				{
					return SensorFluidType.Water;
				}
				return SensorFluidType.Fuel;
			}
		}

		public SensorPrecisionType SensorPrecisionType
		{
			get
			{
				if ((RawValue & 2) == 2)
				{
					return SensorPrecisionType.HighPrecision;
				}
				if ((RawValue & 1) == 1)
				{
					return SensorPrecisionType.MediumPrecision;
				}
				return SensorPrecisionType.LowPrecision;
			}
		}

		public float SensorPrecision
		{
			get
			{
				if (SensorPrecisionType == SensorPrecisionType.HighPrecision)
				{
					return 1f;
				}
				if (SensorFluidType == SensorFluidType.Fuel)
				{
					return 11f;
				}
				return 33f;
			}
		}

		public bool AreTankAlertsSupported => (RawValue & 4) == 4;

		public bool IsBatteryLevelSupported => (RawValue & 8) == 8;

		public bool IsTankCapacitySupported => (RawValue & 0x10) == 16;

		public bool IsTankHeightOrientationSupported => (RawValue & 0x20) == 32;

		public LogicalDeviceTankSensorCapability()
			: this(null)
		{
		}

		public LogicalDeviceTankSensorCapability(byte? rawCapability)
		{
			UpdateDeviceCapability(rawCapability);
		}

		public LogicalDeviceTankSensorCapability(SensorPrecisionType sensorPrecisionType, bool supportsTankAlerts)
			: this(MakeRawCapability(sensorPrecisionType, supportsTankAlerts))
		{
		}

		private static byte MakeRawCapability(SensorPrecisionType sensorPrecisionType, bool supportsTankAlerts)
		{
			byte b = 0;
			if (sensorPrecisionType == SensorPrecisionType.HighPrecision)
			{
				b = (byte)(b | 2u);
			}
			if (supportsTankAlerts)
			{
				b = (byte)(b | 4u);
			}
			return b;
		}

		protected override void OnUpdateDeviceCapabilityChanged()
		{
			NotifyPropertyChanged("SensorFluidType");
			NotifyPropertyChanged("SensorPrecisionType");
			NotifyPropertyChanged("AreTankAlertsSupported");
			base.OnUpdateDeviceCapabilityChanged();
		}
	}
}
