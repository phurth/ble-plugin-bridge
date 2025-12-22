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
using OneControl.Devices.Interfaces;
using Serilog;

namespace OneControl.Devices
{
	public class LogicalDeviceMonitorPanel : LogicalDevice<LogicalDeviceMonitorPanelStatus, ILogicalDeviceMonitorPanelCapability>, ILogicalDeviceMonitorPanelDirect, ILogicalDeviceMonitorPanel, IMonitorPanel, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithCapability<ILogicalDeviceMonitorPanelCapability>, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, ILogicalDeviceWithStatus<LogicalDeviceMonitorPanelStatus>, ILogicalDeviceWithStatus, ILogicalDeviceWithStatusUpdate<LogicalDeviceMonitorPanelStatus>, IHighResolutionTankSupport, ILogicalDeviceFirmwareUpdateDevice, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink
	{
		private readonly Dictionary<Pid, ILogicalDevicePidMonitorPanelDeviceId> _pidDeviceIdDict = new Dictionary<Pid, ILogicalDevicePidMonitorPanelDeviceId>();

		private readonly IReadOnlyDictionary<FirmwareUpdateOption, string> _defaultFirmwareUpdateOptions = new Dictionary<FirmwareUpdateOption, string> { 
		{
			FirmwareUpdateOption.StartAddress,
			$"{4325376u}"
		} };

		public override bool IsLegacyDeviceHazardous => false;

		public static IReadOnlyList<Pid> MonitorPanelDeviceIdPidList => PidExtension.MonitorPanelDeviceIdPidList;

		public bool IsDeviceConfigurationDataValid => DeviceStatus.IsDeviceConfigurationDataValid;

		public bool AreDeviceDefinitionsValid => DeviceStatus.AreDeviceDefinitionsValid;

		public IReadOnlyDictionary<Pid, ILogicalDevicePidMonitorPanelDeviceId> PidDeviceIdDict => _pidDeviceIdDict;

		public ILogicalDevicePidULong AvailableMomentarySwitchesPid { get; protected set; }

		public ILogicalDevicePidULong AvailableLatchingSwitchesPid { get; protected set; }

		public ILogicalDevicePidULong AvailableSupplyTankIndicatorsPid { get; protected set; }

		public ILogicalDevicePidULong AvailableWasteTankIndicatorsPid { get; protected set; }

		public IReadOnlyDictionary<FirmwareUpdateOption, string> DefaultFirmwareUpdateOptions => _defaultFirmwareUpdateOptions;

		public Task<bool> TryAreHighResolutionTanksSupportedAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(base.DeviceCapability.SupportsHighResolutionTanks);
		}

		public LogicalDeviceMonitorPanel(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceMonitorPanelStatus(), (ILogicalDeviceMonitorPanelCapability)new LogicalDeviceMonitorPanelCapability(), service, isFunctionClassChangeable)
		{
			AvailableMomentarySwitchesPid = new LogicalDevicePidULong(this, Pid.MonitorPanelControlTypeMomentarySwitch.ConvertToPid(), LogicalDeviceSessionType.None, null, 255uL);
			AvailableLatchingSwitchesPid = new LogicalDevicePidULong(this, Pid.MonitorPanelControlTypeLatchingSwitch.ConvertToPid(), LogicalDeviceSessionType.None, null, 255uL);
			AvailableSupplyTankIndicatorsPid = new LogicalDevicePidULong(this, Pid.MonitorPanelControlTypeSupplyTank.ConvertToPid(), LogicalDeviceSessionType.None, null, 255uL);
			AvailableWasteTankIndicatorsPid = new LogicalDevicePidULong(this, Pid.MonitorPanelControlTypeWasteTank.ConvertToPid(), LogicalDeviceSessionType.None, null, 255uL);
			foreach (Pid monitorPanelDeviceIdPid in MonitorPanelDeviceIdPidList)
			{
				_pidDeviceIdDict[monitorPanelDeviceIdPid] = new LogicalDevicePidMonitorPanelDeviceId(this, monitorPanelDeviceIdPid.ConvertToPid(), LogicalDeviceSessionType.Diagnostic);
			}
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
			Log.Information("Data Changed {LogicalId}: ConfigurationDataValid = {IsDeviceConfigurationDataValid}, DevicesDefinitionsValid = {AreDeviceDefinitionsValid}", base.LogicalId, DeviceStatus.IsDeviceConfigurationDataValid, AreDeviceDefinitionsValid);
			OnPropertyChanged("IsDeviceConfigurationDataValid");
			OnPropertyChanged("AreDeviceDefinitionsValid");
		}

		public Task<FirmwareUpdateSupport> TryGetFirmwareUpdateSupportAsync(CancellationToken cancelToken)
		{
			return DeviceService.DeviceSourceManager.TryGetFirmwareUpdateSupportAsync(this, cancelToken);
		}

		public async Task UpdateFirmwareAsync(IReadOnlyList<byte> data, Func<ILogicalDeviceTransferProgress, bool> progressAck, CancellationToken cancellationToken, Dictionary<FirmwareUpdateOption, object>? options = null)
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
