using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceCloudGateway : ICloudGateway, ILogicalDeviceWithStatus<LogicalDeviceCloudGatewayStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<LogicalDeviceCloudGatewayStatus>, ILogicalDeviceMyRvLink, ILogicalDeviceIdsCan
	{
		SoftwareUpdateState SoftwareUpdateState { get; }

		Task<CommandResult> SendSoftwareUpdateAuthorizationAsync(CancellationToken cancelToken);
	}
}
