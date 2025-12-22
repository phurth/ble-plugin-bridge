using System;
using System.Collections.Generic;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDeviceSource;

namespace OneControl.Direct.IdsCanAccessoryBle.GenericSensor
{
	public interface IDirectGenericSensorBle : ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirect, ILogicalDeviceSource, ILogicalDeviceSourceDirectPid, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectAccessoryHistoryData, ILogicalDeviceSourceDirectMetadata
	{
		IEnumerable<GenericSensorBle> GenericSensors { get; }

		bool RegisterGenericSensor(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName);

		void UnRegisterGenericSensor(Guid bleDeviceId);

		bool IsGenericSensorRegistered(Guid bleDeviceId);

		GenericSensorBle? GetGenericSensor(ILogicalDevice? logicalDevice);
	}
}
