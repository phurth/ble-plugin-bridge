using System.ComponentModel;
using IDS.Portable.LogicalDevice.Json;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceStatus : IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged
	{
	}
	public interface ILogicalDeviceStatus<out TStatusSerializable> : ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged where TStatusSerializable : ILogicalDeviceStatusSerializable
	{
		TStatusSerializable CopyAsSerializable();
	}
}
