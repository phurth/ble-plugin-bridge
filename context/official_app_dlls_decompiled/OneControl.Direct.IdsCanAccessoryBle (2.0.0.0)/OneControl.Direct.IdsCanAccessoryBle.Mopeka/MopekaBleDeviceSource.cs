using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices;
using OneControl.Devices.TankSensor.Mopeka;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.Mopeka
{
	public class MopekaBleDeviceSource : CommonDisposable, IMopekaBleDeviceSource, ILogicalDeviceExSnapshot, ILogicalDeviceExOnline, ILogicalDeviceEx, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirect, ILogicalDeviceSource, ILogicalDeviceSourceDirectMetadata
	{
		private const string LogTag = "MopekaBleDeviceSource";

		private const string MopekaSoftwarePartNumber = "mopeka";

		private readonly ILogicalDeviceService _deviceService;

		private readonly IBleService _bleService;

		private readonly Func<ILPSettingsRepository> _lpSettingsRepositoryFactory;

		private readonly ConcurrentDictionary<MAC, MopekaSensor> _linkedMopekaSensors = new ConcurrentDictionary<MAC, MopekaSensor>();

		public string DeviceSourceToken => "Ids.MopekaSensor";

		public bool AllowAutoOfflineLogicalDeviceRemoval => false;

		public bool IsDeviceSourceActive => Enumerable.Any(_linkedMopekaSensors);

		public ILogicalDeviceService DeviceService => _deviceService;

		public IEnumerable<ISensorConnection> SensorConnectionsAll
		{
			get
			{
				foreach (MopekaSensor value in _linkedMopekaSensors.Values)
				{
					yield return value.SensorConnection;
				}
			}
		}

		public IN_MOTION_LOCKOUT_LEVEL InTransitLockoutLevel => (byte)0;

		public static MopekaBleDeviceSource? SharedExtension { get; set; }

		public MopekaBleDeviceSource(ILogicalDeviceService deviceService, IBleService bleService, Func<ILPSettingsRepository> lpSettingsRepositoryFactory)
		{
			_deviceService = deviceService;
			_bleService = bleService;
			_lpSettingsRepositoryFactory = lpSettingsRepositoryFactory;
			_bleService.Scanner.FactoryRegistry.Register(new MopekaBleScanResultFactory());
		}

		public bool LinkMopekaSensor(SensorConnectionMopeka sensorConnection)
		{
			MAC mAC = sensorConnection?.MacAddress;
			if ((object)mAC == null)
			{
				return false;
			}
			MopekaSensor result = MopekaSensor.Create(_deviceService, this, _bleService, _lpSettingsRepositoryFactory(), sensorConnection).Result;
			bool num = _linkedMopekaSensors.TryAdd(mAC, result);
			if (num)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Link Mopeka Sensor ");
				defaultInterpolatedStringHandler.AppendFormatted(mAC);
				TaggedLog.Information("MopekaBleDeviceSource", defaultInterpolatedStringHandler.ToStringAndClear());
				return num;
			}
			result.TryDispose();
			return num;
		}

		public bool UnlinkMopekaSensor(MAC macAddress)
		{
			if (!_linkedMopekaSensors.TryRemove(macAddress, out var mopekaSensor))
			{
				return false;
			}
			mopekaSensor.DetachFromSource();
			mopekaSensor.TryDispose();
			return true;
		}

		public MopekaSensor GetSensor(MAC macAddress)
		{
			if (!IsMopekaSensorLinked(macAddress))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 1);
				defaultInterpolatedStringHandler.AppendLiteral("No sensor associated with the MAC address: ");
				defaultInterpolatedStringHandler.AppendFormatted(macAddress);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "macAddress");
			}
			return _linkedMopekaSensors[macAddress];
		}

		public bool IsMopekaSensorLinked(MAC macAddress)
		{
			return _linkedMopekaSensors.ContainsKey(macAddress);
		}

		public bool IsLogicalDeviceSupported(ILogicalDevice? logicalDevice)
		{
			if (logicalDevice == null || logicalDevice!.IsDisposed)
			{
				return false;
			}
			MAC productMacAddress = logicalDevice!.LogicalId.ProductMacAddress;
			if (productMacAddress != null)
			{
				return IsMopekaSensorLinked(productMacAddress);
			}
			return false;
		}

		public bool IsLogicalDeviceOnline(ILogicalDevice? logicalDevice)
		{
			if (!IsLogicalDeviceSupported(logicalDevice))
			{
				return false;
			}
			return _linkedMopekaSensors[logicalDevice!.LogicalId.ProductMacAddress].IsOnline;
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

		public static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice)
		{
			if (!(logicalDevice is LogicalDeviceTankSensor))
			{
				return null;
			}
			if (SharedExtension == null)
			{
				TaggedLog.Warning("MopekaBleDeviceSource", "SharedExtension Not Configured for MopekaBleDeviceSource");
			}
			return SharedExtension;
		}

		public void LogicalDeviceAttached(ILogicalDevice logicalDevice)
		{
		}

		public void LogicalDeviceDetached(ILogicalDevice logicalDevice)
		{
		}

		public void LogicalDeviceOnlineChanged(ILogicalDevice logicalDevice)
		{
		}

		public void LogicalDeviceSnapshotLoaded(ILogicalDevice logicalDevice, LogicalDeviceSnapshot snapshot)
		{
		}

		public override void Dispose(bool disposing)
		{
			foreach (MopekaSensor value in _linkedMopekaSensors.Values)
			{
				value.TryDispose();
			}
			_linkedMopekaSensors.Clear();
		}

		public bool IsLogicalDeviceRenameSupported(ILogicalDevice? logicalDevice)
		{
			return IsLogicalDeviceSupported(logicalDevice);
		}

		public Task RenameLogicalDevice(ILogicalDevice? logicalDevice, FUNCTION_NAME toName, byte toFunctionInstance, CancellationToken cancellationToken)
		{
			if (!IsLogicalDeviceRenameSupported(logicalDevice))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Cannot rename device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice?.LogicalId.ProductMacAddress);
				defaultInterpolatedStringHandler.AppendLiteral(", operation unsupported.");
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if ((object)logicalDevice?.LogicalId.ProductMacAddress == null)
			{
				throw new Exception("Can't rename Mopeka device because mac address is null.");
			}
			if (!logicalDevice!.Rename(toName, toFunctionInstance))
			{
				throw new Exception("Error renaming Mopeka device, logical device rename returned false.");
			}
			GetSensor(logicalDevice!.LogicalId.ProductMacAddress).SensorConnection.DefaultFunctionName = toName.ToFunctionName();
			return Task.CompletedTask;
		}

		public Task<string> GetSoftwarePartNumberAsync(ILogicalDevice logicalDevice, CancellationToken cancelToken)
		{
			return Task.FromResult("mopeka");
		}

		public Version? GetDeviceProtocolVersion(ILogicalDevice logicalDevice)
		{
			return null;
		}
	}
}
