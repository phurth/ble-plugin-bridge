using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayHBridgeMomentaryType1Sim : LogicalDeviceRelayHBridgeMomentaryType1, ILogicalDeviceCommandRunnerIdsCan, INotifyPropertyChanged, ICommonDisposable, IDisposable, ILogicalDeviceSimulated, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, IDevicesCommon
	{
		private const string LogTag = "LogicalDeviceRelayHBridgeMomentaryType1Sim";

		private CancellationTokenSource _relaySimTaskCancelSource = new CancellationTokenSource();

		private LogicalDeviceRelayHBridgeStatusType1 _relayStatus = new LogicalDeviceRelayHBridgeStatusType1();

		private bool _commandSessionActivated;

		public override LogicalDeviceActiveConnection ActiveConnection => LogicalDeviceActiveConnection.Direct;

		public override bool ActiveSession => CommandSessionActivated;

		public bool IsRunningCommands => false;

		public bool HasQueuedCommands => false;

		public override bool CommandSessionActivated => _commandSessionActivated;

		public LogicalDeviceRelayHBridgeMomentaryType1Sim(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService service = null)
			: base(logicalDeviceId, new LogicalDeviceRelayCapabilityType1(RelayCapabilityFlagType1.None), service)
		{
			IdsCanCommandRunner = this;
			BatteryVoltagePidCan = new LogicalDevicePidSimFixedPoint(FixedPointType.UnsignedBigEndian16x16, PID.BATTERY_VOLTAGE, 11.2f);
			Task.Run(async delegate
			{
				while (!_relaySimTaskCancelSource.IsCancellationRequested)
				{
					UpdateDeviceStatus(_relayStatus.Data, 1u);
					await TaskExtension.TryDelay(250, _relaySimTaskCancelSource.Token);
				}
			}, _relaySimTaskCancelSource.Token);
		}

		public Task CommandActivateSession(CancellationToken cancelToken, bool activateSessionNow = true)
		{
			bool flag = !CommandSessionActivated;
			TaggedLog.Debug("LogicalDeviceRelayHBridgeMomentaryType1Sim", $"CommandActivateSession (SIM) for {DeviceName} - Start ChangingState={flag}");
			_commandSessionActivated = true;
			if (flag)
			{
				NotifyPropertyChanged("ActiveSession");
				NotifyPropertyChanged("CommandSessionActivated");
			}
			TaggedLog.Debug("LogicalDeviceRelayHBridgeMomentaryType1Sim", "CommandActivateSession (SIM) for " + DeviceName + " - Stop");
			return Task.FromResult(0);
		}

		public void CommandDeactivateSession(bool closeSession = true)
		{
			bool commandSessionActivated = CommandSessionActivated;
			TaggedLog.Debug("LogicalDeviceRelayHBridgeMomentaryType1Sim", $"CommandDeactivateSession (SIM) for {DeviceName} - Start ChangingState={commandSessionActivated}");
			_commandSessionActivated = false;
			if (commandSessionActivated)
			{
				NotifyPropertyChanged("ActiveSession");
				NotifyPropertyChanged("CommandSessionActivated");
			}
			TaggedLog.Debug("LogicalDeviceRelayHBridgeMomentaryType1Sim", "CommandDeactivateSession (SIM) for " + DeviceName + " - Stop");
		}

		public Task<CommandResult> SendCommandAsync(IDeviceCommandPacket dataPacket, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl> cmdControl = null, CommandSendOption options = CommandSendOption.None)
		{
			return SendCommandAsync(dataPacket.CommandByte, dataPacket.CopyCurrentData(), dataPacket.Size, dataPacket.CommandResponseTimeMs, cancelToken, cmdControl, options);
		}

		public Task<CommandResult> SendCommandAsync(byte commandByte, byte[] data, uint dataSize, int responseTimeMs, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl> cmdControl = null, CommandSendOption options = CommandSendOption.None)
		{
			if (dataSize != 1 || data.Length < 1)
			{
				return Task.FromResult(CommandResult.ErrorOther);
			}
			LogicalDeviceRelayHBridgeMomentaryCommandType1 logicalDeviceRelayHBridgeMomentaryCommandType = new LogicalDeviceRelayHBridgeMomentaryCommandType1(base.LogicalId, data[0]);
			if (logicalDeviceRelayHBridgeMomentaryCommandType.ClearingFault)
			{
				_relayStatus.SetBit(BasicBitMask.BitMask0X40, value: false);
			}
			_relayStatus.SetBit(BasicBitMask.BitMask0X02, logicalDeviceRelayHBridgeMomentaryCommandType.TurningOnRelay2);
			_relayStatus.SetBit(BasicBitMask.BitMask0X01, logicalDeviceRelayHBridgeMomentaryCommandType.TurningOnRelay1);
			return Task.FromResult(CommandResult.Completed);
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_relaySimTaskCancelSource?.Cancel();
			_relaySimTaskCancelSource?.Dispose();
			_relaySimTaskCancelSource = null;
		}
	}
}
