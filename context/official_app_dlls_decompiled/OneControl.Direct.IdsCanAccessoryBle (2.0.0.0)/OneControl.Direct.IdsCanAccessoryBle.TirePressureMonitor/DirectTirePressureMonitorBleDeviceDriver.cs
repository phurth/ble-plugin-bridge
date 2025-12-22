using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using ids.portable.ble.BleManager;
using ids.portable.ble.Exceptions;
using ids.portable.ble.ScanResults;
using IDS.Portable.Common;
using IDS.Portable.Devices.TPMS;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.TirePressureMonitor
{
	public class DirectTirePressureMonitorBleDeviceDriver : CommonDisposable, ITirePressureMonitorBleDeviceSource, ILogicalDeviceSourceDirectConnection, ILogicalDeviceSourceDirect, ILogicalDeviceSource, ILogicalDeviceSourceConnection, ILogicalDeviceSourceTirePressureMonitor, ILogicalDeviceSourceDirectIdsAccessory, IAccessoryBleDeviceSource<SensorConnectionTirePressureMonitor>, IAccessoryBleDeviceSource, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata, IAccessoryBleDeviceSourceDevices<TirePressureMonitorBleDeviceDriver>
	{
		private readonly IBleService _bleService;

		private const string LogTag = "DirectTirePressureMonitorBleDeviceDriver";

		private const string TirePressureMonitorSoftwarePartNumber = "TPMSV1";

		private readonly ConcurrentDictionary<Guid, TirePressureMonitorBleDeviceDriver> _registeredTirePressureMonitors = new ConcurrentDictionary<Guid, TirePressureMonitorBleDeviceDriver>();

		private const string DeviceSourceTokenDefault = "Ids.Accessory.TirePressureMonitor.Default";

		public bool IsStarted;

		public string DeviceSourceToken { get; }

		public bool AllowAutoOfflineLogicalDeviceRemoval => false;

		public bool IsDeviceSourceActive => Enumerable.Any(_registeredTirePressureMonitors);

		public ILogicalDeviceService DeviceService { get; }

		public IBleManager BleManager { get; }

		public IEnumerable<ISensorConnection> SensorConnectionsAll
		{
			get
			{
				foreach (TirePressureMonitorBleDeviceDriver value in _registeredTirePressureMonitors.Values)
				{
					yield return value.SensorConnection;
				}
			}
		}

		public IEnumerable<TirePressureMonitorBleDeviceDriver> SensorDevices => _registeredTirePressureMonitors.Values;

		public IN_MOTION_LOCKOUT_LEVEL InTransitLockoutLevel => (byte)0;

		public ILogicalDeviceSessionManager? SessionManager
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public IReadOnlyList<ILogicalDeviceTag> ConnectionTagList
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool IsConnected => IsStarted;

		public event Action<ILogicalDeviceSourceDirectConnection>? DidConnectEvent;

		public event Action<ILogicalDeviceSourceDirectConnection>? DidDisconnectEvent;

		public event UpdateDeviceSourceReachabilityEventHandler? UpdateDeviceSourceReachabilityEvent;

		public DirectTirePressureMonitorBleDeviceDriver(IBleService bleService, ILogicalDeviceService deviceService, string deviceSourceToken = "Ids.Accessory.TirePressureMonitor.Default")
		{
			_bleService = bleService;
			DeviceService = deviceService;
			DeviceSourceToken = deviceSourceToken ?? "Ids.Accessory.TirePressureMonitor.Default";
			BleManager = _bleService.Manager;
			_bleService.Scanner.DidReceiveScanResult += ReceivedBleScanResult;
		}

		public ILogicalDeviceTirePressureMonitor? GetTirePressureMonitorLogicalDevice(SensorConnectionTirePressureMonitor connection)
		{
			foreach (KeyValuePair<Guid, TirePressureMonitorBleDeviceDriver> registeredTirePressureMonitor in _registeredTirePressureMonitors)
			{
				if (registeredTirePressureMonitor.Value.SensorConnection.ConnectionGuid == connection.ConnectionGuid)
				{
					return registeredTirePressureMonitor.Value.LogicalDevice;
				}
			}
			return null;
		}

		public bool RegisterSensor(SensorConnectionTirePressureMonitor sensorConnection)
		{
			Guid? guid = sensorConnection?.ConnectionGuid;
			if (guid.HasValue)
			{
				Guid valueOrDefault = guid.GetValueOrDefault();
				try
				{
					if (IsSensorRegistered(valueOrDefault))
					{
						return false;
					}
					TirePressureMonitorBleDeviceDriver tirePressureMonitorBleDeviceDriver = new TirePressureMonitorBleDeviceDriver(_bleService, this, sensorConnection);
					tirePressureMonitorBleDeviceDriver.UpdateTirePressureMonitorReachabilityEvent += DeviceReachabilityUpdated;
					bool num = _registeredTirePressureMonitors.TryAdd(valueOrDefault, tirePressureMonitorBleDeviceDriver);
					if (num)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Register Tire Pressure Monitor ");
						defaultInterpolatedStringHandler.AppendFormatted(valueOrDefault);
						TaggedLog.Debug("DirectTirePressureMonitorBleDeviceDriver", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					if (IsStarted)
					{
						tirePressureMonitorBleDeviceDriver.Start();
					}
					return num;
				}
				catch (BleScannerServiceAlreadyRegisteredException)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Tire Pressure Monitor already registered for ");
					defaultInterpolatedStringHandler.AppendFormatted(valueOrDefault);
					TaggedLog.Debug("DirectTirePressureMonitorBleDeviceDriver", defaultInterpolatedStringHandler.ToStringAndClear());
					return false;
				}
				catch (Exception ex2)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(42, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Tire Pressure Monitor error registering ");
					defaultInterpolatedStringHandler.AppendFormatted(valueOrDefault);
					defaultInterpolatedStringHandler.AppendLiteral(": ");
					defaultInterpolatedStringHandler.AppendFormatted(ex2.Message);
					TaggedLog.Error("DirectTirePressureMonitorBleDeviceDriver", defaultInterpolatedStringHandler.ToStringAndClear());
					return false;
				}
			}
			return false;
		}

		private void DeviceReachabilityUpdated(TirePressureMonitorBleDeviceDriver tirePressureMonitorBle)
		{
			this.UpdateDeviceSourceReachabilityEvent?.Invoke(this);
		}

		public void UnRegisterSensor(Guid bleDeviceId)
		{
			if (_registeredTirePressureMonitors.TryRemove(bleDeviceId, out var tirePressureMonitorBleDeviceDriver))
			{
				tirePressureMonitorBleDeviceDriver.UpdateTirePressureMonitorReachabilityEvent -= DeviceReachabilityUpdated;
				tirePressureMonitorBleDeviceDriver.TryDispose();
			}
		}

		public bool IsSensorRegistered(Guid bleDeviceId)
		{
			return _registeredTirePressureMonitors.ContainsKey(bleDeviceId);
		}

		public bool IsLogicalDeviceSupported(ILogicalDevice? logicalDevice)
		{
			return true;
		}

		public bool IsLogicalDeviceOnline(ILogicalDevice? logicalDevice)
		{
			ILogicalDevice logicalDevice2 = logicalDevice;
			if (logicalDevice2 == null || logicalDevice2.IsDisposed)
			{
				return false;
			}
			return Enumerable.FirstOrDefault(_registeredTirePressureMonitors.Values, (TirePressureMonitorBleDeviceDriver sd) => logicalDevice2.LogicalId.ProductMacAddress?.Equals(sd.AccessoryMacAddress) ?? false)?.IsOnline ?? false;
		}

		public TirePressureMonitorBleDeviceDriver? GetSensorDevice(ILogicalDevice? logicalDevice)
		{
			return Enumerable.FirstOrDefault(_registeredTirePressureMonitors.Values, (TirePressureMonitorBleDeviceDriver ts) => true);
		}

		public IEnumerable<ILogicalDeviceTag> MakeDeviceSourceTags(ILogicalDevice? logicalDevice)
		{
			return new ILogicalDeviceTag[0];
		}

		public IN_MOTION_LOCKOUT_LEVEL GetLogicalDeviceInTransitLockoutLevel(ILogicalDevice? logicalDevice)
		{
			return (byte)0;
		}

		public bool IsLogicalDeviceHazardous(ILogicalDevice? logicalDevice)
		{
			return false;
		}

		private void ReceivedBleScanResult(IBleScanResult scanResult)
		{
		}

		public bool IsLogicalDeviceRenameSupported(ILogicalDevice? logicalDevice)
		{
			return IsLogicalDeviceSupported(logicalDevice);
		}

		public async Task RenameLogicalDevice(ILogicalDevice? logicalDevice, FUNCTION_NAME toName, byte toFunctionInstance, CancellationToken cancellationToken)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(112, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Tire Pressure Monitor rename: LogicalDevice: ");
			defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
			defaultInterpolatedStringHandler.AppendLiteral("  toName: ");
			defaultInterpolatedStringHandler.AppendFormatted(toName);
			defaultInterpolatedStringHandler.AppendLiteral(" toFunctionInstance: ");
			defaultInterpolatedStringHandler.AppendFormatted(toFunctionInstance);
			defaultInterpolatedStringHandler.AppendLiteral(". This has not been implemented yet.");
			TaggedLog.Debug("DirectTirePressureMonitorBleDeviceDriver", defaultInterpolatedStringHandler.ToStringAndClear());
			throw new NotImplementedException();
		}

		public override void Dispose(bool disposing)
		{
			try
			{
				_bleService.Scanner.DidReceiveScanResult -= ReceivedBleScanResult;
			}
			catch
			{
			}
			foreach (KeyValuePair<Guid, TirePressureMonitorBleDeviceDriver> registeredTirePressureMonitor in _registeredTirePressureMonitors)
			{
				UnRegisterSensor(registeredTirePressureMonitor.Key);
			}
			_registeredTirePressureMonitors.Clear();
		}

		public Task<string> GetSoftwarePartNumberAsync(ILogicalDevice logicalDevice, CancellationToken cancelToken)
		{
			return Task.FromResult("TPMSV1");
		}

		public Version? GetDeviceProtocolVersion(ILogicalDevice logicalDevice)
		{
			return null;
		}

		public void Start()
		{
			IsStarted = true;
			foreach (KeyValuePair<Guid, TirePressureMonitorBleDeviceDriver> registeredTirePressureMonitor in _registeredTirePressureMonitors)
			{
				registeredTirePressureMonitor.Value.Start();
			}
		}

		public void Stop()
		{
			IsStarted = false;
			foreach (KeyValuePair<Guid, TirePressureMonitorBleDeviceDriver> registeredTirePressureMonitor in _registeredTirePressureMonitors)
			{
				registeredTirePressureMonitor.Value.Stop();
			}
		}

		public LogicalDeviceReachability DeviceSourceReachability(ILogicalDevice logicalDevice)
		{
			return FindAssociatedBleControlForLogicalDevice(logicalDevice)?.Reachability(logicalDevice) ?? LogicalDeviceReachability.Unknown;
		}

		private TirePressureMonitorBleDeviceDriver? FindAssociatedBleControlForLogicalDevice(ILogicalDevice logicalDevice)
		{
			foreach (KeyValuePair<Guid, TirePressureMonitorBleDeviceDriver> registeredTirePressureMonitor in _registeredTirePressureMonitors)
			{
				if (logicalDevice == registeredTirePressureMonitor.Value.LogicalDevice)
				{
					return registeredTirePressureMonitor.Value;
				}
			}
			return null;
		}
	}
}
