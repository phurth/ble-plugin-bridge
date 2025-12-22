namespace IDS.Core.IDS_CAN
{
	internal class SetBlock : IBlock
	{
		public IDevice Device { get; private set; }

		public BLOCK_ID ID { get; private set; }

		public BLOCK_FLAGS Flags { get; private set; }

		public ulong Capacity { get; private set; }

		public ulong Size { get; private set; }

		public uint CRC { get; set; }

		public ulong StartAddress { get; set; }

		public ulong SetSize { get; set; }

		public SESSION_ID ReadSessionID { get; private set; }

		public SESSION_ID WriteSessionID { get; private set; }

		public SetBlock(IDevice device, BLOCK_ID id, BLOCK_FLAGS flags, SESSION_ID read_id, SESSION_ID write_id, ulong capacity, ulong size, uint crc, ulong startaddress)
		{
			Device = device;
			ID = id;
			Flags = flags;
			ReadSessionID = read_id;
			WriteSessionID = write_id;
			Capacity = capacity;
			Size = size;
			CRC = crc;
			StartAddress = startaddress;
		}
	}
}
