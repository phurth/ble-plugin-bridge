using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	internal class DeviceDiscoverer : Adapter.BackgroundTaskObject, IDeviceDiscoverer, IEnumerable<IRemoteDevice>, IEnumerable
	{
		private class DeviceCache : Disposable
		{
			private static readonly TimeSpan DEVICE_TIMEOUT = TimeSpan.FromSeconds(5.0);

			private static readonly TimeSpan FORCE_STATUS_MESSAGE_TIME = TimeSpan.FromSeconds(3.5);

			private readonly object Lock = new object();

			private readonly DeviceDiscoverer Parent;

			private readonly Adapter Adapter;

			private readonly ADDRESS Address;

			private readonly MAC MAC = new MAC();

			private readonly MAC TempMAC = new MAC();

			private RemoteDevice mDevice;

			private IDS_CAN_VERSION_NUMBER ProtocolVersion = IDS_CAN_VERSION_NUMBER.UNKNOWN;

			private DEVICE_ID? DeviceID;

			private CIRCUIT_ID? CircuitID;

			private CAN.PAYLOAD? DeviceStatus;

			private Timer LastNetworkMsgTime = new Timer();

			private Timer DeviceDetectedTime = new Timer();

			private bool HaveMAC => ProtocolVersion != IDS_CAN_VERSION_NUMBER.UNKNOWN;

			public RemoteDevice Device
			{
				get
				{
					return mDevice;
				}
				private set
				{
					if (mDevice == value)
					{
						return;
					}
					RemoteDevice remoteDevice;
					lock (Lock)
					{
						remoteDevice = Interlocked.Exchange(ref mDevice, value);
						if (remoteDevice != null)
						{
							Parent.Dictionary.TryRemove(remoteDevice.GetDeviceUniqueID(), out var _);
						}
						if (value != null)
						{
							Parent.Dictionary.AddOrUpdate(value.GetDeviceUniqueID(), value, (ulong key, RemoteDevice val) => value);
						}
						if (value != null)
						{
							if (remoteDevice == null)
							{
								Interlocked.Increment(ref Parent.mNumDevicesDetectedOnNetwork);
							}
						}
						else if (remoteDevice != null)
						{
							Interlocked.Decrement(ref Parent.mNumDevicesDetectedOnNetwork);
						}
					}
					if (base.IsDisposed)
					{
						remoteDevice?.Dispose();
						return;
					}
					remoteDevice?.GoOffline();
					if (value != null)
					{
						Task.Run(delegate
						{
							Adapter.Events.Publish(new RemoteDeviceOnlineEvent(Adapter, value));
						});
					}
				}
			}

			public DeviceCache(DeviceDiscoverer parent, Adapter adapter, ADDRESS address)
			{
				Parent = parent;
				Adapter = adapter;
				Address = address;
			}

			public override void Dispose(bool disposing)
			{
				if (disposing)
				{
					TakeDeviceOffline();
				}
			}

			public void ForceDeviceOnline(ILocalDevice device)
			{
				if (!base.IsDisposed)
				{
					TakeDeviceOffline();
					MAC.CopyFrom(device.MAC);
					ProtocolVersion = device.ProtocolVersion;
					DeviceID = device.GetDeviceID();
					CircuitID = device.CircuitID;
					DeviceStatus = device.DeviceStatus;
					DeviceDetectedTime.Reset();
					LastNetworkMsgTime.Reset();
					TryCreateRemoteDevice();
				}
			}

			private void TryCreateRemoteDevice()
			{
				RemoteDevice remoteDevice = null;
				try
				{
					remoteDevice = new RemoteDevice(Adapter, Address, MAC, ProtocolVersion, DeviceID.Value, CircuitID.Value, DeviceStatus.Value);
				}
				catch
				{
					return;
				}
				Device = remoteDevice;
			}

			public void TakeDeviceOffline()
			{
				Device = null;
				MAC.Clear();
				ProtocolVersion = IDS_CAN_VERSION_NUMBER.UNKNOWN;
				CircuitID = null;
				DeviceID = null;
				DeviceStatus = null;
			}

			public void BackgroundTask()
			{
				if (HaveMAC && LastNetworkMsgTime.ElapsedTime >= DEVICE_TIMEOUT)
				{
					TakeDeviceOffline();
					return;
				}
				RemoteDevice device = Device;
				if (device != null)
				{
					if (device.IsOnline)
					{
						device.BackgroundTask();
					}
					else
					{
						TakeDeviceOffline();
					}
				}
			}

			public void OnAdapterMessageRx(AdapterRxEvent rx)
			{
				if (base.IsDisposed || !Parent.Adapter.IsConnected)
				{
					return;
				}
				switch ((byte)rx.MessageType)
				{
				case 0:
				{
					if (!TempMAC.UnloadFromMessage(rx))
					{
						break;
					}
					IDS_CAN_VERSION_NUMBER iDS_CAN_VERSION_NUMBER = rx[1];
					LastNetworkMsgTime.Reset();
					if (MAC != TempMAC || ProtocolVersion != iDS_CAN_VERSION_NUMBER)
					{
						TakeDeviceOffline();
						MAC.CopyFrom(TempMAC);
						ProtocolVersion = iDS_CAN_VERSION_NUMBER;
						DeviceDetectedTime.Reset();
						break;
					}
					if (!DeviceStatus.HasValue && DeviceDetectedTime.ElapsedTime >= FORCE_STATUS_MESSAGE_TIME)
					{
						DeviceStatus = default(CAN.PAYLOAD);
					}
					if (Device == null && ProtocolVersion != IDS_CAN_VERSION_NUMBER.UNKNOWN && DeviceID.HasValue && CircuitID.HasValue && DeviceStatus.HasValue)
					{
						TryCreateRemoteDevice();
					}
					break;
				}
				case 1:
					if (HaveMAC && rx.Count >= 4)
					{
						CircuitID = rx.GetUINT32(0);
					}
					break;
				case 2:
					if (HaveMAC && rx.Count >= 7)
					{
						if (rx.Count >= 8)
						{
							DeviceID = new DEVICE_ID(rx.GetUINT16(0), rx[2], rx[3], rx[6] >> 4, rx.GetUINT16(4), rx[6] & 0xF, rx[7]);
						}
						else
						{
							DeviceID = new DEVICE_ID(rx.GetUINT16(0), rx[2], rx[3], rx[6] >> 4, rx.GetUINT16(4), rx[6] & 0xF, null);
						}
						RemoteDevice device = Device;
						if (device != null && (device.ProductID != DeviceID?.ProductID || device.DeviceType != DeviceID?.DeviceType || device.DeviceInstance != DeviceID?.DeviceInstance))
						{
							TakeDeviceOffline();
						}
					}
					break;
				case 3:
					if (HaveMAC)
					{
						DeviceStatus = rx.Payload;
					}
					break;
				}
				Device?.OnAdapterMessageRx(rx);
			}

			public void OnBroadcastAddressClaim(AdapterRxEvent rx)
			{
				if (rx.SourceAddress == ADDRESS.BROADCAST && (byte)rx.MessageType == 0 && rx.Count == 8 && rx[0] == (byte)Address && HaveMAC && TempMAC.UnloadFromMessage(rx) && TempMAC.CompareTo(MAC) < 0)
				{
					TakeDeviceOffline();
				}
			}
		}

		private readonly DeviceCache[] Cache = new DeviceCache[256];

		private readonly ConcurrentDictionary<ulong, RemoteDevice> Dictionary = new ConcurrentDictionary<ulong, RemoteDevice>();

		private int mNumDevicesDetectedOnNetwork;

		public int NumDevicesDetectedOnNetwork => mNumDevicesDetectedOnNetwork;

		public DeviceDiscoverer(Adapter adapter)
			: base(adapter)
		{
			foreach (ADDRESS item in ADDRESS.GetEnumerator())
			{
				if (item.IsValidDeviceAddress)
				{
					Cache[(byte)item] = new DeviceCache(this, adapter, item);
				}
			}
			base.Adapter.Events.Subscribe<Comm.AdapterOpenedEvent>(OnAdapterOpened, SubscriptionType.Strong, base.Subscriptions);
			base.Adapter.Events.Subscribe<Comm.AdapterClosedEvent>(OnAdapterClosed, SubscriptionType.Strong, base.Subscriptions);
			base.Adapter.Events.Subscribe<AdapterRxEvent>(OnAdapterRx, SubscriptionType.Strong, base.Subscriptions);
			base.Adapter.Events.Subscribe<LocalDeviceOnlineEvent>(OnLocalDeviceOnline, SubscriptionType.Strong, base.Subscriptions);
			base.Adapter.Events.Subscribe<LocalDeviceOfflineEvent>(OnLocalDeviceOffline, SubscriptionType.Strong, base.Subscriptions);
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				DeviceCache[] cache = Cache;
				for (int i = 0; i < cache.Length; i++)
				{
					cache[i]?.Dispose();
				}
			}
		}

		IEnumerator<IRemoteDevice> IEnumerable<IRemoteDevice>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<RemoteDevice> GetEnumerator()
		{
			if (!base.Adapter.IsConnected)
			{
				yield break;
			}
			DeviceCache[] cache = Cache;
			for (int i = 0; i < cache.Length; i++)
			{
				RemoteDevice remoteDevice = cache[i]?.Device;
				if (remoteDevice != null && remoteDevice.IsOnline)
				{
					yield return remoteDevice;
				}
			}
		}

		public IEnumerable<IRemoteDevice> GetAllDevicesMatchingFilter(Func<IDevice, bool> filter)
		{
			using IEnumerator<RemoteDevice> enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				RemoteDevice current = enumerator.Current;
				if (current != null && filter(current))
				{
					yield return current;
				}
			}
		}

		public IRemoteDevice GetDeviceByAddress(ADDRESS address)
		{
			if (address == null || !address.IsValidDeviceAddress)
			{
				return null;
			}
			return Cache[(byte)address]?.Device;
		}

		public IRemoteDevice GetDeviceByUniqueID(ulong unique_id)
		{
			Dictionary.TryGetValue(unique_id, out var result);
			return result;
		}

		private void KillAllDevices()
		{
			DeviceCache[] cache = Cache;
			for (int i = 0; i < cache.Length; i++)
			{
				cache[i]?.TakeDeviceOffline();
			}
		}

		private void OnAdapterOpened(Comm.AdapterOpenedEvent message)
		{
			KillAllDevices();
		}

		private void OnAdapterClosed(Comm.AdapterClosedEvent message)
		{
			KillAllDevices();
		}

		private void OnLocalDeviceOnline(LocalDeviceOnlineEvent message)
		{
			Cache[(byte)message.Device.Address]?.ForceDeviceOnline(message.Device);
		}

		private void OnLocalDeviceOffline(LocalDeviceOfflineEvent message)
		{
			Cache[(byte)message.PrevAddress]?.TakeDeviceOffline();
		}

		private void OnAdapterRx(AdapterRxEvent rx)
		{
			if (rx.SourceAddress == ADDRESS.BROADCAST && (byte)rx.MessageType == 0 && rx.Count == 8)
			{
				ADDRESS aDDRESS = rx[0];
				Cache[(byte)aDDRESS]?.OnBroadcastAddressClaim(rx);
			}
			Cache[(byte)rx.SourceAddress]?.OnAdapterMessageRx(rx);
		}

		public override void BackgroundTask()
		{
			DeviceCache[] cache = Cache;
			for (int i = 0; i < cache.Length; i++)
			{
				cache[i]?.BackgroundTask();
			}
		}
	}
}
