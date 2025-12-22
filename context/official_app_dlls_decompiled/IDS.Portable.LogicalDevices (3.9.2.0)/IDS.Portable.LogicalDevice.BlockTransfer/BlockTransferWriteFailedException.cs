using System;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	public class BlockTransferWriteFailedException : BlockTransferException
	{
		public BlockTransferBlockId BlockId { get; }

		public ILogicalDeviceTransferProgress Progress { get; }

		public BlockTransferWriteFailedException(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, ILogicalDeviceTransferProgress progress, Exception? innerException = null)
			: base($"Block Transfer {blockId} write failed {progress} for {logicalDevice}", innerException)
		{
			BlockId = blockId;
			Progress = progress;
		}
	}
}
