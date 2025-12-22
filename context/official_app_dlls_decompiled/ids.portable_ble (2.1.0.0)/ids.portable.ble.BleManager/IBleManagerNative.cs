using System.Threading;
using System.Threading.Tasks;
using Plugin.BLE.Abstractions.Contracts;

namespace ids.portable.ble.BleManager
{
	public interface IBleManagerNative
	{
		bool Bonded(IDevice? device);

		Task<bool> CreateBond(IDevice? device, CancellationToken cancellationToken);

		void GoToDeviceSettings();

		void BleDeviceCacheBust(IDevice device);
	}
}
