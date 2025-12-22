using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.OneControlTouchPanel
{
	public class LogicalDeviceOneControlTouchPanelSim : LogicalDeviceOneControlTouchPanel, ILogicalDeviceSimulated, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		private const string LogTag = "LogicalDeviceOneControlTouchPanelSim";

		private const int StatusLength = 0;

		private readonly LogicalDeviceOneControlTouchPanelStatus _simStatus = new LogicalDeviceOneControlTouchPanelStatus();

		private readonly BackgroundOperation _simulator;

		public const int TickIntervalMs = 1000;

		private readonly byte[] _statusData = new byte[0];

		public override LogicalDeviceActiveConnection ActiveConnection => LogicalDeviceActiveConnection.Direct;

		public LogicalDeviceOneControlTouchPanelSim(ILogicalDeviceId logicalDeviceId, ILogicalDeviceOneControlTouchPanelCapability capability, LogicalDeviceOneControlTouchPanelStatus status, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, capability, status, service, isFunctionClassChangeable)
		{
			_simStatus.Update(_statusData, 0);
			UpdateDeviceStatus(_simStatus.Data, _simStatus.Size);
			_simulator = new BackgroundOperation((BackgroundOperation.BackgroundOperationFunc)SimulatorAsync);
			_simulator.Start();
		}

		public override void UpdateDeviceOnline(bool online)
		{
		}

		private async Task SimulatorAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				await TaskExtension.TryDelay(1000, cancellationToken);
			}
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_simulator.Stop();
		}
	}
}
