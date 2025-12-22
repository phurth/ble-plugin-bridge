namespace IDS.Core.IDS_CAN
{
	public struct CAN_ID
	{
		public MESSAGE_TYPE MessageType { get; private set; }

		public ADDRESS SourceAddress { get; private set; }

		public ADDRESS TargetAddress { get; private set; }

		public byte MessageData { get; private set; }

		public CAN_ID(MESSAGE_TYPE type, ADDRESS source_address)
		{
			MessageType = type;
			SourceAddress = source_address;
			TargetAddress = ADDRESS.BROADCAST;
			MessageData = 0;
		}

		public CAN_ID(MESSAGE_TYPE type, ADDRESS source_address, ADDRESS target_address, byte ext_data)
		{
			MessageType = type;
			SourceAddress = source_address;
			TargetAddress = target_address;
			MessageData = ext_data;
		}

		public CAN_ID(CAN.ID id)
		{
			if (id.IsExtended)
			{
				uint value = id.Value;
				SourceAddress = (byte)(value >> 18);
				TargetAddress = (byte)(value >> 8);
				MessageData = (byte)value;
				MessageType = (byte)(0x80u | ((value >> 24) & 0x1Cu) | ((value >> 16) & 3u));
			}
			else
			{
				uint value2 = id.Value;
				SourceAddress = (byte)value2;
				MessageType = (byte)((value2 >> 8) & 7u);
				TargetAddress = ADDRESS.BROADCAST;
				MessageData = 0;
			}
		}

		public static implicit operator CAN_ID(CAN.ID id)
		{
			return new CAN_ID(id);
		}

		public static implicit operator CAN.ID(CAN_ID id)
		{
			if (id.MessageType.IsPointToPoint)
			{
				return new CAN.ID(0u | (uint)(((byte)id.MessageType & 0x1C) << 24) | (uint)((byte)id.SourceAddress << 18) | (uint)(((byte)id.MessageType & 3) << 16) | (uint)((byte)id.TargetAddress << 8) | id.MessageData, isExtended: true);
			}
			return new CAN.ID(0u | (uint)(((byte)id.MessageType & 7) << 8) | (byte)id.SourceAddress, isExtended: false);
		}
	}
}
