using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevices;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceCommandRunnerIdsCan : LogicalDeviceCommandRunnerIdsCan<ILogicalDevice>
	{
		public LogicalDeviceCommandRunnerIdsCan(ILogicalDevice logicalDevice)
			: base(logicalDevice)
		{
		}
	}
	public class LogicalDeviceCommandRunnerIdsCan<TLogicalDevice> : CommonDisposable, ILogicalDeviceCommandRunnerIdsCan, INotifyPropertyChanged, ICommonDisposable, System.IDisposable where TLogicalDevice : class, ILogicalDevice
	{
		private const string LogTag = "LogicalDeviceCommandRunnerIdsCan";

		public uint SessionKeepAliveTime;

		public uint SessionGetTimeout = 3000u;

		public uint CommandProcessingTime = 3000u;

		private bool _commandSessionActivated;

		private CommandTrackerIdsCan? _queuedCommand;

		private CommandTrackerIdsCan? _runningCommand;

		public TLogicalDevice LogicalDevice { get; }

		public uint RetryInterval => 50u;

		public bool CommandSessionActivated
		{
			get
			{
				return _commandSessionActivated;
			}
			set
			{
				this.UpdateAndThenNotifyMainThreadIfNeeded(ref _commandSessionActivated, value, this.PropertyChanged, "CommandSessionActivated");
			}
		}

		public bool IsRunningCommands { get; protected set; }

		public bool HasQueuedCommands => _queuedCommand != null;

		public event PropertyChangedEventHandler? PropertyChanged;

		public LogicalDeviceCommandRunnerIdsCan(TLogicalDevice logicalDevice)
		{
			LogicalDevice = logicalDevice ?? throw new ArgumentNullException("logicalDevice");
		}

		public async Task CommandActivateSession(CancellationToken cancelToken, bool activateSessionNow = true)
		{
			CommandSessionActivated = true;
			if (!activateSessionNow)
			{
				return;
			}
			try
			{
				if (await LogicalDevice.SessionManager.ActivateSessionAsync(LogicalDeviceSessionType.RemoteControl, LogicalDevice, cancelToken, SessionKeepAliveTime, SessionGetTimeout) == null)
				{
					throw new Exception("Unable to get Session Client");
				}
			}
			catch (ActivateSessionRemoteActiveException)
			{
			}
			catch (ActivateSessionDisabledException)
			{
			}
			catch (SessionManagerNotAvailableException ex3)
			{
				if (LogicalDevice.ActiveConnection != LogicalDeviceActiveConnection.Remote)
				{
					TaggedLog.Error("LogicalDeviceCommandRunnerIdsCan", $"CommandActivateSession - Unable to active session for {LogicalDevice}: {ex3.Message}");
					throw;
				}
			}
			catch (Exception ex4)
			{
				TaggedLog.Error("LogicalDeviceCommandRunnerIdsCan", $"CommandActivateSession - Unable to active session for {LogicalDevice}: {ex4.Message}");
				throw;
			}
		}

		public void CommandDeactivateSession(bool closeSession = true)
		{
			try
			{
				CommandSessionActivated = false;
				LogicalDevice.SessionManager.DeactivateSession(LogicalDeviceSessionType.RemoteControl, LogicalDevice, closeSession);
			}
			catch (SessionManagerNotAvailableException)
			{
			}
			catch (Exception ex2)
			{
				TaggedLog.Debug("LogicalDeviceCommandRunnerIdsCan", $"CommandDeactivateSession - Unable to deactivate session for {LogicalDevice}: {ex2}\n{ex2.StackTrace}");
			}
		}

		public virtual Task<CommandResult> SendCommandAsync(IDeviceCommandPacket dataPacket, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl>? cmdControl = null, CommandSendOption options = CommandSendOption.None)
		{
			return SendCommandAsync(dataPacket.CommandByte, dataPacket.CopyCurrentData(), dataPacket.Size, dataPacket.CommandResponseTimeMs, cancelToken, cmdControl, options);
		}

		public Task<CommandResult> SendCommandAsync(byte commandByte, byte[] data, uint dataSize, int responseTimeMs, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl>? cmdControl = null, CommandSendOption options = CommandSendOption.None)
		{
			if (cancelToken.IsCancellationRequested)
			{
				return Task.FromResult(CommandResult.Canceled);
			}
			TaskCompletionSource<CommandResult> taskCompletionSource;
			lock (this)
			{
				if (base.IsDisposed)
				{
					TaggedLog.Error("LogicalDeviceCommandRunnerIdsCan", $"{LogicalDevice} Can't send new command as command running has been disposed");
					return Task.FromResult(CommandResult.Canceled);
				}
				if (LogicalDevice.ActiveConnection == LogicalDeviceActiveConnection.Offline)
				{
					return Task.FromResult(CommandResult.ErrorDeviceOffline);
				}
				if (LogicalDevice.ActiveConnection != LogicalDeviceActiveConnection.Direct)
				{
					return Task.FromResult(CommandResult.ErrorRemoteOperationNotSupported);
				}
				if (LogicalDevice.ShouldAutoClearInTransitLockout && !options.HasFlag(CommandSendOption.AutoClearLockoutDisabled))
				{
					((LogicalDevice.DeviceService.DeviceSourceManager.GetPrimaryDeviceSource<ILogicalDeviceSourceDirectConnectionIdsCan>(LogicalDevice)?.Gateway)?.LocalHost)?.SendDisableInMotionLockoutCommand();
				}
				taskCompletionSource = new TaskCompletionSource<CommandResult>();
				byte[] array = new byte[data.Length];
				if (data.Length != 0)
				{
					Buffer.BlockCopy(data, 0, array, 0, data.Length);
				}
				CommandTrackerIdsCan queuedCommand = new CommandTrackerIdsCan(commandByte, array, cancelToken, cmdControl, taskCompletionSource, responseTimeMs);
				if (_queuedCommand != null)
				{
					bool flag = data.Length != _queuedCommand!.Data.Length;
					int num = 0;
					while (!flag && num < data.Length)
					{
						if (data[num] != _queuedCommand!.Data[num])
						{
							flag = true;
						}
						num++;
					}
					_queuedCommand!.Result.SetResult(flag ? CommandResult.Canceled : CommandResult.CanceledWithSameCommand);
					if (flag)
					{
						TaggedLog.Debug("LogicalDeviceCommandRunnerIdsCan", $"{LogicalDevice} Replacing queued command with different command.");
					}
					else
					{
						TaggedLog.Debug("LogicalDeviceCommandRunnerIdsCan", $"{LogicalDevice} Replacing queued command with same command data.");
					}
				}
				_queuedCommand = queuedCommand;
				if (options.HasFlag(CommandSendOption.CancelCurrentCommand))
				{
					CommandTrackerIdsCan runningCommand = _runningCommand;
					if (runningCommand != null)
					{
						runningCommand.CommandReplaced = true;
					}
				}
				StartCommandRunnerTaskIfNeeded();
			}
			return taskCompletionSource.Task;
		}

		private void StartCommandRunnerTaskIfNeeded()
		{
			lock (this)
			{
				if (base.IsDisposed || IsRunningCommands)
				{
					return;
				}
				IsRunningCommands = true;
			}
			Task.Run((Func<Task>)CommandRunnerTaskAsync);
		}

		private async Task CommandRunnerTaskAsync()
		{
			while (true)
			{
				lock (this)
				{
					if (IsRunningCommands)
					{
						if (_runningCommand == null)
						{
							_runningCommand = _queuedCommand;
							_queuedCommand = null;
						}
						if (_runningCommand != null)
						{
							goto IL_0071;
						}
					}
				}
				break;
				IL_0071:
				if (_runningCommand!.SendCommand)
				{
					if (base.IsDisposed)
					{
						_runningCommand!.Result.SetResult(CommandResult.Canceled);
						_runningCommand = null;
						continue;
					}
					if (_runningCommand!.CommandReplaced)
					{
						_runningCommand!.Result.SetResult(CommandResult.Canceled);
						_runningCommand = null;
						continue;
					}
					CommandResult commandResult = await SendCommandAsync(_runningCommand!.CancelToken, _runningCommand!.CommandByte, _runningCommand!.Data);
					if (commandResult != 0)
					{
						_runningCommand!.Result.SetResult(commandResult);
						_runningCommand = null;
						continue;
					}
				}
				if (_runningCommand!.CmdControl == null)
				{
					_runningCommand!.Result.SetResult(CommandResult.Completed);
					_runningCommand = null;
					continue;
				}
				if (base.IsDisposed)
				{
					_runningCommand!.Result.SetResult(CommandResult.Canceled);
					_runningCommand = null;
					continue;
				}
				if (_runningCommand!.ResponseTimeMs > 0)
				{
					await TaskExtension.TryDelay(_runningCommand!.ResponseTimeMs, _runningCommand!.CancelToken);
				}
				if (_runningCommand!.CancelToken.IsCancellationRequested)
				{
					_runningCommand!.Result.SetResult(CommandResult.Canceled);
					_runningCommand = null;
					continue;
				}
				try
				{
					CommandControl commandControl = _runningCommand!.CmdControl!(LogicalDevice);
					switch (commandControl)
					{
					case CommandControl.Cancel:
						_runningCommand!.Result.SetResult(CommandResult.Canceled);
						_runningCommand = null;
						break;
					case CommandControl.Completed:
						_runningCommand!.Result.SetResult(CommandResult.Completed);
						_runningCommand = null;
						break;
					case CommandControl.WaitAndResend:
					case CommandControl.WaitNoResend:
						if (_runningCommand!.CommandReplaced)
						{
							_runningCommand!.Result.SetResult(CommandResult.Canceled);
							_runningCommand = null;
						}
						else if (_runningCommand!.GetCommandRunningTime() >= (double)CommandProcessingTime)
						{
							_runningCommand!.Result.SetResult(CommandResult.ErrorCommandTimeout);
							_runningCommand = null;
						}
						else
						{
							_runningCommand!.SendCommand = commandControl == CommandControl.WaitAndResend;
							await Task.Delay((int)RetryInterval, _runningCommand!.CancelToken);
						}
						break;
					}
				}
				catch (TimeoutException)
				{
					_runningCommand?.Result.SetResult(CommandResult.ErrorCommandTimeout);
					_runningCommand = null;
				}
				catch (OperationCanceledException)
				{
					_runningCommand?.Result.SetResult(CommandResult.Canceled);
					_runningCommand = null;
				}
				catch
				{
					_runningCommand?.Result.SetResult(CommandResult.ErrorOther);
					_runningCommand = null;
				}
			}
			_runningCommand?.Result.TrySetResult(CommandResult.Canceled);
			lock (this)
			{
				IsRunningCommands = false;
				_queuedCommand?.Result.TrySetResult(CommandResult.Canceled);
				_queuedCommand = null;
			}
		}

		private async Task<CommandResult> SendCommandAsync(CancellationToken cancelToken, byte commandByte, byte[] dataToSend)
		{
			if (!(LogicalDevice.DeviceService?.GetPrimaryDeviceSourceDirect(LogicalDevice) is ILogicalDeviceSourceDirectConnectionIdsCan logicalDeviceSourceDirectConnectionIdsCan))
			{
				return CommandResult.ErrorDeviceOffline;
			}
			IRemoteDevice physicalDevice = logicalDeviceSourceDirectConnectionIdsCan.FindRemoteDevice(LogicalDevice);
			if (physicalDevice == null || !physicalDevice.IsOnline)
			{
				TaggedLog.Debug("LogicalDeviceCommandRunnerIdsCan", $"{LogicalDevice} Can't find physical device to send command");
				return CommandResult.ErrorDeviceOffline;
			}
			try
			{
				if (await LogicalDevice.SessionManager.ActivateSessionAsync(LogicalDeviceSessionType.RemoteControl, LogicalDevice, cancelToken, SessionKeepAliveTime, SessionGetTimeout) == null)
				{
					TaggedLog.Debug("LogicalDeviceCommandRunnerIdsCan", $"{LogicalDevice} can't get session for physical device to send command {LogicalDevice}");
					return CommandResult.ErrorDeviceOffline;
				}
			}
			catch (OperationCanceledException)
			{
				TaggedLog.Debug("LogicalDeviceCommandRunnerIdsCan", $"{LogicalDevice} SendCommandAsync Operation Canceled");
				return CommandResult.Canceled;
			}
			catch (TimeoutException)
			{
				TaggedLog.Debug("LogicalDeviceCommandRunnerIdsCan", $"{LogicalDevice} SendCommandAsync Timeout");
				return CommandResult.ErrorSessionTimeout;
			}
			catch (PhysicalDeviceNotFoundException)
			{
				TaggedLog.Debug("LogicalDeviceCommandRunnerIdsCan", $"{LogicalDevice} SendCommandAsync Device Offline");
				return CommandResult.ErrorDeviceOffline;
			}
			catch (ActivateSessionDeviceOffline)
			{
				TaggedLog.Debug("LogicalDeviceCommandRunnerIdsCan", $"{LogicalDevice} SendCommandAsync Device Offline");
				return CommandResult.ErrorDeviceOffline;
			}
			catch (ActivateSessionEnforcedInTransitLockout)
			{
				TaggedLog.Debug("LogicalDeviceCommandRunnerIdsCan", $"{LogicalDevice} In Transit Lockout Enforced");
				return CommandResult.ErrorCommandNotAllowed;
			}
			catch (ActivateSessionNotAcceptingCommands)
			{
				TaggedLog.Debug("LogicalDeviceCommandRunnerIdsCan", $"{LogicalDevice} Device not currently accepting commands");
				return CommandResult.ErrorCommandNotAllowed;
			}
			catch (Exception ex4)
			{
				TaggedLog.Debug("LogicalDeviceCommandRunnerIdsCan", $"Command runner session exception {ex4.Message} for {LogicalDevice}");
				return CommandResult.ErrorOther;
			}
			if (!SendRemoteControlCommand(physicalDevice, commandByte, dataToSend))
			{
				TaggedLog.Debug("LogicalDeviceCommandRunnerIdsCan", $"{LogicalDevice} unable to queue command.");
				return CommandResult.ErrorQueueingCommand;
			}
			return CommandResult.Completed;
		}

		private bool SendRemoteControlCommand(IRemoteDevice physicalDevice, byte commandByte, byte[] commandToSend)
		{
			if (commandToSend.Length > 8)
			{
				TaggedLog.Error("LogicalDeviceCommandRunnerIdsCan", $"Unable to send remote control command as Data size specified is too big for {LogicalDevice}");
				return false;
			}
			CAN.PAYLOAD payload = default(CAN.PAYLOAD);
			for (int i = 0; i < commandToSend.Length; i++)
			{
				payload.Append(commandToSend[i]);
			}
			return physicalDevice.Adapter.LocalHost.Transmit29((byte)130, commandByte, physicalDevice, payload);
		}

		public override void Dispose(bool disposing)
		{
			lock (this)
			{
				CommandDeactivateSession();
			}
		}
	}
}
