using System;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	[Flags]
	public enum BlockTransferStopOptions : byte
	{
		None = 0,
		Read = 1,
		Write = 2,
		Reset = 4
	}
}
