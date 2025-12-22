using System.Threading;
using System.Threading.Tasks;
using Plugin.BLE.Abstractions.Contracts;

namespace ids.portable.ble.BleManager
{
	internal abstract class BleManagerNative : IBleManagerNative
	{
		protected readonly IBluetoothLE Ble;

		protected readonly IAdapter BleAdapter;

		public virtual bool IsExplicitServiceUuidScanningSupported { get; }

		public virtual int ScanTimeoutMs { get; }

		public virtual int ScannerStopDelayMs { get; }

		protected BleManagerNative(IBluetoothLE ble)
		{
			Ble = ble;
			BleAdapter = Ble.Adapter;
		}

		public virtual bool Bonded(IDevice? device)
		{
			return false;
		}

		public virtual Task<bool> CreateBond(IDevice? device, CancellationToken cancellationToken)
		{
			return Task.FromResult(false);
		}

		public virtual void GoToDeviceSettings()
		{
		}

		public virtual void BleDeviceCacheBust(IDevice device)
		{
		}
	}
}
