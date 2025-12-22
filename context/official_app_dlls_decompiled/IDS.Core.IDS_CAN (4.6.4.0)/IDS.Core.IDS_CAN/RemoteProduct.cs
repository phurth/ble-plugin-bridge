using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IDS.Core.IDS_CAN
{
	internal class RemoteProduct : IRemoteProduct, IProduct, IBusEndpoint, IUniqueProductInfo, IEnumerable<IDevice>, IEnumerable, IEnumerable<IRemoteDevice>
	{
		private readonly ConcurrentDictionary<ulong, RemoteDevice> Devices = new ConcurrentDictionary<ulong, RemoteDevice>();

		private readonly DTCManager DTCManager;

		private readonly ulong ProductUniqueID;

		public string Name { get; private set; }

		public string UniqueName { get; private set; }

		public IAdapter Adapter { get; private set; }

		public MAC MAC { get; private set; }

		public IDS_CAN_VERSION_NUMBER ProtocolVersion { get; private set; }

		public PRODUCT_ID ProductID { get; private set; }

		public byte ProductInstance { get; private set; }

		public ADDRESS Address => ProductInstance;

		public int AssemblyPartNumber => ProductID.AssemblyPartNumber;

		public int DeviceCount => Devices.Count;

		public bool IsOnline => !Devices.IsEmpty;

		public SOFTWARE_UPDATE_STATE SoftwareUpdateState { get; private set; }

		public IDTCManager DTCs { get; private set; }

		public IEnumerator<IRemoteDevice> GetEnumerator()
		{
			return Devices.Values.GetEnumerator();
		}

		IEnumerator<IDevice> IEnumerable<IDevice>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public RemoteProduct(IAdapter adapter, PRODUCT_ID product_id, MAC mac, IDS_CAN_VERSION_NUMBER protocol_version, byte product_instance)
		{
			Adapter = adapter;
			ProductID = product_id;
			MAC = new MAC(mac);
			ProtocolVersion = protocol_version;
			ProductInstance = product_instance;
			Name = ProductID.ToString();
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 2);
			defaultInterpolatedStringHandler.AppendFormatted(ProductID);
			defaultInterpolatedStringHandler.AppendLiteral(" MAC[");
			defaultInterpolatedStringHandler.AppendFormatted(MAC);
			defaultInterpolatedStringHandler.AppendLiteral("]");
			UniqueName = defaultInterpolatedStringHandler.ToStringAndClear();
			DTCManager = new DTCManager(this);
			DTCs = DTCManager;
			ProductUniqueID = this.GetProductUniqueID();
		}

		public override string ToString()
		{
			return Name;
		}

		public void OnProductTx(AdapterRxEvent tx)
		{
			if ((byte)tx.MessageType == 6 && tx.Count >= 1)
			{
				SoftwareUpdateState = (byte)(tx[0] & 3u);
			}
			DTCManager.OnProductTx(tx);
		}

		internal void AddOrUpdateDevice(RemoteDevice device)
		{
			UpdateInstance(device.ProductInstance);
			RemoteDevice prev = null;
			lock (Devices)
			{
				Devices.AddOrUpdate(device.GetDeviceUniqueID(), device, delegate(ulong key, RemoteDevice value)
				{
					prev = value;
					return device;
				});
			}
			if (prev != device)
			{
				prev?.GoOffline();
			}
		}

		internal void UpdateInstance(byte instance)
		{
			if (instance > 0)
			{
				ProductInstance = instance;
			}
		}

		internal void RemoveOfflineDevice(RemoteDevice device)
		{
			ulong deviceUniqueID = device.GetDeviceUniqueID();
			lock (Devices)
			{
				if (Devices.TryGetValue(deviceUniqueID, out var remoteDevice) && remoteDevice == device)
				{
					Devices.TryRemove(deviceUniqueID, out var _);
				}
			}
		}
	}
}
