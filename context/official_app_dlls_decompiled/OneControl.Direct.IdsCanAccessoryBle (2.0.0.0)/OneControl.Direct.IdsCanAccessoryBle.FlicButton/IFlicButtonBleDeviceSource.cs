using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.FlicButton
{
	public interface IFlicButtonBleDeviceSource : ILogicalDeviceSourceDirectConnection, ILogicalDeviceSourceDirect, ILogicalDeviceSource, ILogicalDeviceSourceConnection, IAccessoryBleDeviceSource<SensorConnectionFlic>, IAccessoryBleDeviceSource, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata, IAccessoryBleDeviceSourceDevices<IFlicButtonBleDeviceDriver>
	{
		Task<SensorConnectionFlic?> ScanAndPairFlicButton(CancellationToken cancellationToken);

		Task<bool> UnpairFlicButtonAsync(MAC mac);
	}
}
