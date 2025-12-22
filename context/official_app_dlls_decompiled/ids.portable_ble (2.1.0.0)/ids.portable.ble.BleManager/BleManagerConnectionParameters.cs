using System;
using ids.portable.ble.Platforms.Shared.ScanResults;

namespace ids.portable.ble.BleManager
{
	public readonly struct BleManagerConnectionParameters : IBleManagerConnectionParameters
	{
		public Guid DeviceId { get; }

		public string DeviceName { get; }

		public PairingMethod PairingMethod { get; }

		public uint? KeySeedCypher { get; }

		public int? ConnectionTimeoutMs { get; }

		public BleManagerConnectionParameters(Guid deviceId, string deviceName, PairingMethod pairingMethod, uint? keySeedCypher, int? connectionTimeout = null)
		{
			DeviceId = deviceId;
			DeviceName = deviceName ?? string.Empty;
			PairingMethod = pairingMethod;
			KeySeedCypher = keySeedCypher;
			ConnectionTimeoutMs = connectionTimeout;
		}
	}
}
