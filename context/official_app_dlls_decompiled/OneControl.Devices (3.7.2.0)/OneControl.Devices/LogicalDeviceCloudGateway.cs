using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Remote;

namespace OneControl.Devices
{
	public class LogicalDeviceCloudGateway : LogicalDevice<LogicalDeviceCloudGatewayStatus, ILogicalDeviceCapability>, ILogicalDeviceRemoteCloudGateway, ILogicalDeviceCloudGateway, ICloudGateway, ILogicalDeviceWithStatus<LogicalDeviceCloudGatewayStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<LogicalDeviceCloudGatewayStatus>, ILogicalDeviceMyRvLink, ILogicalDeviceIdsCan, ILogicalDeviceRemote, ILogicalDeviceSoftwareUpdateStatus
	{
		private const string LogTag = "LogicalDeviceCloudGateway";

		protected string CachedAssetId;

		public RemoteOnline RemoteOnline { get; protected set; }

		public RemoteSoftwareUpdate RemoteSoftwareUpdate { get; protected set; }

		public override bool IsRemoteAccessAvailable => DeviceService.RemoteManager?.IsRemoteAccessAvailable(this) ?? false;

		public IRemoteChannelDefOnline RemoteOnlineChannel => RemoteOnline?.Channel;

		public SoftwareUpdateState SoftwareUpdateState
		{
			get
			{
				switch (ActiveConnection)
				{
				case LogicalDeviceActiveConnection.Direct:
				case LogicalDeviceActiveConnection.Cloud:
					return base.Product!.SoftwareUpdateStateLastKnown;
				case LogicalDeviceActiveConnection.Remote:
					return RemoteSoftwareUpdate.SoftwareUpdateState;
				default:
					return SoftwareUpdateState.Unknown;
				}
			}
		}

		public LogicalDeviceCloudGateway(ILogicalDeviceId logicalDeviceId, LogicalDeviceCloudGatewayStatus status, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, status, (ILogicalDeviceCapability)new LogicalDeviceCapability(), service, isFunctionClassChangeable)
		{
			RemoteOnline = new RemoteOnline(this, RemoteChannels);
			RemoteSoftwareUpdate = new RemoteSoftwareUpdate(this, RemoteChannels);
		}

		public LogicalDeviceCloudGateway(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: this(logicalDeviceId, new LogicalDeviceCloudGatewayStatus(), service, isFunctionClassChangeable)
		{
		}

		public override void OnDeviceOnlineChanged()
		{
			base.OnDeviceOnlineChanged();
			switch (ActiveConnection)
			{
			}
		}

		public async Task<string> ReadGatewayAssetId(CancellationToken cancellationToken)
		{
			if (!string.IsNullOrEmpty(CachedAssetId))
			{
				return CachedAssetId;
			}
			if (ActiveConnection != LogicalDeviceActiveConnection.Direct && ActiveConnection != LogicalDeviceActiveConnection.Cloud)
			{
				return null;
			}
			LogicalDevicePidText logicalDevicePidText = new LogicalDevicePidText(this, PID.CLOUD_GATEWAY_ASSET_ID_PART_1, LogicalDeviceSessionType.None);
			LogicalDevicePidText logicalDevicePidText2 = new LogicalDevicePidText(this, PID.CLOUD_GATEWAY_ASSET_ID_PART_2, LogicalDeviceSessionType.None);
			LogicalDevicePidText logicalDevicePidText3 = new LogicalDevicePidText(this, PID.CLOUD_GATEWAY_ASSET_ID_PART_3, LogicalDeviceSessionType.None);
			try
			{
				Task<string> assetIdPart1 = logicalDevicePidText.ReadTextAsync(cancellationToken);
				Task<string> assetIdPart2 = logicalDevicePidText2.ReadTextAsync(cancellationToken);
				Task<string> assetIdPart3 = logicalDevicePidText3.ReadTextAsync(cancellationToken);
				await Task.WhenAll<string>(assetIdPart1, assetIdPart2, assetIdPart3);
				return CachedAssetId = assetIdPart1.Result + assetIdPart2.Result + assetIdPart3.Result;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceCloudGateway", "Unable to read K-Code " + ex.Message);
				return null;
			}
		}

		public Task<CommandResult> SendSoftwareUpdateAuthorizationAsync(CancellationToken cancelToken)
		{
			switch (ActiveConnection)
			{
			case LogicalDeviceActiveConnection.Direct:
			case LogicalDeviceActiveConnection.Cloud:
				return base.Product!.SendSoftwareUpdateAuthorizationAsync(cancelToken);
			case LogicalDeviceActiveConnection.Remote:
				return RemoteSoftwareUpdate.SendSoftwareUpdateAuthorizationAsync(cancelToken);
			case LogicalDeviceActiveConnection.Offline:
				return Task.FromResult(CommandResult.ErrorDeviceOffline);
			default:
				return Task.FromResult(CommandResult.ErrorOther);
			}
		}

		public override void Dispose(bool disposing)
		{
			RemoteOnline?.TryDispose();
			RemoteOnline = null;
			RemoteSoftwareUpdate?.TryDispose();
			RemoteSoftwareUpdate = null;
			base.Dispose(disposing);
		}

		NETWORK_STATUS ILogicalDeviceIdsCan.get_LastReceivedNetworkStatus()
		{
			return base.LastReceivedNetworkStatus;
		}

		TRemoteChannelDef ILogicalDeviceRemote.GetRemoteChannelForChannelId<TRemoteChannelDef>(string channelId)
		{
			return GetRemoteChannelForChannelId<TRemoteChannelDef>(channelId);
		}
	}
}
