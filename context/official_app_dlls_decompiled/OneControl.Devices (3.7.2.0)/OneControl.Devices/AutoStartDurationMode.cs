using System;

namespace OneControl.Devices
{
	public struct AutoStartDurationMode
	{
		private readonly int value;

		public static AutoStartDurationMode Thirty = 30;

		public static AutoStartDurationMode Sixty = 60;

		public static AutoStartDurationMode Ninety = 90;

		public static AutoStartDurationMode OneHundredAndTwenty = 120;

		public static AutoStartDurationMode OneHundredAndEighty = 180;

		public AutoStartDurationMode(int givenValue)
		{
			value = givenValue;
		}

		public AutoStartDurationMode(AutoStartDurationMode givenValue)
		{
			value = givenValue.value;
		}

		public static implicit operator AutoStartDurationMode(int givenValue)
		{
			return new AutoStartDurationMode(givenValue);
		}

		public static implicit operator int(AutoStartDurationMode givenValue)
		{
			return givenValue.value;
		}

		public override string ToString()
		{
			return $"{(int)this} minutes";
		}

		public override bool Equals(object obj)
		{
			if (obj is int num)
			{
				return num == value;
			}
			if (obj is TimeSpan timeSpan)
			{
				return timeSpan.TotalMinutes == (double)value;
			}
			return base.Equals(obj);
		}
	}
}
