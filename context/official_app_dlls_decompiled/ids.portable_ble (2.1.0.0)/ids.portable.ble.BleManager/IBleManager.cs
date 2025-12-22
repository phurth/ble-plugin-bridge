using System;
using System.Threading;
using System.Threading.Tasks;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace ids.portable.ble.BleManager
{
	public interface IBleManager
	{
		bool UseKeySeed { get; }

		event EventHandler<BondErrorEventArgs>? NotBonded;

		event EventHandler<BondErrorEventArgs>? PeripheralLostBondInfo;

		event EventHandler<DeviceEventArgs> DeviceAdvertised;

		event EventHandler<DeviceEventArgs> DeviceDiscovered;

		event EventHandler<DeviceEventArgs> DeviceConnected;

		event EventHandler<DeviceEventArgs> DeviceDisconnected;

		event EventHandler<DeviceErrorEventArgs> DeviceConnectionLost;

		Task<bool> UpdateRssi(IDevice? device);

		Task<IDevice?> TryConnectToDeviceAsync(Guid deviceId, CancellationToken cancellationToken);

		Task<IDevice> ConnectToDeviceAsync(Guid deviceId, CancellationToken cancellationToken);

		Task TryDisconnectDeviceAsync(IDevice device);

		Task DisconnectDeviceAsync(IDevice device);

		Task<IDevice?> ConnectToNonLippertDeviceAsync(Guid deviceId, CancellationToken cancellationToken);

		Task<IDevice> ConnectToDeviceAsync(IBleManagerConnectionParameters connectionParams, CancellationToken cancellationToken);

		Task<IService?> GetServiceAsync(IDevice device, Guid serviceGuid, CancellationToken cancellationToken);

		Task<ICharacteristic?> GetCharacteristicAsync(IDevice device, Guid serviceGuid, Guid characteristicGuid, CancellationToken cancellationToken);

		Task<ICharacteristic?> GetCharacteristicAsync(IDevice device, IService service, Guid characteristicGuid, CancellationToken cancellationToken);

		Task<byte[]?> ReadCharacteristicAsync(IDevice device, Guid serviceGuid, Guid characteristicGuid, CancellationToken cancellationToken);

		Task<byte[]?> ReadCharacteristicAsync(ICharacteristic characteristic, CancellationToken cancellationToken);

		Task<bool> WriteCharacteristicAsync(IDevice device, Guid serviceGuid, Guid characteristicGuid, byte[] data, CancellationToken cancellationToken);

		Task<bool> WriteCharacteristicWithResponseAsync(IDevice device, Guid serviceGuid, Guid characteristicGuid, byte[] data, CancellationToken cancellationToken);

		Task<bool> WriteCharacteristicAsync(ICharacteristic characteristic, byte[] data, CancellationToken cancellationToken);

		bool WriteCharacteristic(ICharacteristic characteristic, byte[] data);

		Task<bool> WriteCharacteristicWithResponse(ICharacteristic characteristic, byte[] data);

		Task<bool> WriteCharacteristicWithResponseAsync(ICharacteristic characteristic, byte[] data, CancellationToken cancellationToken);

		Task<bool> StartCharacteristicUpdatesAsync(ICharacteristic characteristic);

		void StopCharacteristicUpdates(ICharacteristic characteristic);

		void SetUseKeySeed(bool useKeySeed);

		void GoToDeviceSettings();
	}
}
