using IDS.Core.IDS_CAN;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	public static class BlockTransferIdExtension
	{
		public static BLOCK_ID ToBlockId(this BlockTransferBlockId blockId)
		{
			return (ushort)blockId;
		}

		public static BlockTransferBlockId ToBlockId(this BLOCK_ID id)
		{
			return Enum<BlockTransferBlockId>.TryConvert((int)(ushort)id);
		}
	}
}
