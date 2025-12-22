using System;
using System.Linq;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using ids.portable.ble.Platforms.Shared.Reachability;
using IDS.Portable.Common;
using IDS.Portable.Devices.TPMS;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.TPMS;
using OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceDriver;
using OneControl.Direct.IdsCanAccessoryBle.Connections;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace OneControl.Direct.IdsCanAccessoryBle.TPMS
{
	public class TpmsBleDeviceDriver : BleDeviceDriverLoCap<ITpmsBleDeviceSource, SensorConnectionTpms, ILogicalDeviceTpms>
	{
		public const string DeviceNamePrefix = "LIP";

		protected override string LogTag => "TpmsBleDeviceDriver";

		public override DEVICE_TYPE BleDeviceType => (byte)42;

		protected override TimeSpan MinimumConnectionWindowTimeSpan => TimeSpan.FromMinutes(1.0);

		protected override int BleConnectionAutoCloseTimeoutMs => 30000;

		protected override int BleConnectionRetryDelayMs => 160000;

		protected override int BleConnectAttemptMs => 80000;

		protected override int BleConnectTimeoutMaxMs => 200;

		protected override int LogWarningTimeMs => 120000;

		public ILogicalDeviceTirePressureMonitor? LogicalDeviceTpmsLegacyAdapter { get; private set; }

		public TpmsBleDeviceDriver(IBleService bleService, ITpmsBleDeviceSource sourceDirect, SensorConnectionTpms sensorConnection)
		{
			SensorConnectionTpms sensorConnection2 = sensorConnection;
			base._002Ector(bleService, sourceDirect, sensorConnection2);
			ILogicalDeviceManager? deviceManager = _sourceDirect.DeviceService.DeviceManager;
			LogicalDeviceTpmsLegacyAdapter = ((deviceManager != null) ? Enumerable.FirstOrDefault(deviceManager!.FindLogicalDevices((ILogicalDeviceTirePressureMonitor ld) => ld.LogicalId.ProductMacAddress == sensorConnection2.AccessoryMac)) : null);
		}

		protected override void DeviceReachabilityManagerOnReachabilityChanged(BleDeviceReachability oldReachability, BleDeviceReachability newReachability)
		{
			switch (newReachability)
			{
			case BleDeviceReachability.Unreachable:
				base.LogicalDevice?.UpdateDeviceOnline(online: false);
				LogicalDeviceTpmsLegacyAdapter?.UpdateDeviceOnline(online: false);
				break;
			case BleDeviceReachability.Reachable:
				base.LogicalDevice?.UpdateDeviceOnline(online: true);
				LogicalDeviceTpmsLegacyAdapter?.UpdateDeviceOnline(online: true);
				break;
			default:
				base.LogicalDevice?.UpdateDeviceOnline();
				LogicalDeviceTpmsLegacyAdapter?.UpdateDeviceOnline();
				break;
			}
			_sourceDirect.DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
		}

		public override void Update(IdsCanAccessoryScanResult accessoryScanResult)
		{
			base.Update(accessoryScanResult);
			IdsCanAccessoryStatus? accessoryStatus = accessoryScanResult.GetAccessoryStatus(base.AccessoryMacAddress);
			if (!accessoryStatus.HasValue)
			{
				return;
			}
			if (base.LogicalDevice != null && LogicalDeviceTpmsLegacyAdapter == null)
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Creating LogicalDeviceTpmsLegacyAdapter for LogicalDevice: ");
				defaultInterpolatedStringHandler.AppendFormatted(base.LogicalDevice);
				TaggedLog.Information(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				LogicalDeviceId logicalDeviceId = new LogicalDeviceId(accessoryStatus.Value.DeviceType, 255, accessoryStatus.Value.FunctionName, accessoryStatus.Value.FunctionInstance, accessoryStatus.Value.ProductId, base.AccessoryMacAddress);
				ILogicalDevice logicalDevice = _sourceDirect.DeviceService.DeviceManager?.AddLogicalDevice(logicalDeviceId, accessoryStatus.Value.RawCapability, _sourceDirect, (ILogicalDevice ld) => true);
				if (!(logicalDevice is ILogicalDeviceTirePressureMonitor logicalDeviceTpmsLegacyAdapter) || logicalDevice.IsDisposed)
				{
					string logTag2 = LogTag;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(61, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to create logical device for ");
					defaultInterpolatedStringHandler.AppendFormatted(BleDeviceType);
					defaultInterpolatedStringHandler.AppendLiteral(" with BLE Scan Result of ");
					defaultInterpolatedStringHandler.AppendFormatted(accessoryScanResult);
					TaggedLog.Warning(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
					return;
				}
				LogicalDeviceTpmsLegacyAdapter = logicalDeviceTpmsLegacyAdapter;
			}
			if (_sourceDirect.DeviceService.GetPrimaryDeviceSourceDirect(base.LogicalDevice) != _sourceDirect)
			{
				return;
			}
			(byte?, byte[]) accessoryIdsCanExtendedStatusEnhanced = accessoryScanResult.GetAccessoryIdsCanExtendedStatusEnhanced(base.AccessoryMacAddress);
			var (b, _) = accessoryIdsCanExtendedStatusEnhanced;
			if (b.HasValue)
			{
				byte valueOrDefault = b.GetValueOrDefault();
				byte[] item = accessoryIdsCanExtendedStatusEnhanced.Item2;
				if (item != null)
				{
					base.LogicalDevice!.UpdateDeviceStatusExtended(item, (uint)item.Length, valueOrDefault);
				}
			}
			base.LogicalDevice?.UpdateSoftwarePartNumber(accessoryScanResult.GetVersionInfo().SoftwarePartNumber);
		}
	}
}
