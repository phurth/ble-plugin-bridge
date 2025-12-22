using System;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	public class BlockTransferBlockTooSmallException : BlockTransferException
	{
		public BlockTransferBlockTooSmallException(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, int size, Exception? innerException = null)
			: base($"Block Transfer {blockId} too small {size} for {logicalDevice}", innerException)
		{
		}
	}
}
