using System;
using System.Runtime.CompilerServices;

namespace IDS.Core.IDS_CAN
{
	public sealed class SOFTWARE_UPDATE_STATE
	{
		public const byte NO_UPDATE_AVAILBLE = 0;

		public const byte UPDATE_AVAILBLE = 1;

		public const byte UPDATE_AUTHORIZED = 2;

		public const byte UPDATE_IN_PROGRESS = 3;

		private static readonly SOFTWARE_UPDATE_STATE[] Array;

		private readonly byte Value;

		private readonly string Name;

		static SOFTWARE_UPDATE_STATE()
		{
			Array = new SOFTWARE_UPDATE_STATE[4];
			new SOFTWARE_UPDATE_STATE(0, "NO_UPDATE_AVAILBLE");
			new SOFTWARE_UPDATE_STATE(1, "UPDATE_AVAILBLE");
			new SOFTWARE_UPDATE_STATE(2, "UPDATE_AUTHORIZED");
			new SOFTWARE_UPDATE_STATE(3, "UPDATE_IN_PROGRESS");
		}

		private SOFTWARE_UPDATE_STATE(byte value, string name)
		{
			Value = value;
			Name = name;
			Array[value] = this;
		}

		public static implicit operator byte(SOFTWARE_UPDATE_STATE level)
		{
			return level.Value;
		}

		public static implicit operator SOFTWARE_UPDATE_STATE(byte value)
		{
			if (value > 3)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(42, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Cannot convert ");
				defaultInterpolatedStringHandler.AppendFormatted(value);
				defaultInterpolatedStringHandler.AppendLiteral(" to a SOFTWARE_UPDATE_STATE");
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
