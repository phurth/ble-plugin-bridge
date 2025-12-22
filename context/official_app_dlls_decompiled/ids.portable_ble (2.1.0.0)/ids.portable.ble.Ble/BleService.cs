using ids.portable.ble.BleManager;
using ids.portable.ble.BleScanner;

namespace ids.portable.ble.Ble
{
	internal class BleService : IBleService
	{
		public IBleManager Manager { get; }

		public IBleScannerService Scanner { get; }

		public BleService(IBleManager bleManager, IBleScannerService bleScannerService)
		{
			Manager = bleManager;
			Scanner = bleScannerService;
		}
	}
}
