using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceCommandRunnerIdsCan : INotifyPropertyChanged, ICommonDisposable, IDisposable
	{
		bool HasQueuedCommands { get; }

		bool IsRunningCommands { get; }

		bool CommandSessionActivated { get; }

		Task CommandActivateSession(CancellationToken cancelToken, bool activateSessionNow = true);

		void CommandDeactivateSession(bool closeSession = true);

		Task<CommandResult> SendCommandAsync(IDeviceCommandPacket dataPacket, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl>? cmdControl = null, CommandSendOption options = CommandSendOption.None);

		Task<CommandResult> SendCommandAsync(byte commandByte, byte[] data, uint dataSize, int responseTimeMs, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl>? cmdControl = null, CommandSendOption options = CommandSendOption.None);
	}
}
