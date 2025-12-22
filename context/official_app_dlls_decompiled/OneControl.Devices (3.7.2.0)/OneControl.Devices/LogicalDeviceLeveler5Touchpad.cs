using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.FirmwareUpdate;
using IDS.Portable.LogicalDevice.LogicalDeviceSource;

namespace OneControl.Devices
{
	public class LogicalDeviceLeveler5Touchpad : LogicalDevice<LogicalDeviceLeveler5TouchpadStatus, ILogicalDeviceCapability>, ILogicalDeviceLeveler5Touchpad, ILogicalDeviceWithStatus<LogicalDeviceLeveler5TouchpadStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<LogicalDeviceLeveler5TouchpadStatus>, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink, ILogicalDeviceFirmwareUpdateDevice
	{
		private const string LogTag = "LogicalDeviceLeveler5Touchpad";

		private readonly Dictionary<FirmwareUpdateOption, string> _defaultFirmwareUpdateOptions = new Dictionary<FirmwareUpdateOption, string>();

		public override bool IsLegacyDeviceHazardous => false;

		public IReadOnlyDictionary<FirmwareUpdateOption, string> DefaultFirmwareUpdateOptions
		{
			get
			{
				lock (_defaultFirmwareUpdateOptions)
				{
					if (_defaultFirmwareUpdateOptions.Count != 0)
					{
						return _defaultFirmwareUpdateOptions;
					}
					if (base.LogicalId.ProductId == PRODUCT_ID.BASECAMP_LEVELER_5W_TOUCHPAD_ASSEMBLY)
					{
						_defaultFirmwareUpdateOptions[FirmwareUpdateOption.StartAddress] = $"{4325376u}";
					}
					else
					{
						TaggedLog.Error("LogicalDeviceLeveler5Touchpad", $"Level 5 Touchpad is not configured with the correct Product ID. Product ID: {base.LogicalId.ProductId}");
					}
					return _defaultFirmwareUpdateOptions;
				}
			}
		}

		public LogicalDeviceLeveler5Touchpad(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService deviceService = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceLeveler5TouchpadStatus(), (ILogicalDeviceCapability)new LogicalDeviceCapability(), deviceService, isFunctionClassChangeable)
		{
		}

		public virtual Task<FirmwareUpdateSupport> TryGetFirmwareUpdateSupportAsync(CancellationToken cancelToken)
		{
			return DeviceService.DeviceSourceManager.TryGetFirmwareUpdateSupportAsync(this, cancelToken);
		}

		public virtual async Task UpdateFirmwareAsync(IReadOnlyList<byte> data, Func<ILogicalDeviceTransferProgress, bool> progressAck, CancellationToken cancellationToken, Dictionary<FirmwareUpdateOption, object>? options = null)
		{
			ILogicalDeviceSourceDirectFirmwareUpdateDevice logicalDeviceSourceDirectFirmwareUpdateDevice = await DeviceService.DeviceSourceManager.TryGetSupportedFirmwareUpdateDeviceSource(this, cancellationToken);
			if (logicalDeviceSourceDirectFirmwareUpdateDevice == null)
			{
				throw new FirmwareUpdateNotSupportedException(this, FirmwareUpdateSupport.Unknown);
			}
			using ILogicalDeviceFirmwareUpdateSession firmwareUpdateSession = DeviceService.FirmwareUpdateManager.StartFirmwareUpdateSession(this);
			await logicalDeviceSourceDirectFirmwareUpdateDevice.UpdateFirmwareAsync(firmwareUpdateSession, data, progressAck, cancellationToken, options);
		}

		NETWORK_STATUS ILogicalDeviceIdsCan.get_LastReceivedNetworkStatus()
		{
			return base.LastReceivedNetworkStatus;
		}
	}
}
