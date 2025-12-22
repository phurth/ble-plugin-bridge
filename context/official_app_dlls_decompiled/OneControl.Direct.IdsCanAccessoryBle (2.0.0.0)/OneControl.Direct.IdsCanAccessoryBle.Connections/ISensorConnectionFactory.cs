using System;
using System.Collections.Generic;
using IDS.Core.IDS_CAN;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public interface ISensorConnectionFactory
	{
		DEVICE_TYPE DeviceType { get; }

		bool IsStandardSource { get; }

		ILogicalDeviceSourceDirect DeviceSource { get; }

		Type SensorConnectionType { get; }

		IEnumerable<ISensorConnection> SensorConnectionsAll { get; }

		(ISensorConnection? SensorConnection, bool NewRegistration) TryAddSensorConnection(IAccessoryScanResult accessoryScanResult);

		bool TryAddSensorConnection(ISensorConnection sensorConnection);

		bool TryRemoveSensorConnection(ISensorConnection sensorConnection);
	}
}
