using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Leveler.Type3.State;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerType3Sim : LogicalDeviceLevelerType3, ILogicalDeviceSimulated, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		private CancellationTokenSource _levelerSimTaskCancelSource = new CancellationTokenSource();

		private readonly LogicalDeviceLevelerStatusType3 _levelerStatus = new LogicalDeviceLevelerStatusType3();

		private LogicalDeviceLevelerType3SimState _levelerState;

		public override LogicalDeviceActiveConnection ActiveConnection => LogicalDeviceActiveConnection.Direct;

		public override bool ActiveSession => IdsCanCommandRunner.CommandSessionActivated;

		public override ILogicalDevicePidFixedPoint VoltageMeasurementBatteryPid { get; } = new LogicalDevicePidSimFixedPoint(FixedPointType.UnsignedBigEndian16x16, PID.BATTERY_VOLTAGE, 10.5f);


		public LogicalDeviceLevelerType3Sim(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService deviceService = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, deviceService, isFunctionClassChangeable)
		{
			_levelerState = new LogicalDeviceLevelerType3SimState(_levelerStatus);
			IdsCanCommandRunner = new Leveler3CommandRunnerSim(_levelerState);
			Task.Run(async delegate
			{
				while (!_levelerSimTaskCancelSource.IsCancellationRequested)
				{
					UpdateDeviceStatus(_levelerStatus.Data, 6u);
					UpdateTextConsole(_levelerState);
					await TaskExtension.TryDelay(250, _levelerSimTaskCancelSource.Token);
				}
			}, _levelerSimTaskCancelSource.Token);
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_levelerSimTaskCancelSource?.TryCancelAndDispose();
			_levelerSimTaskCancelSource = null;
		}
	}
}
