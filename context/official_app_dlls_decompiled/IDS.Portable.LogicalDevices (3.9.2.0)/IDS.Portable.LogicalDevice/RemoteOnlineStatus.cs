using System.ComponentModel;

namespace IDS.Portable.LogicalDevice
{
	[DefaultValue(RemoteOnlineStatus.Offline)]
	public enum RemoteOnlineStatus
	{
		Offline,
		Online,
		Locked
	}
}
