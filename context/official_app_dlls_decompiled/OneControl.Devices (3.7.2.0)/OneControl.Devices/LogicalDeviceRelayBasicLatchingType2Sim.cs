using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayBasicLatchingType2Sim : LogicalDeviceRelayBasicLatchingType2, ILogicalDeviceSimulated, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		private const string LogTag = "LogicalDeviceRelayBasicLatchingType2Sim";

		private CancellationTokenSource _relaySimTaskCancelSource = new CancellationTokenSource();

		private ILogicalDeviceRelayBasicStatus _relayStatus = LogicalDeviceRelayBasicLatching<LogicalDeviceRelayBasicStatusType2, LogicalDeviceRelayBasicCommandFactoryLatchingType2, LogicalDeviceRelayCapabilityType2>.MakeNewStatus();

		public LogicalDeviceRelayBasicLatchingType2Sim(LogicalDeviceId logicalDeviceId, ILogicalDeviceService service = null)
			: base(logicalDeviceId, new LogicalDeviceRelayCapabilityType2(RelayCapabilityFlagType2.None), service)
		{
			LogicalDeviceRelayBasicLatchingType2Sim logicalDeviceRelayBasicLatchingType2Sim = this;
			IdsCanCommandRunnerDefault = new RelayBasicLatchingCommandRunnerType2Sim(_relayStatus);
			BatteryVoltagePidCan = new LogicalDevicePidSimFixedPoint(FixedPointType.UnsignedBigEndian16x16, PID.BATTERY_VOLTAGE, 11.2f);
			CancellationToken token = _relaySimTaskCancelSource.Token;
			Task.Run(async delegate
			{
				while (!token.IsCancellationRequested)
				{
					logicalDeviceRelayBasicLatchingType2Sim.UpdateDeviceStatus(logicalDeviceRelayBasicLatchingType2Sim._relayStatus.Data, (uint)logicalDeviceRelayBasicLatchingType2Sim._relayStatus.Data.Length);
					await TaskExtension.TryDelay(250, token);
				}
			}, token);
		}

		public override async Task<CommandResult> SendCommandAsync(bool turnOnRelay, bool waitForCurrentStatus)
		{
			await Task.Delay(500);
			return await base.SendCommandAsync(turnOnRelay, waitForCurrentStatus);
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_relaySimTaskCancelSource?.Cancel();
			_relaySimTaskCancelSource?.Dispose();
			_relaySimTaskCancelSource = null;
		}

		public void SimSetUserClearRequired(bool clearRequired)
		{
			_relayStatus.SetUserClearRequired(clearRequired);
		}

		public void SimSetUserMessageDtc(DTC_ID dtc)
		{
			_relayStatus.SetUserMessageDtc(dtc);
		}
	}
}
