using System;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface IRemoteChannelDefOnline : IRemoteChannelDefStatus<RemoteOnlineStatus>, IRemoteChannelDefStatus, IRemoteChannelDef, ICommonDisposable, IDisposable
	{
		RemoteOnlineStatus RemoteOnlineStatus { get; }

		bool IsRemoteOnline { get; }

		bool IsRemoteLocked { get; }

		void UpdateRemoteChannelOnlineStatus(bool channelOnline);

		void UpdateRemoteChannelOnlineStatus(RemoteOnlineStatus onlineStatus);
	}
}
