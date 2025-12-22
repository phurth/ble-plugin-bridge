using System;
using ids.portable.ble.Platforms.Shared.ScanResults;

namespace ids.portable.ble.BleManager
{
	public interface IBleManagerConnectionParameters
	{
		Guid DeviceId { get; }

		string DeviceName { get; }

		PairingMethod PairingMethod { get; }

		uint? KeySeedCypher { get; }

		int? ConnectionTimeoutMs { get; }
	}
}
