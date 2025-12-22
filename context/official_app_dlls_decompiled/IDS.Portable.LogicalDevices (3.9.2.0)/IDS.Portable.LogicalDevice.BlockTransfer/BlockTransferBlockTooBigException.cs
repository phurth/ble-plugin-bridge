using System;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	public class BlockTransferBlockTooBigException : BlockTransferException
	{
		public BlockTransferBlockTooBigException(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, int size, Exception? innerException = null)
			: base($"Block Transfer {blockId} too big {size} for {logicalDevice}", innerException)
		{
		}
	}
}
