using System;
using IDS.Core.Types;

namespace IDS.Core.IDS_CAN.Devices
{
	public class AC_POWER_MONITOR : LocalDevice
	{
		private float _measuredVoltage;

		private float _measuredCurrent;

		private byte _powerQuality;

		public uint SHORE_POWER_AMP_RATING { get; set; }

		public float MeasuredVoltage
		{
			get
			{
				return _measuredVoltage;
			}
			set
			{
				if (_measuredVoltage != value)
				{
					_measuredVoltage = value;
					UpdateStatus();
				}
			}
		}

		public float MeasuredCurrent
		{
			get
			{
				return _measuredCurrent;
			}
			set
			{
				if (_measuredCurrent != value)
				{
					_measuredCurrent = value;
					UpdateStatus();
				}
			}
		}

		public byte PowerQuality
		{
			get
			{
				return _powerQuality;
			}
			set
			{
				if (_powerQuality != value)
				{
					_powerQuality = value;
					UpdateStatus();
				}
			}
		}

		public bool NotAcceptingCommands
		{
			get
			{
				return base.IsNotAcceptingCommands;
			}
			set
			{
				base.IsNotAcceptingCommands = value;
			}
		}

		public AC_POWER_MONITOR(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(new LocalProduct(adapter, mac, product_id, version, software_part_number), (byte)26, 0, (ushort)259, 0, 0, options)
		{
			Init();
		}

		private void Init()
		{
			AddPID(PID.SHORE_POWER_AMP_RATING, () => SHORE_POWER_AMP_RATING, delegate(UInt48 arg)
			{
				SHORE_POWER_AMP_RATING = (uint)arg;
			});
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			int num = (int)Math.Round(MeasuredVoltage * 256f);
			if (num > int.MaxValue)
			{
				num = 32767;
			}
			if (num < int.MinValue)
			{
				num = -32768;
			}
			int num2 = (int)Math.Round(MeasuredCurrent * 256f);
			if (num2 > int.MaxValue)
			{
				num2 = int.MaxValue;
			}
			if (num2 < int.MinValue)
			{
				num2 = int.MinValue;
			}
			base.DeviceStatus = CAN.PAYLOAD.FromArgs((short)num, (short)num2, PowerQuality);
		}
	}
}
