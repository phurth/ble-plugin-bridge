using System;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	public class BlockTransferNotSupportedException : BlockTransferException
	{
		public BlockTransferNotSupportedException(ILogicalDevice logicalDevice, Exception? innerException = null)
			: base($"Block Transfer Not Supported by Device {logicalDevice}", innerException)
		{
		}

		public BlockTransferNotSupportedException(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, Exception? innerException = null)
			: base($"Block Transfer Not Supported by Device {logicalDevice} for {blockId}", innerException)
		{
		}
	}
}
