using System;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	public class BlockTransferOperationNotSupportedException : BlockTransferException
	{
		public BlockTransferOperationNotSupportedException(Exception? innerException = null)
			: base("This Block Transfer operation is not supported.", innerException)
		{
		}
	}
}
