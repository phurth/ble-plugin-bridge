namespace IDS.Core.IDS_CAN.Devices
{
	public class TEMPERATURE_SENSOR_SIMULATOR_INTERFACE : LocalDevice
	{
		public TEMPERATURE_SENSOR_STATUS_PARAMS mStatusMessageParams;

		public short TemperatureC
		{
			get
			{
				return mStatusMessageParams.TemperatureC;
			}
			set
			{
				mStatusMessageParams.TemperatureC = value;
				base.DeviceStatus = mStatusMessageParams.GetPayload();
			}
		}

		public byte BatteryVoltage
		{
			get
			{
				return mStatusMessageParams.BatteryVoltage;
			}
			set
			{
				mStatusMessageParams.BatteryVoltage = value;
				base.DeviceStatus = mStatusMessageParams.GetPayload();
			}
		}

		public byte BatteryLevel
		{
			get
			{
				return mStatusMessageParams.BatteryLevel;
			}
			set
			{
				mStatusMessageParams.BatteryLevel = value;
				base.DeviceStatus = mStatusMessageParams.GetPayload();
			}
		}

		public TEMPERATURE_SENSOR_SIMULATOR_INTERFACE(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(new LocalProduct(adapter, mac, product_id, version, software_part_number), (byte)25, 0, (ushort)166, 0, 0, options)
		{
			mStatusMessageParams = new TEMPERATURE_SENSOR_STATUS_PARAMS();
			mStatusMessageParams.TemperatureC = 6144;
		}
	}
}
