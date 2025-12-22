using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public sealed class IDS_CAN_VERSION_NUMBER
	{
		private static readonly IDS_CAN_VERSION_NUMBER[] Table;

		private static readonly List<IDS_CAN_VERSION_NUMBER> List;

		public static readonly IDS_CAN_VERSION_NUMBER UNKNOWN;

		public const byte VERSION_0_9 = 0;

		public const byte VERSION_1_0 = 1;

		public const byte VERSION_1_1 = 2;

		public const byte VERSION_1_2 = 3;

		public const byte VERSION_1_3 = 4;

		public const byte VERSION_1_4 = 5;

		public const byte VERSION_1_5 = 6;

		public const byte VERSION_1_6 = 7;

		public const byte VERSION_2_0 = 8;

		public const byte VERSION_2_1 = 9;

		public const byte VERSION_2_2 = 10;

		public const byte VERSION_2_3 = 11;

		public const byte VERSION_2_4 = 12;

		public const byte VERSION_2_5 = 13;

		public const byte VERSION_2_6 = 14;

		public const byte VERSION_2_7 = 15;

		public const byte VERSION_2_8 = 16;

		public const byte VERSION_2_9 = 17;

		public const byte VERSION_3_0 = 18;

		public const byte VERSION_3_1 = 19;

		public const byte VERSION_3_2 = 20;

		public const byte VERSION_3_3 = 21;

		public const byte VERSION_3_4 = 22;

		public const byte VERSION_3_5 = 23;

		public const byte VERSION_3_6 = 24;

		public const byte VERSION_3_7 = 25;

		public const byte VERSION_3_8 = 26;

		public const byte VERSION_3_9 = 27;

		public const byte VERSION_4_0 = 28;

		public const byte VERSION_4_1 = 29;

		public const byte VERSION_4_2 = 30;

		public const byte VERSION_4_3 = 31;

		public const byte VERSION_4_4 = 32;

		public const byte VERSION_4_5 = 33;

		public const byte VERSION_4_6 = 34;

		public const byte VERSION_4_7 = 35;

		public const byte VERSION_LATEST = 28;

		public readonly byte Value;

		public readonly string Name;

		public readonly int Major;

		public readonly int Minor;

		public static IEnumerable<IDS_CAN_VERSION_NUMBER> GetEnumerator()
		{
			return List;
		}

		static IDS_CAN_VERSION_NUMBER()
		{
			Table = new IDS_CAN_VERSION_NUMBER[256];
			List = new List<IDS_CAN_VERSION_NUMBER>();
			UNKNOWN = new IDS_CAN_VERSION_NUMBER(-1, -1, -1);
			new IDS_CAN_VERSION_NUMBER(0, 0, 9);
			new IDS_CAN_VERSION_NUMBER(1, 1, 0);
			new IDS_CAN_VERSION_NUMBER(2, 1, 1);
			new IDS_CAN_VERSION_NUMBER(3, 1, 2);
			new IDS_CAN_VERSION_NUMBER(4, 1, 3);
			new IDS_CAN_VERSION_NUMBER(5, 1, 4);
			new IDS_CAN_VERSION_NUMBER(6, 1, 5);
			new IDS_CAN_VERSION_NUMBER(7, 1, 6);
			new IDS_CAN_VERSION_NUMBER(8, 2, 0);
			new IDS_CAN_VERSION_NUMBER(9, 2, 1);
			new IDS_CAN_VERSION_NUMBER(10, 2, 2);
			new IDS_CAN_VERSION_NUMBER(11, 2, 3);
			new IDS_CAN_VERSION_NUMBER(12, 2, 4);
			new IDS_CAN_VERSION_NUMBER(13, 2, 5);
			new IDS_CAN_VERSION_NUMBER(14, 2, 6);
			new IDS_CAN_VERSION_NUMBER(15, 2, 7);
			new IDS_CAN_VERSION_NUMBER(16, 2, 8);
			new IDS_CAN_VERSION_NUMBER(17, 2, 9);
			new IDS_CAN_VERSION_NUMBER(18, 3, 0);
			new IDS_CAN_VERSION_NUMBER(19, 3, 1);
			new IDS_CAN_VERSION_NUMBER(20, 3, 2);
			new IDS_CAN_VERSION_NUMBER(21, 3, 3);
			new IDS_CAN_VERSION_NUMBER(22, 3, 4);
			new IDS_CAN_VERSION_NUMBER(23, 3, 5);
			new IDS_CAN_VERSION_NUMBER(24, 3, 6);
			new IDS_CAN_VERSION_NUMBER(25, 3, 7);
			new IDS_CAN_VERSION_NUMBER(26, 3, 8);
			new IDS_CAN_VERSION_NUMBER(27, 3, 9);
			new IDS_CAN_VERSION_NUMBER(28, 4, 0);
			new IDS_CAN_VERSION_NUMBER(29, 4, 1);
			new IDS_CAN_VERSION_NUMBER(30, 4, 2);
			new IDS_CAN_VERSION_NUMBER(31, 4, 3);
			new IDS_CAN_VERSION_NUMBER(32, 4, 4);
			new IDS_CAN_VERSION_NUMBER(33, 4, 5);
			new IDS_CAN_VERSION_NUMBER(34, 4, 6);
			new IDS_CAN_VERSION_NUMBER(35, 4, 7);
		}

		private IDS_CAN_VERSION_NUMBER(byte value)
		{
			Value = value;
			Name = "Version " + value.HexString() + "h";
			Major = -1;
			Minor = -1;
			Table[value] = this;
		}

		private IDS_CAN_VERSION_NUMBER(int value, int major, int minor)
		{
			if (value < 0 || value > 255)
			{
				Value = 0;
				Major = -1;
				Minor = -1;
				Name = "UNKNOWN";
			}
			else
			{
				Value = (byte)value;
				Major = major;
				Minor = minor;
				Name = "Version " + Major + "." + Minor;
				List.Add(this);
				Table[value] = this;
			}
		}

		public static implicit operator byte(IDS_CAN_VERSION_NUMBER version)
		{
			return version.Value;
		}

		public static implicit operator IDS_CAN_VERSION_NUMBER(byte value)
		{
			if (Table[value] != null)
			{
				return Table[value];
			}
			return new IDS_CAN_VERSION_NUMBER(value);
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
