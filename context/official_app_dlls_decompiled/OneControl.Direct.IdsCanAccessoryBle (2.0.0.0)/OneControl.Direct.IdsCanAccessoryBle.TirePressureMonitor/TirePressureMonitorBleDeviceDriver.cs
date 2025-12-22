using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using ids.portable.ble.Platforms.Shared.Reachability;
using IDS.Portable.Common;
using IDS.Portable.Devices.TPMS;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.Connections;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.EventArgs;

namespace OneControl.Direct.IdsCanAccessoryBle.TirePressureMonitor
{
	public class TirePressureMonitorBleDeviceDriver : BackgroundOperation, ICommonDisposable, IDisposable
	{
		private const string LogTag = "TirePressureMonitorBleDeviceDriver";

		private const int SleepTimeMs = 15000;

		public const int WaitForBleDeviceTimeMs = 30000;

		public const int WaitForBleDeviceRetryTimeMs = 1000;

		public readonly DEVICE_TYPE DeviceType = (byte)42;

		public readonly FUNCTION_NAME FunctionName = (ushort)304;

		public const byte FunctionInstance = 0;

		private readonly ITirePressureMonitorBleDeviceSource _sourceDirect;

		private readonly IBleService _bleService;

		private short _currentSequence = 4095;

		private int _isDisposed;

		private BleDeviceReachabilityManager? _deviceReachabilityManager;

		public ILogicalDeviceTirePressureMonitor? LogicalDevice { get; private set; }

		public SensorConnectionTirePressureMonitor SensorConnection { get; }

		internal Guid BleDeviceId => SensorConnection.ConnectionGuid;

		public MAC AccessoryMacAddress => SensorConnection.AccessoryMac;

		public bool IsDisposed => _isDisposed != 0;

		protected virtual TimeSpan MinimumConnectionWindowTimeSpan => TimeSpan.FromMinutes(1.0);

		protected virtual TimeSpan MaximumConnectionWindowTimeSpan => TimeSpan.FromSeconds(65535.0);

		private BleDeviceReachabilityManager DeviceReachabilityManager => _deviceReachabilityManager ?? (_deviceReachabilityManager = CreateDeviceReachabilityManager());

		private string DeviceReachabilityName
		{
			get
			{
				string connectionNameFriendly = SensorConnection.ConnectionNameFriendly;
				object obj = LogicalDevice?.DeviceName;
				if (obj == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
					defaultInterpolatedStringHandler.AppendFormatted(SensorConnection.AccessoryMac);
					obj = defaultInterpolatedStringHandler.ToStringAndClear();
				}
				return connectionNameFriendly + "(" + (string)obj + ")";
			}
		}

		public bool IsOnline => DeviceReachabilityManager.Reachability == BleDeviceReachability.Reachable;

		public event UpdateTirePressureMonitorReachabilityEventHandler? UpdateTirePressureMonitorReachabilityEvent;

		public event ReachabilityChangedHandler? ReachabilityChanged;

		public TirePressureMonitorBleDeviceDriver(IBleService bleService, ITirePressureMonitorBleDeviceSource sourceDirect, SensorConnectionTirePressureMonitor sensorConnection)
		{
			_bleService = bleService;
			_sourceDirect = sourceDirect;
			SensorConnection = sensorConnection ?? throw new ArgumentNullException("sensorConnection");
			LogicalDevice = CreateLogicalDevice();
		}

		private ILogicalDeviceTirePressureMonitor? CreateLogicalDevice()
		{
			TaggedLog.Information("TirePressureMonitorBleDeviceDriver", "Creating Logical Device for Tire Pressure Monitor");
			LogicalDeviceTirePressureMonitorId logicalDeviceId = new LogicalDeviceTirePressureMonitorId(SensorConnection.ConnectionGuid, SensorConnectionTirePressureMonitor.TpmsProductIdDefault, SensorConnection.ConnectionNameFriendly);
			ILogicalDevice logicalDevice = _sourceDirect.DeviceService.DeviceManager?.AddLogicalDevice(logicalDeviceId, 0, _sourceDirect, (ILogicalDevice ld) => true);
			ILogicalDeviceTirePressureMonitor logicalDeviceTirePressureMonitor = logicalDevice as ILogicalDeviceTirePressureMonitor;
			if (logicalDeviceTirePressureMonitor == null || logicalDevice.IsDisposed)
			{
				TaggedLog.Warning("TirePressureMonitorBleDeviceDriver", "Unable to create LogicalDeviceTirePressureMonitor");
				return null;
			}
			_sourceDirect.DeviceService.DeviceManager?.RemoveLogicalDevice((ILogicalDevice d) => d is ILogicalDeviceTirePressureMonitor logicalDeviceTirePressureMonitor2 && !(logicalDeviceTirePressureMonitor2 is ITpmsAdapter) && d != logicalDeviceTirePressureMonitor);
			return logicalDeviceTirePressureMonitor;
		}

		protected override async Task BackgroundOperationAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested && IsDisposed)
			{
				return;
			}
			_bleService.Manager.DeviceAdvertised += Instance_DeviceAdvertisedAsync;
			while (!cancellationToken.IsCancellationRequested && !IsDisposed)
			{
				try
				{
					this.UpdateTirePressureMonitorReachabilityEvent?.Invoke(this);
				}
				catch
				{
				}
				await TaskExtension.TryDelay(15000, cancellationToken);
			}
			try
			{
				this.UpdateTirePressureMonitorReachabilityEvent?.Invoke(this);
			}
			catch
			{
			}
			try
			{
				_bleService.Manager.DeviceAdvertised -= Instance_DeviceAdvertisedAsync;
			}
			catch
			{
			}
		}

		private void Instance_DeviceAdvertisedAsync(object sender, DeviceEventArgs e)
		{
			ILogicalDeviceTirePressureMonitor logicalDevice = LogicalDevice;
			if (!(logicalDevice?.LogicalId is LogicalDeviceTirePressureMonitorId logicalDeviceTirePressureMonitorId))
			{
				return;
			}
			Guid bleDeviceGuid = logicalDeviceTirePressureMonitorId.BleDeviceGuid;
			if (!(e.Device.Id != bleDeviceGuid))
			{
				DeviceReachabilityManager.DeviceReachableUntil(MinimumConnectionWindowTimeSpan);
				AdvertisementRecord advertisementRecord2 = Enumerable.FirstOrDefault(e.Device.AdvertisementRecords, (AdvertisementRecord advertisementRecord) => advertisementRecord.Type == AdvertisementRecordType.ManufacturerSpecificData);
				if (advertisementRecord2 != null)
				{
					ProcessManufacturerSpecificDataAdvertisement(logicalDevice, advertisementRecord2.Data);
				}
			}
		}

		private void ProcessManufacturerSpecificDataAdvertisement(ILogicalDeviceTirePressureMonitor tpmsDevice, byte[] manufacturerSpecificData)
		{
			if (TirePressureMonitorAdvertisement.TryMakeTpmsAdvertisement(manufacturerSpecificData, out var advertisement))
			{
				tpmsDevice.UpdateTirePressureMonitorAdvertisementStatus(advertisement);
				_currentSequence = advertisement.Sequence;
			}
		}

		public LogicalDeviceReachability Reachability(ILogicalDevice logicalDevice)
		{
			if (logicalDevice != LogicalDevice)
			{
				return LogicalDeviceReachability.Unknown;
			}
			if (!IsOnline)
			{
				return LogicalDeviceReachability.Unreachable;
			}
			return LogicalDeviceReachability.Reachable;
		}

		public void TryDispose()
		{
			try
			{
				if (!IsDisposed)
				{
					Dispose();
				}
			}
			catch
			{
			}
		}

		public void Dispose()
		{
			if (!IsDisposed && Interlocked.Exchange(ref _isDisposed, 1) == 0)
			{
				Dispose(disposing: true);
			}
		}

		public virtual void Dispose(bool disposing)
		{
			Stop();
		}

		private BleDeviceReachabilityManager CreateDeviceReachabilityManager()
		{
			return new BleDeviceReachabilityManager(MinimumConnectionWindowTimeSpan, MaximumConnectionWindowTimeSpan, DeviceReachabilityManagerOnReachabilityChanged, () => DeviceReachabilityName);
		}

		protected virtual void DeviceReachabilityManagerOnReachabilityChanged(BleDeviceReachability oldReachability, BleDeviceReachability newReachability)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Updating TPMS Reachability: ");
			defaultInterpolatedStringHandler.AppendFormatted(newReachability);
			TaggedLog.Debug("TirePressureMonitorBleDeviceDriver", defaultInterpolatedStringHandler.ToStringAndClear());
			switch (newReachability)
			{
			case BleDeviceReachability.Unreachable:
				LogicalDevice?.UpdateDeviceOnline(online: false);
				break;
			case BleDeviceReachability.Reachable:
				LogicalDevice?.UpdateDeviceOnline(online: true);
				break;
			default:
				LogicalDevice?.UpdateDeviceOnline();
				break;
			}
			_sourceDirect.DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
			this.ReachabilityChanged?.Invoke(oldReachability, newReachability);
		}
	}
}
