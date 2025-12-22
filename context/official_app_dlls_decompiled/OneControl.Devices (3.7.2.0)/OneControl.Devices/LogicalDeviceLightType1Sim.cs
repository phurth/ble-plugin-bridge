using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLightType1Sim : LogicalDeviceLightType1, ILogicalDeviceSimulated, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		private CancellationTokenSource _relaySimTaskCancelSource = new CancellationTokenSource();

		private ILogicalDeviceRelayBasicStatus _relayStatus = LogicalDeviceRelayBasicLatching<LogicalDeviceRelayBasicStatusType1, LogicalDeviceRelayBasicCommandFactoryLatchingType1, LogicalDeviceRelayCapabilityType1>.MakeNewStatus();

		public override bool IsLegacyDeviceHazardous => false;

		public LogicalDeviceLightType1Sim(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService service = null)
			: base(logicalDeviceId, new LogicalDeviceRelayCapabilityType1(RelayCapabilityFlagType1.None), service)
		{
			IdsCanCommandRunnerDefault = new RelayBasicLatchingCommandRunnerType1Sim(_relayStatus);
			Task.Run(async delegate
			{
				while (!_relaySimTaskCancelSource.IsCancellationRequested)
				{
					UpdateDeviceStatus(_relayStatus.Data, 1u);
					await TaskExtension.TryDelay(250, _relaySimTaskCancelSource.Token);
				}
			}, _relaySimTaskCancelSource.Token);
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
