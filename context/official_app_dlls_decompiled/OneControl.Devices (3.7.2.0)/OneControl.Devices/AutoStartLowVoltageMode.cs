using System;

namespace OneControl.Devices
{
	public struct AutoStartLowVoltageMode
	{
		private readonly float value;

		public static AutoStartLowVoltageMode Off = 0f;

		public static AutoStartLowVoltageMode Thirteen = 13f;

		public static AutoStartLowVoltageMode TwelvePointFive = 12.5f;

		public static AutoStartLowVoltageMode Twelve = 12f;

		public static AutoStartLowVoltageMode ElevenPointSeven = 11.7f;

		public static AutoStartLowVoltageMode ElevenPointFive = 11.5f;

		public static AutoStartLowVoltageMode Eleven = 11f;

		public static AutoStartLowVoltageMode TenPointFive = 10.5f;

		public AutoStartLowVoltageMode(float givenValue)
		{
			value = givenValue;
		}

		public AutoStartLowVoltageMode(AutoStartLowVoltageMode givenValue)
		{
			value = givenValue.value;
		}

		public static implicit operator AutoStartLowVoltageMode(float givenValue)
		{
			return new AutoStartLowVoltageMode(givenValue);
		}

		public static implicit operator float(AutoStartLowVoltageMode givenValue)
		{
			return givenValue.value;
		}

		public override string ToString()
		{
			return $"{(float)this} volts";
		}

		public override bool Equals(object obj)
		{
			if (obj is float num)
			{
				return (double)Math.Abs(num - value) < 0.01;
			}
			return base.Equals(obj);
		}
	}
}
