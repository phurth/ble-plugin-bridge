using System.ComponentModel;
using IDS.Portable.LogicalDevice.Json;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceStatusExtended<out TStatusSerializable> : ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged where TStatusSerializable : ILogicalDeviceStatusExtendedSerializable
	{
		TStatusSerializable CopyAsSerializable();
	}
}
