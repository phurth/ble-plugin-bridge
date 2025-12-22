using ids.portable.ble.BleManager;
using ids.portable.ble.BleScanner;

namespace ids.portable.ble.Ble
{
	public interface IBleService
	{
		IBleManager Manager { get; }

		IBleScannerService Scanner { get; }
	}
}
