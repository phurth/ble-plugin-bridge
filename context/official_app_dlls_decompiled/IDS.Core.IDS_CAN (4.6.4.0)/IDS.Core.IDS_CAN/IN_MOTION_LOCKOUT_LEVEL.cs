using System;
using System.Runtime.CompilerServices;

namespace IDS.Core.IDS_CAN
{
	public sealed class IN_MOTION_LOCKOUT_LEVEL
	{
		public const byte LEVEL_0_NO_LOCKOUT = 0;

		public const byte LEVEL_1_MOBILE_DEVICE_LOCKOUT = 1;

		public const byte LEVEL_2_NETWORK_LOCKOUT = 2;

		public const byte LEVEL_3_FULL_LOCKOUT = 3;

		private static readonly IN_MOTION_LOCKOUT_LEVEL[] Array;

		private readonly byte Value;

		private readonly string Name;

		static IN_MOTION_LOCKOUT_LEVEL()
		{
			Array = new IN_MOTION_LOCKOUT_LEVEL[4];
			new IN_MOTION_LOCKOUT_LEVEL(0, "NO_LOCKOUT");
			new IN_MOTION_LOCKOUT_LEVEL(1, "MOBILE_DEVICE_LOCKOUT");
			new IN_MOTION_LOCKOUT_LEVEL(2, "NETWORK_LOCKOUT");
			new IN_MOTION_LOCKOUT_LEVEL(3, "FULL_LOCKOUT");
		}

		private IN_MOTION_LOCKOUT_LEVEL(byte value, string name)
		{
			Value = value;
			Name = name;
			Array[value] = this;
		}

		public static implicit operator byte(IN_MOTION_LOCKOUT_LEVEL level)
		{
			return level.Value;
		}

		public static implicit operator IN_MOTION_LOCKOUT_LEVEL(byte value)
		{
			if (value > 3)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Cannot convert ");
				defaultInterpolatedStringHandler.AppendFormatted(value);
				defaultInterpolatedStringHandler.AppendLiteral(" to a IN_MOTION_LOCKOUT_LEVEL");
				throw new InvalidCastException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return Array[value];
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
