using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	public interface IBlockTransfer
	{
		Task<IReadOnlyList<BlockTransferBlockId>> GetDeviceBlockListAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken);

		Task<BlockTransferPropertyFlags> GetDeviceBlockPropertyFlagsAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken);

		Task<LogicalDeviceSessionType> GetDeviceBlockPropertyReadSessionIdAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken);

		Task<LogicalDeviceSessionType> GetDeviceBlockPropertyWriteSessionIdAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken);

		Task<ulong> GetDeviceBlockCapacityAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken);

		Task<ulong> GetDeviceBlockSizeAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken);

		Task<uint> GetDeviceBlockCrcAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, bool recalculate, CancellationToken cancellationToken);

		Task<uint> GetDeviceBlockStartAddressAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken);

		Task StartDeviceBlockTransferAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, BlockTransferStartOptions options, CancellationToken cancellationToken, uint? startAddress = null, uint? size = null);

		Task StopDeviceBlockTransferAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, BlockTransferStopOptions options, CancellationToken cancellationToken);

		Task DeviceBlockWriteAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, IReadOnlyList<byte> data, Func<ILogicalDeviceTransferProgress, bool> progressAck, CancellationToken cancellationToken);

		Task<IReadOnlyList<byte>> DeviceBlockReadAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken);
	}
}
