using System;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	public class BlockTransferResponseFailureException : BlockTransferException
	{
		public BlockTransferResponseFailureException(RESPONSE response, Exception? innerException = null)
			: base($"Block Transfer received a response that wasn't success: {response} ", innerException)
		{
		}

		public BlockTransferResponseFailureException(Exception? innerException = null)
			: base("Block Transfer received a response that was null. ", innerException)
		{
		}
	}
}
