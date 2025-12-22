using System;
using System.Collections.Generic;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public interface IAccessoryBleDeviceSourceDevices<out TAccessoryBleDeviceDriver> : IAccessoryBleDeviceSource, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirect, ILogicalDeviceSource, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata where TAccessoryBleDeviceDriver : IDisposable
	{
		IEnumerable<TAccessoryBleDeviceDriver> SensorDevices { get; }

		TAccessoryBleDeviceDriver? GetSensorDevice(ILogicalDevice? logicalDevice);
	}
}
