using System;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	[Flags]
	public enum BlockTransferStartOptions : byte
	{
		None = 0,
		Read = 1,
		Write = 2,
		StartAddress = 4,
		Size = 8,
		Erase = 0x10
	}
}
