namespace IDS.Core.IDS_CAN.Devices
{
	public class AWNING_SENSOR_SIMULATOR_INTERFACE : LocalDevice
	{
		private AWNING_SENSOR_STATUS_PARAMS mStatus;

		public ushort AngleDegrees
		{
			get
			{
				return mStatus.Angle;
			}
			set
			{
				if (mStatus.Angle != value)
				{
					mStatus.Angle = value;
					base.DeviceStatus = mStatus.GetPayload();
				}
			}
		}

		public byte HighWindAlert
		{
			get
			{
				return mStatus.HighWindAlert;
			}
			set
			{
				mStatus.HighWindAlert = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		public byte MediumWindAlert
		{
			get
			{
				return mStatus.MediumWindAlert;
			}
			set
			{
				mStatus.MediumWindAlert = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		public byte LowWindAlert
		{
			get
			{
				return mStatus.LowWindAlert;
			}
			set
			{
				mStatus.LowWindAlert = value;
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

		public byte LowBattAlert
		{
			get
			{
				return mStatus.LowBattAlert;
			}
			set
			{
				mStatus.LowBattAlert = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		public byte MiscStatus
		{
			get
			{
				return mStatus.MiscStatus;
			}
			set
			{
				mStatus.MiscStatus = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		public AWNING_SENSOR_SIMULATOR_INTERFACE(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(new LocalProduct(adapter, mac, product_id, version, software_part_number), (byte)47, 0, (ushort)361, 0, 0, options)
		{
			mStatus = new AWNING_SENSOR_STATUS_PARAMS();
			base.DeviceStatus = mStatus.GetPayload();
		}
	}
}
