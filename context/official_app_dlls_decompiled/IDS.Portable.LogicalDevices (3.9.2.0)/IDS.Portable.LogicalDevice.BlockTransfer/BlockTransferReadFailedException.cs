using System;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	public class BlockTransferReadFailedException : BlockTransferException
	{
		public BlockTransferReadFailedException(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, ILogicalDeviceTransferProgress progress, Exception? innerException = null)
			: base($"Block Transfer {blockId} read failed {progress} for {logicalDevice}", innerException)
		{
		}
	}
}
