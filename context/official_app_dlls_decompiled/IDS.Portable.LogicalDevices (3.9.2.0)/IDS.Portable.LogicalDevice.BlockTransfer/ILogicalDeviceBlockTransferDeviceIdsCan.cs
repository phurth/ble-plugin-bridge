using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice.BlockTransfer
{
	public interface ILogicalDeviceBlockTransferDeviceIdsCan : ILogicalDeviceBlockTransferDevice, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		int BulkTransferDelayMs(BlockTransferBlockId blockId);
	}
}
