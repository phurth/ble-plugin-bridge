using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDeviceSource;
using OneControl.Devices.BatteryMonitor;
using OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceSource;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.BatteryMonitor
{
	public class BatteryMonitorBleDeviceSource : BleDeviceSourceLocap<BatteryMonitorBleDeviceDriver, SensorConnectionBatteryMonitor, ILogicalDeviceBatteryMonitor>, IBatteryMonitorBleDeviceSource, IAccessoryBleDeviceSourceLocap<SensorConnectionBatteryMonitor>, IAccessoryBleDeviceSourceLocap, ILogicalDeviceSourceDirect, ILogicalDeviceSource, IAccessoryBleDeviceSource<SensorConnectionBatteryMonitor>, IAccessoryBleDeviceSource, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata, ILogicalDeviceSourceDirectPid, ILogicalDeviceSourceDirectAccessoryHistoryData, IAccessoryBleDeviceSourceDevices<BatteryMonitorBleDeviceDriver>
	{
		private readonly IBleService _bleService;

		private const string DeviceSourceTokenDefault = "Ids.Accessory.BatteryMonitor.Default";

		public const int BleConnectionAutoCloseTimeoutMs = 10000;

		public const int BleConnectionRetryDelayMs = 1000;

		public const int BleConnectAttemptMs = 10000;

		public const int BleConnectTimeoutMaxMs = 30000;

		public const int StateOfChargeHistoryDaysMax = 90;

		public const int StateOfChargeHistoryStartingBlockId = 0;

		public const int MaxBlockId = 89;

		protected override string LogTag => "BatteryMonitorBleDeviceSource";

		public BatteryMonitorBleDeviceSource(IBleService bleService, ILogicalDeviceService deviceService)
			: base(bleService, deviceService, "Ids.Accessory.BatteryMonitor.Default", TimeSpan.FromMilliseconds(10000.0))
		{
			_bleService = bleService;
		}

		protected override BatteryMonitorBleDeviceDriver CreateDeviceBle(SensorConnectionBatteryMonitor sensorConnection)
		{
			return new BatteryMonitorBleDeviceDriver(_bleService, this, sensorConnection);
		}

		public override bool RegisterSensor(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			return RegisterSensor(new SensorConnectionBatteryMonitor(bleDeviceName, bleDeviceId, accessoryMacAddress, softwarePartNumber));
		}

		protected override ILogicalDeviceTag CreateLogicalDeviceTag(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			return new LogicalDeviceTagSourceBatteryMonitorBle(bleDeviceId, accessoryMacAddress, softwarePartNumber, bleDeviceName);
		}

		public override Task<IReadOnlyList<byte>> GetAccessoryHistoryDataAsync(ILogicalDevice logicalDevice, byte block, byte startIndex = 0, byte dataLength = byte.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (block > 89)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(64, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Battery monitor supports blocks id's up to ");
				defaultInterpolatedStringHandler.AppendFormatted(89);
				defaultInterpolatedStringHandler.AppendLiteral(", but ");
				defaultInterpolatedStringHandler.AppendFormatted(block);
				defaultInterpolatedStringHandler.AppendLiteral(" was requested.");
				throw new LogicalDeviceHistoryDataException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return ((GetSensorDevice(logicalDevice) ?? throw new LogicalDeviceHistoryDataException("Error retrieving accessory history data."))!.AccessoryConnectionManager ?? throw new LogicalDeviceHistoryDataException("Error retrieving accessory history data."))!.GetAccessoryHistoryDataAsync(block, startIndex, dataLength, cancellationToken);
		}
	}
}
