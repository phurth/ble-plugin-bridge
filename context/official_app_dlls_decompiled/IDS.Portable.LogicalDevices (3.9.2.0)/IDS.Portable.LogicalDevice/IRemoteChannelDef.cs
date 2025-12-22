using System;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface IRemoteChannelDef : ICommonDisposable, IDisposable
	{
		ILogicalDeviceRemote LogicalDevice { get; }

		string ChannelId { get; }
	}
	public interface IRemoteChannelDef<TValue> : IRemoteChannelDefStatus<TValue>, IRemoteChannelDefStatus, IRemoteChannelDef, ICommonDisposable, IDisposable, IRemoteChannelDefCommand<TValue>, IRemoteChannelDefCommand
	{
	}
}
