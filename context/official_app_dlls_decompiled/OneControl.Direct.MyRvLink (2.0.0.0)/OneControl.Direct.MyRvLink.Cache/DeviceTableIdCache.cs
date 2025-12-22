using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OneControl.Direct.MyRvLink.Devices;

namespace OneControl.Direct.MyRvLink.Cache
{
	public class DeviceTableIdCache
	{
		private readonly IDirectConnectionMyRvLink _directManager;

		private DeviceTableIdCacheSerializable? _deviceTableIdCacheSerializable;

		public DeviceTableIdCache(IDirectConnectionMyRvLink directManager)
		{
			_directManager = directManager;
		}

		public async Task<DeviceTableIdCacheSerializable> GetDeviceTableIdCacheSerializableAsync()
		{
			DeviceTableIdCacheSerializable deviceTableIdCacheSerializable = _deviceTableIdCacheSerializable;
			if (deviceTableIdCacheSerializable == null)
			{
				DeviceTableIdCacheSerializable deviceTableIdCacheSerializable2 = await DeviceTableIdCacheSerializable.TryLoadAsync(_directManager.DeviceSourceToken);
				DeviceTableIdCache deviceTableIdCache = this;
				DeviceTableIdCacheSerializable obj = deviceTableIdCacheSerializable2 ?? new DeviceTableIdCacheSerializable(_directManager.DeviceSourceToken);
				DeviceTableIdCacheSerializable deviceTableIdCacheSerializable3 = obj;
				deviceTableIdCache._deviceTableIdCacheSerializable = obj;
				deviceTableIdCacheSerializable = deviceTableIdCacheSerializable3;
			}
			return deviceTableIdCacheSerializable;
		}

		public async Task<bool> UpdateDevicesAsync(uint deviceTableCrc, byte deviceTableId, IReadOnlyList<IMyRvLinkDevice> deviceList)
		{
			DeviceTableIdCacheSerializable obj = await GetDeviceTableIdCacheSerializableAsync();
			MyRvLinkDeviceTableSerializable deviceTableSerializable = new MyRvLinkDeviceTableSerializable(deviceTableCrc, Enumerable.ToList(Enumerable.Select(deviceList, (IMyRvLinkDevice device) => new MyRvLinkDeviceSerializable(device))), DateTime.Now);
			obj.Update(deviceTableId, deviceTableSerializable);
			return await obj.TrySaveAsync();
		}

		public async Task<IReadOnlyList<IMyRvLinkDevice>?> GetDevicesForDeviceTableCrcAsync(uint deviceTableCrc)
		{
			return (await GetDeviceTableIdCacheSerializableAsync()).GetFirstDeviceTableIdSerializableForCrc(deviceTableCrc)?.TryDecode();
		}

		public async Task<IReadOnlyList<IMyRvLinkDevice>?> GetDevicesForDeviceTableIdAsync(byte deviceTableId)
		{
			return (await GetDeviceTableIdCacheSerializableAsync()).GetDeviceTableIdSerializableForTableId(deviceTableId)?.TryDecode();
		}
	}
}
