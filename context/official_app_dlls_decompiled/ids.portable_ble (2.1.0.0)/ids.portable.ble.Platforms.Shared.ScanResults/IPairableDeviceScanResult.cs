using ids.portable.ble.ScanResults;

namespace ids.portable.ble.Platforms.Shared.ScanResults
{
	public interface IPairableDeviceScanResult : IBleScanResultWithIdsManufacturingData, IBleScanResult
	{
		PairingMethod PairingMethod { get; }

		bool PairingEnabled { get; }
	}
}
