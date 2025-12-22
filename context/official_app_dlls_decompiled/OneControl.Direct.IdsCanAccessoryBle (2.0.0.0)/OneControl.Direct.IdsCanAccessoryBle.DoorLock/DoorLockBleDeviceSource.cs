using System;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDeviceSource;
using OneControl.Devices.DoorLock;
using OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceSource;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.DoorLock
{
	public class DoorLockBleDeviceSource : BleDeviceSourceLocap<DoorLockBleDeviceDriver, SensorConnectionDoorLock, ILogicalDeviceDoorLock>, IDoorLockBleDeviceSource, IAccessoryBleDeviceSourceLocap<SensorConnectionDoorLock>, IAccessoryBleDeviceSourceLocap, ILogicalDeviceSourceDirect, ILogicalDeviceSource, IAccessoryBleDeviceSource<SensorConnectionDoorLock>, IAccessoryBleDeviceSource, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata, ILogicalDeviceSourceDirectPid, ILogicalDeviceSourceDirectAccessoryHistoryData, IAccessoryBleDeviceSourceDevices<DoorLockBleDeviceDriver>
	{
		private readonly IBleService _bleService;

		private const string DeviceSourceTokenDefault = "Ids.Accessory.DoorLock.Default";

		public const int BleConnectionAutoCloseTimeoutMs = 30000;

		public const int BleConnectAttemptMs = 40000;

		public const int BleConnectTimeoutMaxMs = 80000;

		public const int BleConnectionRetryDelayMs = 200;

		protected override string LogTag => "DoorLockBleDeviceSource";

		public DoorLockBleDeviceSource(IBleService bleService, ILogicalDeviceService deviceService)
			: base(bleService, deviceService, "Ids.Accessory.DoorLock.Default", TimeSpan.FromMilliseconds(40000.0))
		{
			_bleService = bleService;
		}

		protected override DoorLockBleDeviceDriver CreateDeviceBle(SensorConnectionDoorLock sensorConnection)
		{
			return new DoorLockBleDeviceDriver(_bleService, this, sensorConnection);
		}

		public override bool RegisterSensor(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			return RegisterSensor(new SensorConnectionDoorLock(bleDeviceName, bleDeviceId, accessoryMacAddress, softwarePartNumber));
		}

		protected override ILogicalDeviceTag CreateLogicalDeviceTag(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			return new LogicalDeviceTagSourceDoorLockBle(bleDeviceId, accessoryMacAddress, softwarePartNumber, bleDeviceName);
		}
	}
}
