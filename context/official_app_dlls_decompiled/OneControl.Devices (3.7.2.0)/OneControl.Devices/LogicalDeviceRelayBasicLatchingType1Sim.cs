using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayBasicLatchingType1Sim : LogicalDeviceRelayBasicLatchingType1, ILogicalDeviceSimulated, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		private CancellationTokenSource _relaySimTaskCancelSource = new CancellationTokenSource();

		private ILogicalDeviceRelayBasicStatus _relayStatus = LogicalDeviceRelayBasicLatching<LogicalDeviceRelayBasicStatusType1, LogicalDeviceRelayBasicCommandFactoryLatchingType1, LogicalDeviceRelayCapabilityType1>.MakeNewStatus();

		public LogicalDeviceRelayBasicLatchingType1Sim(LogicalDeviceId logicalDeviceId, ILogicalDeviceService service = null)
			: base(logicalDeviceId, new LogicalDeviceRelayCapabilityType1(RelayCapabilityFlagType1.None), service)
		{
			IdsCanCommandRunnerDefault = new RelayBasicLatchingCommandRunnerType1Sim(_relayStatus);
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
	}
}
