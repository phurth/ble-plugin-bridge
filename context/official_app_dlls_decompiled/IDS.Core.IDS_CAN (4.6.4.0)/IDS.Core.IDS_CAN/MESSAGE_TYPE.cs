using System;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public sealed class MESSAGE_TYPE
	{
		private static readonly MESSAGE_TYPE[] Table;

		private static readonly List<MESSAGE_TYPE> List;

		private const byte EXTENDED = 128;

		public const byte NETWORK = 0;

		public const byte CIRCUIT_ID = 1;

		public const byte DEVICE_ID = 2;

		public const byte DEVICE_STATUS = 3;

		public const byte PRODUCT_STATUS = 6;

		public const byte TIME = 7;

		public const byte REQUEST = 128;

		public const byte RESPONSE = 129;

		public const byte COMMAND = 130;

		public const byte EXT_STATUS = 131;

		public const byte TEXT_CONSOLE = 132;

		public const byte GROUP_ID = 133;

		public const byte DAQ = 155;

		public const byte IOT = 157;

		public const byte BULK_XFER = 159;

		private static readonly MESSAGE_TYPE INVALID;

		private readonly byte Value;

		public readonly string Name;

		public bool IsBroadcast => (Value & 0x80) == 0;

		public bool IsPointToPoint => (Value & 0x80) != 0;

		public static IEnumerable<MESSAGE_TYPE> GetEnumerator()
		{
			return List;
		}

		static MESSAGE_TYPE()
		{
			Table = new MESSAGE_TYPE[256];
			List = new List<MESSAGE_TYPE>();
			INVALID = new MESSAGE_TYPE(-1, "INVALID");
			for (int i = 0; i < Table.Length; i++)
			{
				Table[i] = INVALID;
			}
			new MESSAGE_TYPE(0, "NETWORK");
			new MESSAGE_TYPE(1, "CIRCUIT_ID");
			new MESSAGE_TYPE(2, "DEVICE_ID");
			new MESSAGE_TYPE(3, "DEVICE_STATUS");
			new MESSAGE_TYPE(4, "RESERVED_100");
			new MESSAGE_TYPE(5, "RESERVED_101");
			new MESSAGE_TYPE(6, "PRODUCT_STATUS");
			new MESSAGE_TYPE(7, "TIME");
			new MESSAGE_TYPE(128, "REQUEST");
			new MESSAGE_TYPE(129, "RESPONSE");
			new MESSAGE_TYPE(130, "COMMAND");
			new MESSAGE_TYPE(131, "EXT_STATUS");
			new MESSAGE_TYPE(132, "TEXT_CONSOLE");
			new MESSAGE_TYPE(133, "GROUP_ID");
			new MESSAGE_TYPE(134, "RESERVED_00110");
			new MESSAGE_TYPE(135, "RESERVED_00111");
			new MESSAGE_TYPE(136, "RESERVED_01000");
			new MESSAGE_TYPE(137, "RESERVED_01001");
			new MESSAGE_TYPE(138, "RESERVED_01010");
			new MESSAGE_TYPE(139, "RESERVED_01011");
			new MESSAGE_TYPE(140, "RESERVED_01100");
			new MESSAGE_TYPE(141, "RESERVED_01101");
			new MESSAGE_TYPE(142, "RESERVED_01110");
			new MESSAGE_TYPE(143, "RESERVED_01111");
			new MESSAGE_TYPE(144, "RESERVED_10000");
			new MESSAGE_TYPE(145, "RESERVED_10001");
			new MESSAGE_TYPE(146, "RESERVED_10010");
			new MESSAGE_TYPE(147, "RESERVED_10011");
			new MESSAGE_TYPE(148, "RESERVED_10100");
			new MESSAGE_TYPE(149, "RESERVED_10101");
			new MESSAGE_TYPE(150, "RESERVED_10110");
			new MESSAGE_TYPE(151, "RESERVED_10111");
			new MESSAGE_TYPE(152, "RESERVED_11000");
			new MESSAGE_TYPE(153, "RESERVED_11001");
			new MESSAGE_TYPE(154, "RESERVED_11010");
			new MESSAGE_TYPE(155, "DAQ");
			new MESSAGE_TYPE(156, "RESERVED_11100");
			new MESSAGE_TYPE(157, "IOT");
			new MESSAGE_TYPE(158, "RESERVED_11110");
			new MESSAGE_TYPE(159, "BULK_XFER");
		}

		private MESSAGE_TYPE(int value, string name)
		{
			Value = (byte)value;
			Name = name.Trim();
			if (value >= 0)
			{
				Table[value] = this;
				List.Add(this);
			}
		}

		public static implicit operator byte(MESSAGE_TYPE msg)
		{
			if (msg == INVALID)
			{
				throw new InvalidCastException("Cannot convert MESSAGE_TYPE.INVALID to a byte");
			}
			return msg.Value;
		}

		public static implicit operator MESSAGE_TYPE(byte value)
		{
			return Table[value];
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
