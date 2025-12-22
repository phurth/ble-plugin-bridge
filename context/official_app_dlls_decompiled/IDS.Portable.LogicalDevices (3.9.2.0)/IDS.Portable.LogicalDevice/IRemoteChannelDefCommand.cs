using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface IRemoteChannelDefCommand : IRemoteChannelDef, ICommonDisposable, IDisposable
	{
	}
	public interface IRemoteChannelDefCommand<in TValue> : IRemoteChannelDefCommand, IRemoteChannelDef, ICommonDisposable, IDisposable
	{
		Task<CommandResult> SendCommandToRemoteAsync(TValue channelCommand, CancellationToken cancelToken);
	}
}
