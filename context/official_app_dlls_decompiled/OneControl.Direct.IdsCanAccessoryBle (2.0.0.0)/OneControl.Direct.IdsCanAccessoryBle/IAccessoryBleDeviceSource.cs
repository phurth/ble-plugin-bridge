using System;
using System.Collections.Generic;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public interface IAccessoryBleDeviceSource : ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirect, ILogicalDeviceSource, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata
	{
		IEnumerable<ISensorConnection> SensorConnectionsAll { get; }

		void UnRegisterSensor(Guid bleDeviceId);

		bool IsSensorRegistered(Guid bleDeviceId);
	}
	public interface IAccessoryBleDeviceSource<in TSensorConnectionBle> : IAccessoryBleDeviceSource, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirect, ILogicalDeviceSource, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata where TSensorConnectionBle : ISensorConnectionBle
	{
		bool RegisterSensor(TSensorConnectionBle sensorConnection);
	}
}
