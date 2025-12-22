using System;

namespace IDS.Core.IDS_CAN
{
	[Flags]
	public enum BLOCK_FLAGS : byte
	{
		NONE = 0,
		READABLE = 1,
		WRITABLE = 2,
		USE_BUFFER_MINIMUM_SIZE = 4,
		USE_SET_START_ADDRESS = 8,
		USE_SET_SIZE = 0x10,
		USE_ERASE_DISABLED = 0x20
	}
}
