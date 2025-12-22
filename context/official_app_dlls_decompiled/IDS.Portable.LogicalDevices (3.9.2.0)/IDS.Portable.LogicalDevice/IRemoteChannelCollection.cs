using System.Collections;
using System.Collections.Generic;

namespace IDS.Portable.LogicalDevice
{
	public interface IRemoteChannelCollection : IEnumerable<(IRemoteChannelDef channelDef, string channelId)>, IEnumerable
	{
		void Add(IRemoteChannelDef channel, string channelId);

		void TryClearAndDisposeChannels();

		void TryRemoveAndDispose<TRemoteChannelDef>() where TRemoteChannelDef : IRemoteChannelDef;

		TRemoteChannelDef GetRemoteChannelForChannelId<TRemoteChannelDef>(string channelId) where TRemoteChannelDef : IRemoteChannelDef;
	}
}
