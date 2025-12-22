using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDevice;
using OneControl.Devices.AccessoryGateway;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public interface IAccessoryPairingManager
	{
		ILogicalDeviceManager LogicalDeviceManager { get; }

		IAccessoryRegistrationManager AccessoryRegistrationManager { get; }

		TimeSpan PairWithRvTimeout { get; }

		TimeSpan UnpairWithRvTimeout { get; }

		ILogicalDeviceAccessoryGateway? GetAccessoryGatewayAssociatedWithRv();

		Task<bool> IsPairedWithRv(ILogicalDeviceAccessory? device, ILogicalDeviceAccessoryGateway? accessoryGateway, CancellationToken token);

		Task<bool> IsPairedOverBle(ILogicalDeviceAccessory? device, CancellationToken token);

		Task<bool> PairWithRv(ILogicalDeviceAccessory? device, ILogicalDeviceAccessoryGateway? accessoryGateway, CancellationToken token);

		Task<bool> UnpairWithRv(ILogicalDeviceAccessory? device, ILogicalDeviceAccessoryGateway? accessoryGateway, CancellationToken token);

		Task<bool> ResyncAccessoryGatewayDevices(CancellationToken token);

		Task TryPairLinkedDevicesWithPhone(CancellationToken cancellationToken);
	}
}
