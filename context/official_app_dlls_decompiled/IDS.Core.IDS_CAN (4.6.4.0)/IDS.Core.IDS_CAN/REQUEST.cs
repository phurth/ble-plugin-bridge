using System;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public sealed class REQUEST
	{
		private static readonly REQUEST[] Table;

		private static readonly List<REQUEST> List;

		public static readonly REQUEST INVALID;

		public const byte PART_NUMBER_READ = 0;

		public const byte MUTE_DEVICE = 1;

		public const byte IN_MOTION_LOCKOUT = 2;

		public const byte SOFTWARE_UPDATE_AUTHORIZATION = 3;

		public const byte NOTIFICATION_ALERT = 4;

		public const byte PID_READ_LIST = 16;

		public const byte PID_READ_WRITE = 17;

		public const byte GET_PID_PROPERTIES = 18;

		public const byte READ_BLOCK_LIST = 32;

		public const byte READ_BLOCK_PROPERTIES = 33;

		public const byte READ_BLOCK_DATA = 34;

		public const byte BEGIN_BLOCK_WRITE = 35;

		public const byte BEGIN_BLOCK_WRITE_BULK_XFER = 36;

		public const byte END_BLOCK_BULK_XFER = 37;

		public const byte END_BLOCK_WRITE = 38;

		public const byte SET_BLOCK_ADDRESS = 39;

		public const byte SET_BLOCK_SIZE = 40;

		public const byte READ_CONTINUOUS_DTCS = 48;

		public const byte CONTINUOUS_DTC_COMMAND = 49;

		public const byte SESSION_READ_LIST = 64;

		public const byte SESSION_READ_STATUS = 65;

		public const byte SESSION_REQUEST_SEED = 66;

		public const byte SESSION_TRANSMIT_KEY = 67;

		public const byte SESSION_HEARTBEAT = 68;

		public const byte SESSION_END = 69;

		public const byte IDS_CAN_REQUEST_DAQ_NUM_CHANNELS = 81;

		public const byte IDS_CAN_REQUEST_DAQ_AUTO_TX_SETTINGS = 82;

		public const byte IDS_CAN_REQUEST_DAQ_CHANNEL_SETTINGS = 83;

		public const byte IDS_CAN_REQUEST_DAQ_PID_ADDRESS = 84;

		public const byte IDS_CAN_REQUEST_LEVELER_TYPE_5_CONTROL = 96;

		public readonly byte Value;

		public readonly string Name;

		public bool IsValid => this != INVALID;

		public static IEnumerable<REQUEST> GetEnumerator()
		{
			return List;
		}

		static REQUEST()
		{
			Table = new REQUEST[256];
			List = new List<REQUEST>();
			INVALID = new REQUEST(-1, "INVALID");
			new REQUEST(0, "PART_NUMBER_READ");
			new REQUEST(1, "MUTE_DEVICE");
			new REQUEST(2, "IN_MOTION_LOCKOUT");
			new REQUEST(3, "SOFTWARE_UPDATE_AUTHORIZATION");
			new REQUEST(4, "NOTIFICATION_ALERT");
			new REQUEST(16, "PID_READ_LIST");
			new REQUEST(17, "PID_READ_WRITE");
			new REQUEST(32, "BLOCK_READ_LIST");
			new REQUEST(33, "BLOCK_READ_PROPERTIES");
			new REQUEST(34, "BLOCK_READ_DATA");
			new REQUEST(35, "BEGIN_BLOCK_WRITE");
			new REQUEST(36, "BEGIN_BLOCK_WRITE_BULK_XFER");
			new REQUEST(37, "BLOCK_END_BULK_XFER");
			new REQUEST(38, "END_BLOCK_WRITE");
			new REQUEST(39, "SET_BLOCK_ADDRESS");
			new REQUEST(40, "SET_BLOCK_SIZE");
			new REQUEST(48, "READ_CONTINUOUS_DTCS");
			new REQUEST(49, "CONTINUOUS_DTC_COMMAND");
			new REQUEST(64, "SESSION_READ_LIST");
			new REQUEST(65, "SESSION_READ_STATUS");
			new REQUEST(66, "SESSION_REQUEST_SEED");
			new REQUEST(67, "SESSION_TRANSMIT_KEY");
			new REQUEST(68, "SESSION_HEARTBEAT");
			new REQUEST(69, "SESSION_END");
			new REQUEST(81, "IDS_CAN_REQUEST_DAQ_NUM_CHANNELS");
			new REQUEST(82, "IDS_CAN_REQUEST_DAQ_AUTO_TX_SETTINGS");
			new REQUEST(83, "IDS_CAN_REQUEST_DAQ_CHANNEL_SETTINGS");
			new REQUEST(84, "IDS_CAN_REQUEST_DAQ_PID_ADDRESS");
			new REQUEST(96, "IDS_CAN_REQUEST_LEVELER_TYPE_5_CONTROL");
			for (int i = 0; i < Table.Length; i++)
			{
				if (Table[i] == null)
				{
					Table[i] = new REQUEST((byte)i, "UNKNOWN_" + ((byte)i).HexString());
				}
			}
		}

		private REQUEST(int value, string name)
		{
			Name = name.Trim();
			Value = (byte)value;
			if (value >= 0)
			{
				List.Add(this);
				Table[value] = this;
			}
		}

		public static implicit operator byte(REQUEST msg)
		{
			if (msg == INVALID)
			{
				throw new InvalidCastException("Cannot convert REQUEST.INVALID to a byte");
			}
			return msg.Value;
		}

		public static implicit operator REQUEST(byte value)
		{
			return Table[value];
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
