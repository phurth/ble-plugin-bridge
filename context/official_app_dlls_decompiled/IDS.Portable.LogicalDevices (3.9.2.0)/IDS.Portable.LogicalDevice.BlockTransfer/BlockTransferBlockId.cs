using System.ComponentModel;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	[DefaultValue(BlockTransferBlockId.Unknown)]
	public enum BlockTransferBlockId : ushort
	{
		Unknown,
		Generic,
		MonitorPanel,
		Reflash
	}
}
