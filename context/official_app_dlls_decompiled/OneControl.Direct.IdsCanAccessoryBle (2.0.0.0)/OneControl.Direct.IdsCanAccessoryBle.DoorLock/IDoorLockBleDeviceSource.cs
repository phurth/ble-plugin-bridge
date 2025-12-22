using System;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDeviceSource;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.DoorLock
{
	public interface IDoorLockBleDeviceSource : IAccessoryBleDeviceSourceLocap<SensorConnectionDoorLock>, IAccessoryBleDeviceSourceLocap, ILogicalDeviceSourceDirect, ILogicalDeviceSource, IAccessoryBleDeviceSource<SensorConnectionDoorLock>, IAccessoryBleDeviceSource, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata, ILogicalDeviceSourceDirectPid, ILogicalDeviceSourceDirectAccessoryHistoryData, IAccessoryBleDeviceSourceDevices<DoorLockBleDeviceDriver>
	{
	}
}
