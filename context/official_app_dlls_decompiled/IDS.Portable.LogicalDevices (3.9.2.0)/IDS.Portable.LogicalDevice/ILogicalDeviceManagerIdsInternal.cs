using System;
using IDS.Portable.Common;
using IDS.Portable.Common.ObservableCollection;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceManagerIdsInternal : ILogicalDeviceManager, ICommonDisposable, IDisposable, IContainerDataSource, IContainerDataSourceBase
	{
		object AddRemoveLock { get; }

		(ILogicalDevice? logicalDevice, bool isNew) AddLogicalDeviceInternal(ILogicalDeviceId logicalDeviceId, byte? rawCapability, ILogicalDeviceSource? deviceSource, Func<ILogicalDevice, bool> isAttemptAutoRenameEnabled);
	}
}
