using System;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	public class BlockTransferException : Exception
	{
		public BlockTransferException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}
	}
}
