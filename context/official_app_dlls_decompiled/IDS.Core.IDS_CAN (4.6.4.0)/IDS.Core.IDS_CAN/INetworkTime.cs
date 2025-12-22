using System;

namespace IDS.Core.IDS_CAN
{
	public interface INetworkTime
	{
		bool IsValid { get; }

		bool IsTimeAuthority { get; }

		DateTime CurrentDateTime { get; set; }

		DateTime TimeLastSet { get; }

		byte TIME_ZONE { get; }

		byte RTC_TIME_SEC { get; set; }

		byte RTC_TIME_MIN { get; set; }

		byte RTC_TIME_HOUR { get; set; }

		byte RTC_TIME_DAY { get; set; }

		byte RTC_TIME_MONTH { get; set; }

		ushort RTC_TIME_YEAR { get; set; }

		uint RTC_EPOCH_SEC { get; set; }

		uint RTC_SET_TIME_SEC { get; }

		ushort TIME_SINCE_CLOCK_SET { get; }

		void SetTime(int year, int month, int day, int hour, int minute, int second);
	}
}
