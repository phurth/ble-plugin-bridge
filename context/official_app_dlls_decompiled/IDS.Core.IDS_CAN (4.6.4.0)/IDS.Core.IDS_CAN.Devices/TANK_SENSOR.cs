using System;

namespace IDS.Core.IDS_CAN.Devices
{
	public class TANK_SENSOR : LocalDevice
	{
		private TANK_SENSOR_STATUS_PARAMS mStatus;

		private bool mIsFuelSensor;

		private bool mIsHighPrecision;

		private bool mSupportsTankAlerts;

		private bool mSupportsBattery;

		private bool mSupportsTankCapacity;

		private bool mIsMopekaType;

		public bool IsFuelSensor
		{
			get
			{
				return mIsFuelSensor;
			}
			set
			{
				mIsFuelSensor = value;
				UpdateDeviceCapabilities();
			}
		}

		public bool IsHighPrecision
		{
			get
			{
				return mIsHighPrecision;
			}
			set
			{
				mIsHighPrecision = value;
				UpdateDeviceCapabilities();
			}
		}

		public bool SupportsTankAlerts
		{
			get
			{
				return mSupportsTankAlerts;
			}
			set
			{
				mSupportsTankAlerts = value;
				UpdateDeviceCapabilities();
			}
		}

		public bool SupportsBattery
		{
			get
			{
				return mSupportsBattery;
			}
			set
			{
				mSupportsBattery = value;
				UpdateDeviceCapabilities();
			}
		}

		public bool SupportsTankCapacity
		{
			get
			{
				return mSupportsTankCapacity;
			}
			set
			{
				mSupportsTankCapacity = value;
				UpdateDeviceCapabilities();
			}
		}

		public bool IsMopekaType
		{
			get
			{
				return mIsMopekaType;
			}
			set
			{
				mIsMopekaType = value;
				UpdateDeviceCapabilities();
			}
		}

		public byte FillLevel
		{
			get
			{
				return mStatus.FillLevel;
			}
			set
			{
				mStatus.FillLevel = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		public byte BatteryLevel
		{
			get
			{
				return mStatus.BatteryLevel;
			}
			set
			{
				mStatus.BatteryLevel = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		public byte MeasurementQuality
		{
			get
			{
				return mStatus.MeasurementQuality;
			}
			set
			{
				mStatus.MeasurementQuality = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		public sbyte XAcceleration
		{
			get
			{
				return mStatus.XAcceleration;
			}
			set
			{
				mStatus.XAcceleration = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		public sbyte YAcceleration
		{
			get
			{
				return mStatus.YAcceleration;
			}
			set
			{
				mStatus.YAcceleration = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		public byte TankLevelAlert
		{
			get
			{
				return mStatus.TankLevelAlert;
			}
			set
			{
				mStatus.TankLevelAlert = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		public ushort UserMessage
		{
			get
			{
				return mStatus.UserMessage;
			}
			set
			{
				mStatus.UserMessage = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		public TANK_SENSOR(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(new LocalProduct(adapter, mac, product_id, version, software_part_number), (byte)10, 0, (ushort)67, 0, 0, options)
		{
			mStatus = new TANK_SENSOR_STATUS_PARAMS();
			base.DeviceStatus = mStatus.GetPayload();
			UpdateDeviceCapabilities();
		}

		protected void UpdateDeviceCapabilities()
		{
			byte b = 0;
			b = (byte)(b | Convert.ToByte(mIsFuelSensor));
			b = (byte)(b | (byte)(Convert.ToByte(mIsHighPrecision) << 1));
			b = (byte)(b | (byte)(Convert.ToByte(mSupportsTankAlerts) << 2));
			b = (byte)(b | (byte)(Convert.ToByte(mSupportsBattery) << 3));
			b = (byte)(b | (byte)(Convert.ToByte(mSupportsTankCapacity) << 4));
			b = (byte)(b | (byte)(Convert.ToByte(mIsMopekaType) << 5));
			base.DeviceCapabilities = b;
		}
	}
}
