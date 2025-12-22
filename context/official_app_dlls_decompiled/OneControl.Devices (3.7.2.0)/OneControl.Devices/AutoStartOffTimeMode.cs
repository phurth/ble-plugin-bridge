using System;

namespace OneControl.Devices
{
	public struct AutoStartOffTimeMode
	{
		private readonly int value;

		public static AutoStartOffTimeMode Thirty = 30;

		public static AutoStartOffTimeMode Sixty = 60;

		public static AutoStartOffTimeMode Ninety = 90;

		public static AutoStartOffTimeMode OneHundredAndTwenty = 120;

		public static AutoStartOffTimeMode OneHundredAndEighty = 180;

		public AutoStartOffTimeMode(int givenValue)
		{
			value = givenValue;
		}

		public AutoStartOffTimeMode(AutoStartOffTimeMode givenValue)
		{
			value = givenValue.value;
		}

		public static implicit operator AutoStartOffTimeMode(int givenValue)
		{
			return new AutoStartOffTimeMode(givenValue);
		}

		public static implicit operator int(AutoStartOffTimeMode givenValue)
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
				return Math.Abs(timeSpan.TotalMinutes - (double)value) < double.Epsilon;
			}
			return base.Equals(obj);
		}
	}
}
