using System;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	[Flags]
	public enum BlockTransferPropertyFlags : byte
	{
		None = 0,
		Readable = 1,
		Writable = 2,
		RequiresMinSizeDataBuffer = 4,
		RequiresStartAddress = 8,
		RequiresSize = 0x10,
		AutomaticErase = 0x20,
		SafeToReboot = 0x40
	}
}
