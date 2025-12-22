using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public abstract class LogicalDeviceCommandRunnerIdsCanSim<TSimDeviceStatus> : CommonDisposable, ILogicalDeviceCommandRunnerIdsCan, INotifyPropertyChanged, ICommonDisposable, IDisposable
	{
		protected TSimDeviceStatus SimDeviceStatus;

		private bool _commandSessionActivated;

		public bool IsRunningCommands => false;

		public bool HasQueuedCommands => false;

		public bool CommandSessionActivated
		{
			get
			{
				return _commandSessionActivated;
			}
			set
			{
				bool num = _commandSessionActivated != value;
				_commandSessionActivated = value;
				if (num)
				{
					this.NotifyMainThread(this.PropertyChanged, "CommandSessionActivated");
				}
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected LogicalDeviceCommandRunnerIdsCanSim(TSimDeviceStatus deviceStatus)
		{
			SimDeviceStatus = deviceStatus;
		}

		public Task CommandActivateSession(CancellationToken cancelToken, bool activateSessionNow = true)
		{
			CommandSessionActivated = true;
			return Task.FromResult(0);
		}

		public void CommandDeactivateSession(bool closeSession = true)
		{
			CommandSessionActivated = false;
		}

		public Task<CommandResult> SendCommandAsync(IDeviceCommandPacket dataPacket, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl>? cmdControl = null, CommandSendOption options = CommandSendOption.None)
		{
			return SendCommandAsync(dataPacket.CommandByte, dataPacket.CopyCurrentData(), dataPacket.Size, dataPacket.CommandResponseTimeMs, cancelToken, cmdControl, options);
		}

		public abstract Task<CommandResult> SendCommandAsync(byte commandByte, byte[] data, uint dataSize, int responseTimeMs, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl>? cmdControl = null, CommandSendOption options = CommandSendOption.None);

		public override void Dispose(bool disposing)
		{
			CommandDeactivateSession();
		}
	}
}
