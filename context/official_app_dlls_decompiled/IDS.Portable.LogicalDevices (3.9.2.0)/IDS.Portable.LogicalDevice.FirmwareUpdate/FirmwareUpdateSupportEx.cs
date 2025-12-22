using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice.LogicalDeviceSource;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public static class FirmwareUpdateSupportEx
	{
		public static bool IsSupported(this FirmwareUpdateSupport firmwareUpdateSupport)
		{
			if (firmwareUpdateSupport != FirmwareUpdateSupport.SupportedViaDevice)
			{
				return firmwareUpdateSupport == FirmwareUpdateSupport.SupportedViaBootloader;
			}
			return true;
		}

		public static async Task<FirmwareUpdateSupport> TryGetFirmwareUpdateSupportAsync(this ILogicalDeviceSourceDirectManager deviceSourceManager, ILogicalDeviceFirmwareUpdateDevice logicalDevice, CancellationToken cancelToken)
		{
			ILogicalDeviceFirmwareUpdateDevice logicalDevice2 = logicalDevice;
			if (deviceSourceManager == null || logicalDevice2 == null)
			{
				return FirmwareUpdateSupport.Unknown;
			}
			if (logicalDevice2.ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				return FirmwareUpdateSupport.DeviceOffline;
			}
			bool noDeviceSourcesSupportFirmwareUpdate = true;
			List<ILogicalDeviceSourceDirectFirmwareUpdateDevice> list = deviceSourceManager.FindDeviceSources((ILogicalDeviceSourceDirectFirmwareUpdateDevice ds) => ds.IsLogicalDeviceOnline(logicalDevice2));
			foreach (ILogicalDeviceSourceDirectFirmwareUpdateDevice item in list)
			{
				FirmwareUpdateSupport firmwareUpdateSupport = await item.TryGetFirmwareUpdateSupportAsync(logicalDevice2, cancelToken);
				if (firmwareUpdateSupport.IsSupported())
				{
					return firmwareUpdateSupport;
				}
				if (firmwareUpdateSupport != FirmwareUpdateSupport.NotSupported)
				{
					noDeviceSourcesSupportFirmwareUpdate = false;
				}
			}
			if (logicalDevice2.ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				return FirmwareUpdateSupport.DeviceOffline;
			}
			return noDeviceSourcesSupportFirmwareUpdate ? FirmwareUpdateSupport.NotSupported : FirmwareUpdateSupport.Unknown;
		}

		public static async Task<ILogicalDeviceSourceDirectFirmwareUpdateDevice?> TryGetSupportedFirmwareUpdateDeviceSource(this ILogicalDeviceSourceDirectManager deviceSourceManager, ILogicalDeviceFirmwareUpdateDevice logicalDevice, CancellationToken cancelToken)
		{
			ILogicalDeviceFirmwareUpdateDevice logicalDevice2 = logicalDevice;
			if (deviceSourceManager == null || logicalDevice2 == null)
			{
				return null;
			}
			if (logicalDevice2.ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				return null;
			}
			List<ILogicalDeviceSourceDirectFirmwareUpdateDevice> list = deviceSourceManager.FindDeviceSources((ILogicalDeviceSourceDirectFirmwareUpdateDevice ds) => ds.IsLogicalDeviceOnline(logicalDevice2));
			foreach (ILogicalDeviceSourceDirectFirmwareUpdateDevice deviceSource in list)
			{
				if ((await deviceSource.TryGetFirmwareUpdateSupportAsync(logicalDevice2, cancelToken)).IsSupported())
				{
					return deviceSource;
				}
			}
			return null;
		}
	}
}
