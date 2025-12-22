using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class RemoteChannelCollection : IRemoteChannelCollection, IEnumerable<(IRemoteChannelDef channelDef, string channelId)>, IEnumerable
	{
		private const string LogTag = "RemoteChannelCollection";

		private readonly object _lock = new object();

		private readonly Dictionary<(Type channelType, string channelId), IRemoteChannelDef> _channelCollection = new Dictionary<(Type, string), IRemoteChannelDef>();

		public void Add(IRemoteChannelDef channel, string channelId)
		{
			lock (_lock)
			{
				Type type = channel.GetType();
				(Type, string) tuple = (type, channelId);
				if (_channelCollection.ContainsKey(tuple))
				{
					TaggedLog.Warning("RemoteChannelCollection", $"Replacing existing remote channel {type}:{channelId}");
				}
				_channelCollection[tuple] = channel;
			}
		}

		public void TryClearAndDisposeChannels()
		{
			lock (_lock)
			{
				foreach (KeyValuePair<(Type, string), IRemoteChannelDef> item in new Dictionary<(Type, string), IRemoteChannelDef>(_channelCollection))
				{
					item.Value.TryDispose();
					_channelCollection.TryRemove(item.Key);
				}
			}
		}

		public void TryRemoveAndDispose<TRemoteChannelDef>() where TRemoteChannelDef : IRemoteChannelDef
		{
			lock (_lock)
			{
				foreach (KeyValuePair<(Type, string), IRemoteChannelDef> item in new Dictionary<(Type, string), IRemoteChannelDef>(_channelCollection))
				{
					if (item.Value is TRemoteChannelDef val)
					{
						val.TryDispose();
						_channelCollection.TryRemove(item.Key);
					}
				}
			}
		}

		public TRemoteChannelDef GetRemoteChannelForChannelId<TRemoteChannelDef>(string channelId) where TRemoteChannelDef : IRemoteChannelDef
		{
			lock (_lock)
			{
				foreach (KeyValuePair<(Type, string), IRemoteChannelDef> item in _channelCollection)
				{
					if (item.Value is TRemoteChannelDef result && string.Equals(item.Key.Item2, channelId, StringComparison.Ordinal))
					{
						return result;
					}
				}
			}
			return default(TRemoteChannelDef);
		}

		[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__7))]
		public IEnumerator<(IRemoteChannelDef channelDef, string channelId)> GetEnumerator()
		{
			//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
			return new _003CGetEnumerator_003Ed__7(0)
			{
				_003C_003E4__this = this
			};
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
