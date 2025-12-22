using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface IRemoteChannelDefSoftwareUpdate : IRemoteChannelDefStatus<SoftwareUpdateState>, IRemoteChannelDefStatus, IRemoteChannelDef, ICommonDisposable, IDisposable
	{
		Task<CommandResult> SendSoftwareUpdateAuthorizationAsync(CancellationToken cancelToken);
	}
}
