using System.Collections.Generic;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSource
	{
		string DeviceSourceToken { get; }

		bool AllowAutoOfflineLogicalDeviceRemoval { get; }

		bool IsDeviceSourceActive { get; }

		IEnumerable<ILogicalDeviceTag> MakeDeviceSourceTags(ILogicalDevice? logicalDevice);
	}
}
