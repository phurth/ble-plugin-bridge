using System;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface IRemoteChannelDefStatus : IRemoteChannelDef, ICommonDisposable, IDisposable
	{
	}
	public interface IRemoteChannelDefStatus<TValue> : IRemoteChannelDefStatus, IRemoteChannelDef, ICommonDisposable, IDisposable
	{
		event RemoteChannelReceivedUpdateStatusEventHandler<TValue> RemoteChannelReceivedUpdateStatusEvent;
	}
}
