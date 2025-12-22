using System;

namespace IDS.Core.IDS_CAN.Devices
{
	public class REAL_TIME_CLOCK : LocalDevice
	{
		public bool IsValid => base.Adapter.Clock.IsValid;

		public DateTime CurrentDateTime
		{
			get
			{
				return base.Adapter.Clock.CurrentDateTime;
			}
			set
			{
				base.Adapter.Clock.CurrentDateTime = value;
			}
		}

		public DateTime TimeLastSet => base.Adapter.Clock.TimeLastSet;

		public byte TIME_ZONE => base.Adapter.Clock.TIME_ZONE;

		public byte RTC_TIME_SEC
		{
			get
			{
				return base.Adapter.Clock.RTC_TIME_SEC;
			}
			set
			{
				base.Adapter.Clock.RTC_TIME_SEC = value;
			}
		}

		public byte RTC_TIME_MIN
		{
			get
			{
				return base.Adapter.Clock.RTC_TIME_MIN;
			}
			set
			{
				base.Adapter.Clock.RTC_TIME_MIN = value;
			}
		}

		public byte RTC_TIME_HOUR
		{
			get
			{
				return base.Adapter.Clock.RTC_TIME_HOUR;
			}
			set
			{
				base.Adapter.Clock.RTC_TIME_HOUR = value;
			}
		}

		public byte RTC_TIME_DAY
		{
			get
			{
				return base.Adapter.Clock.RTC_TIME_DAY;
			}
			set
			{
				base.Adapter.Clock.RTC_TIME_DAY = value;
			}
		}

		public byte RTC_TIME_MONTH
		{
			get
			{
				return base.Adapter.Clock.RTC_TIME_MONTH;
			}
			set
			{
				base.Adapter.Clock.RTC_TIME_MONTH = value;
			}
		}

		public ushort RTC_TIME_YEAR
		{
			get
			{
				return base.Adapter.Clock.RTC_TIME_YEAR;
			}
			set
			{
				base.Adapter.Clock.RTC_TIME_YEAR = value;
			}
		}

		public uint RTC_EPOCH_SEC
		{
			get
			{
				return base.Adapter.Clock.RTC_EPOCH_SEC;
			}
			set
			{
				base.Adapter.Clock.RTC_EPOCH_SEC = value;
			}
		}

		public uint RTC_SET_TIME_SEC { get; private set; }

		public ushort TIME_SINCE_CLOCK_SET => base.Adapter.Clock.TIME_SINCE_CLOCK_SET;

		public REAL_TIME_CLOCK(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(new LocalProduct(adapter, mac, product_id, version, software_part_number), (byte)14, 0, (ushort)150, 0, 0, options)
		{
		}

		public void SetTime(int year, int month, int day, int hour, int minute, int second)
		{
			base.Adapter.Clock.SetTime(year, month, day, hour, minute, second);
		}

		protected override void OnBackgroundTask()
		{
			base.OnBackgroundTask();
			base.DeviceStatus = new TIME_MESSAGE_PAYLOAD(RTC_EPOCH_SEC, TIME_SINCE_CLOCK_SET, TIME_ZONE);
		}
	}
}
