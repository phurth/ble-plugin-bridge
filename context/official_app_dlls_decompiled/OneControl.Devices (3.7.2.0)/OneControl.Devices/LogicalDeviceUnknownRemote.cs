using System;
using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Remote;

namespace OneControl.Devices
{
	public class LogicalDeviceUnknownRemote : LogicalDeviceUnknown, ILogicalDeviceRemote, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		public RemoteOnline RemoteOnline { get; protected set; }

		public override bool IsRemoteAccessAvailable => DeviceService.RemoteManager?.IsRemoteAccessAvailable(this) ?? false;

		public IRemoteChannelDefOnline RemoteOnlineChannel => RemoteOnline?.Channel;

		public LogicalDeviceUnknownRemote(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
			: base(service, logicalDeviceId, rawCapability)
		{
			RemoteOnline = new RemoteOnline(this, RemoteChannels);
		}

		public override void Dispose(bool disposing)
		{
			RemoteOnline?.TryDispose();
			RemoteOnline = null;
			base.Dispose(disposing);
		}

		TRemoteChannelDef ILogicalDeviceRemote.GetRemoteChannelForChannelId<TRemoteChannelDef>(string channelId)
		{
			return GetRemoteChannelForChannelId<TRemoteChannelDef>(channelId);
		}
	}
}
