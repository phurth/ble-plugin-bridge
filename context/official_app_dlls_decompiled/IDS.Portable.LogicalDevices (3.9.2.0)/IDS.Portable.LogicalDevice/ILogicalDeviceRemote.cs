using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceRemote : ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		IRemoteChannelDefOnline RemoteOnlineChannel { get; }

		TRemoteChannelDef GetRemoteChannelForChannelId<TRemoteChannelDef>(string channelId) where TRemoteChannelDef : IRemoteChannelDef;

		void UpdateRemoteAccessAvailable();
	}
}
