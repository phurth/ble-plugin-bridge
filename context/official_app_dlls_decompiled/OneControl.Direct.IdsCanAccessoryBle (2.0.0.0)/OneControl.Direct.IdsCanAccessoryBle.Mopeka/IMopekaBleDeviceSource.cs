using System.Collections.Generic;
using IDS.Core.IDS_CAN;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.Mopeka
{
	public interface IMopekaBleDeviceSource : ILogicalDeviceExSnapshot, ILogicalDeviceExOnline, ILogicalDeviceEx, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirect, ILogicalDeviceSource, ILogicalDeviceSourceDirectMetadata
	{
		IEnumerable<ISensorConnection> SensorConnectionsAll { get; }

		bool LinkMopekaSensor(SensorConnectionMopeka sensorConnection);

		bool UnlinkMopekaSensor(MAC macAddress);

		MopekaSensor GetSensor(MAC macAddress);

		bool IsMopekaSensorLinked(MAC macAddress);
	}
}
