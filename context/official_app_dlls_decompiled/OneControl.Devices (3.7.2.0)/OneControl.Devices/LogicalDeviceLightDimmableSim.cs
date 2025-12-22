using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLightDimmableSim : LogicalDeviceLightDimmable, ILogicalDeviceCommandRunnerIdsCan, INotifyPropertyChanged, ICommonDisposable, IDisposable, ILogicalDeviceSimulated, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, IDevicesCommon
	{
		private const string LogTag = "LogicalDeviceLightDimmableSim";

		private const int _tickIntervalMs = 250;

		private CancellationTokenSource _simTaskCancelSource = new CancellationTokenSource();

		private LogicalDeviceLightDimmableStatus _simStatus = new LogicalDeviceLightDimmableStatus();

		private DimmableLightMode _lastOnMode = DimmableLightMode.On;

		private bool _commandSessionActivated;

		private bool _simSwellDirectionUp;

		private int _simBrightnessTimeMs;

		private int _simDurationTimeMs;

		public override bool IsLegacyDeviceHazardous => false;

		public override LogicalDeviceActiveConnection ActiveConnection => LogicalDeviceActiveConnection.Direct;

		public override bool ActiveSession => CommandSessionActivated;

		public bool IsRunningCommands => false;

		public bool HasQueuedCommands => false;

		public bool CommandSessionActivated => _commandSessionActivated;

		public LogicalDeviceLightDimmableSim(ILogicalDeviceId logicalDeviceId, ILogicalDeviceLightDimmableCapability dimmableCapability, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, dimmableCapability, service, isFunctionClassChangeable)
		{
			IdsCanCommandRunner = this;
			RestartSim();
			Task.Run(async delegate
			{
				while (!_simTaskCancelSource.IsCancellationRequested)
				{
					ApplyDeviceStatus();
					SimTick();
					await TaskExtension.TryDelay(250, _simTaskCancelSource.Token);
				}
			}, _simTaskCancelSource.Token);
		}

		private void ApplyDeviceStatus()
		{
			lock (this)
			{
				UpdateDeviceStatus(_simStatus.Data, _simStatus.MinSize);
			}
		}

		public Task CommandActivateSession(CancellationToken cancelToken, bool activateSessionNow = true)
		{
			bool flag = !CommandSessionActivated;
			TaggedLog.Debug("LogicalDeviceLightDimmableSim", $"CommandActivateSession (SIM) for {DeviceName} - Start ChangingState={flag}");
			_commandSessionActivated = true;
			if (flag)
			{
				NotifyPropertyChanged("ActiveSession");
				NotifyPropertyChanged("CommandSessionActivated");
			}
			TaggedLog.Debug("LogicalDeviceLightDimmableSim", "CommandActivateSession (SIM) for " + DeviceName + " - Stop");
			return Task.FromResult(0);
		}

		public void CommandDeactivateSession(bool closeSession = true)
		{
			bool commandSessionActivated = CommandSessionActivated;
			TaggedLog.Debug("LogicalDeviceLightDimmableSim", $"CommandDeactivateSession (SIM) for {DeviceName} - Start ChangingState={commandSessionActivated}");
			_commandSessionActivated = false;
			if (commandSessionActivated)
			{
				NotifyPropertyChanged("ActiveSession");
				NotifyPropertyChanged("CommandSessionActivated");
			}
			TaggedLog.Debug("LogicalDeviceLightDimmableSim", "CommandDeactivateSession (SIM) for " + DeviceName + " - Stop");
		}

		public Task<CommandResult> SendCommandAsync(IDeviceCommandPacket dataPacket, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl> cmdControl = null, CommandSendOption options = CommandSendOption.None)
		{
			return SendCommandAsync(dataPacket.CommandByte, dataPacket.CopyCurrentData(), dataPacket.Size, dataPacket.CommandResponseTimeMs, cancelToken, cmdControl, options);
		}

		public Task<CommandResult> SendCommandAsync(byte commandByte, byte[] data, uint dataSize, int responseTimeMs, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl> cmdControl = null, CommandSendOption options = CommandSendOption.None)
		{
			if (dataSize != 8 || data.Length < 8)
			{
				return Task.FromResult(CommandResult.ErrorOther);
			}
			LogicalDeviceLightDimmableCommand logicalDeviceLightDimmableCommand = new LogicalDeviceLightDimmableCommand(commandByte, data);
			switch (logicalDeviceLightDimmableCommand.Command)
			{
			case DimmableLightCommand.Off:
				_simStatus.SetLightMode(DimmableLightMode.Off);
				break;
			case DimmableLightCommand.On:
				_simStatus.SetLightMode(DimmableLightMode.On);
				_simStatus.SetMaxBrightness(logicalDeviceLightDimmableCommand.MaxBrightness);
				_simStatus.SetDuration(logicalDeviceLightDimmableCommand.Duration);
				_lastOnMode = DimmableLightMode.On;
				break;
			case DimmableLightCommand.Blink:
				_simStatus.SetLightMode(DimmableLightMode.Blink);
				_simStatus.SetMaxBrightness(logicalDeviceLightDimmableCommand.MaxBrightness);
				_simStatus.SetDuration(logicalDeviceLightDimmableCommand.Duration);
				_simStatus.SetCycleTime1(logicalDeviceLightDimmableCommand.CycleTime1);
				_simStatus.SetCycleTime2(logicalDeviceLightDimmableCommand.CycleTime2);
				_lastOnMode = DimmableLightMode.Blink;
				break;
			case DimmableLightCommand.Swell:
				_simStatus.SetLightMode(DimmableLightMode.Swell);
				_simStatus.SetMaxBrightness(logicalDeviceLightDimmableCommand.MaxBrightness);
				_simStatus.SetDuration(logicalDeviceLightDimmableCommand.Duration);
				_simStatus.SetCycleTime1(logicalDeviceLightDimmableCommand.CycleTime1);
				_simStatus.SetCycleTime2(logicalDeviceLightDimmableCommand.CycleTime2);
				_lastOnMode = DimmableLightMode.Swell;
				break;
			case DimmableLightCommand.Settings:
				_simStatus.SetMaxBrightness(logicalDeviceLightDimmableCommand.MaxBrightness);
				_simStatus.SetDuration(logicalDeviceLightDimmableCommand.Duration);
				break;
			case DimmableLightCommand.Restore:
				_simStatus.SetLightMode(_lastOnMode);
				break;
			}
			ApplyDeviceStatus();
			RestartSim();
			return Task.FromResult(CommandResult.Completed);
		}

		public override async Task<CommandResult> SendCommandAsync(LogicalDeviceLightDimmableCommand command)
		{
			await Task.Delay(500);
			return await base.SendCommandAsync(command);
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_simTaskCancelSource?.TryCancelAndDispose();
			_simTaskCancelSource = null;
		}

		private void RestartSim()
		{
			_simSwellDirectionUp = true;
			_simBrightnessTimeMs = _simStatus.CycleTime1;
			_simDurationTimeMs = _simStatus.Duration * 60000;
			switch (_simStatus.Mode)
			{
			case DimmableLightMode.On:
			case DimmableLightMode.Blink:
				_simStatus.SetBrightness(_simStatus.MaxBrightness);
				break;
			case DimmableLightMode.Off:
			case DimmableLightMode.Swell:
				_simStatus.SetBrightness(0);
				break;
			}
		}

		private void SimTick()
		{
			if (_simStatus.Duration != 0)
			{
				_simDurationTimeMs -= 250;
				if (_simDurationTimeMs <= 0)
				{
					_simStatus.SetDuration(0);
					_simStatus.SetLightMode(DimmableLightMode.Off);
				}
				else
				{
					_simStatus.SetDuration((byte)(_simDurationTimeMs / 60000 + 1));
				}
			}
			switch (_simStatus.Mode)
			{
			case DimmableLightMode.Blink:
				_simBrightnessTimeMs -= 250;
				if (_simBrightnessTimeMs <= 0)
				{
					if (_simSwellDirectionUp)
					{
						_simStatus.SetBrightness(0);
						_simBrightnessTimeMs = _simStatus.CycleTime2;
					}
					else
					{
						_simStatus.SetBrightness(_simStatus.MaxBrightness);
						_simBrightnessTimeMs = _simStatus.CycleTime1;
					}
					_simSwellDirectionUp = !_simSwellDirectionUp;
				}
				break;
			case DimmableLightMode.Swell:
				_simBrightnessTimeMs -= 250;
				if (_simBrightnessTimeMs <= 0)
				{
					if (_simSwellDirectionUp)
					{
						_simStatus.SetBrightness(_simStatus.MaxBrightness);
						_simBrightnessTimeMs = _simStatus.CycleTime2;
					}
					else
					{
						_simStatus.SetBrightness(0);
						_simBrightnessTimeMs = _simStatus.CycleTime1;
					}
					_simSwellDirectionUp = !_simSwellDirectionUp;
				}
				else
				{
					int num = (_simSwellDirectionUp ? _simStatus.CycleTime1 : _simStatus.CycleTime2);
					int num2 = (_simSwellDirectionUp ? (num - _simBrightnessTimeMs) : _simBrightnessTimeMs);
					_simStatus.SetBrightness((byte)(_simStatus.MaxBrightness * num2 / num));
				}
				break;
			case DimmableLightMode.Off:
				_simStatus.SetBrightness(0);
				break;
			case DimmableLightMode.On:
				_simStatus.SetBrightness(_simStatus.MaxBrightness);
				break;
			}
		}
	}
}
