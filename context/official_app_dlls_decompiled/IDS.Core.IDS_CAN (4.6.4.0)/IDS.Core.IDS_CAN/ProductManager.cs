using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	internal class ProductManager : Disposable, IProductManager, IEnumerable<IRemoteProduct>, IEnumerable
	{
		private readonly ConcurrentDictionary<ulong, RemoteProduct> Products = new ConcurrentDictionary<ulong, RemoteProduct>();

		private readonly ProductListChangedEvent ProductListChangedEvent;

		private readonly SubscriptionManager Subscriptions = new SubscriptionManager();

		public IAdapter Adapter { get; private set; }

		public int Count => Products.Count;

		public int TotalProducts => Products.Count;

		public int TotalDevices
		{
			get
			{
				int num = 0;
				using IEnumerator<IRemoteProduct> enumerator = GetEnumerator();
				while (enumerator.MoveNext())
				{
					RemoteProduct remoteProduct = (RemoteProduct)enumerator.Current;
					num += remoteProduct.DeviceCount;
				}
				return num;
			}
		}

		public ProductManager(Adapter adapter)
		{
			Adapter = adapter;
			Adapter.AddDisposable(this);
			ProductListChangedEvent = new ProductListChangedEvent(this);
			Adapter.Events.Subscribe<Comm.AdapterOpenedEvent>(OnAdapterOpened, SubscriptionType.Strong, Subscriptions);
			Adapter.Events.Subscribe<Comm.AdapterClosedEvent>(OnAdapterClosed, SubscriptionType.Strong, Subscriptions);
			Adapter.Events.Subscribe<RemoteDeviceOnlineEvent>(OnRemoteDeviceOnline, SubscriptionType.Strong, Subscriptions);
			Adapter.Events.Subscribe<RemoteDeviceOfflineEvent>(OnRemoteDeviceOffline, SubscriptionType.Strong, Subscriptions);
			Adapter.Events.Subscribe<AdapterRxEvent>(OnAdapterRx, SubscriptionType.Strong, Subscriptions);
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Clear();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<IRemoteProduct> GetEnumerator()
		{
			foreach (RemoteProduct value in Products.Values)
			{
				if (value != null && value.IsOnline)
				{
					yield return value;
				}
			}
		}

		private void PublishChangeEvent()
		{
			if (!base.IsDisposed)
			{
				Task.Run(delegate
				{
					ProductListChangedEvent.Publish();
				});
			}
		}

		public IRemoteProduct GetProduct(ADDRESS address)
		{
			return Adapter.Devices.GetDeviceByAddress(address)?.Product;
		}

		public IRemoteProduct GetProduct(ulong unique_id)
		{
			Products.TryGetValue(unique_id, out var result);
			return result;
		}

		private void OnAdapterOpened(Comm.AdapterOpenedEvent e)
		{
			Clear();
		}

		private void OnAdapterClosed(Comm.AdapterClosedEvent e)
		{
			Clear();
		}

		private void OnRemoteDeviceOnline(RemoteDeviceOnlineEvent message)
		{
			PublishChangeEvent();
		}

		private void OnRemoteDeviceOffline(RemoteDeviceOfflineEvent message)
		{
			if (base.IsDisposed)
			{
				return;
			}
			if (!Adapter.IsConnected)
			{
				Clear();
				return;
			}
			if (message != null && message.Device?.Product?.DeviceCount <= 0)
			{
				ulong productUniqueID = message.Device.Product.GetProductUniqueID();
				if (Products.TryGetValue(productUniqueID, out var remoteProduct) && message.Device.Product == remoteProduct)
				{
					Products.TryRemove(productUniqueID, out var _);
				}
			}
			PublishChangeEvent();
		}

		private void OnAdapterRx(AdapterRxEvent rx)
		{
			if (GetProduct(rx.SourceAddress) is RemoteProduct remoteProduct && remoteProduct.Address == rx.SourceAddress)
			{
				remoteProduct.OnProductTx(rx);
			}
		}

		private void Clear()
		{
			if (!Products.IsEmpty)
			{
				Products.Clear();
				PublishChangeEvent();
			}
		}

		internal RemoteProduct LocateOrCreateProductForDevice(RemoteDevice device)
		{
			ulong productUniqueID = device.GetProductUniqueID();
			Products.TryGetValue(productUniqueID, out var orAdd);
			if (orAdd == null)
			{
				orAdd = Products.GetOrAdd(productUniqueID, (ulong k) => new RemoteProduct(device.Adapter, device.ProductID, device.MAC, device.ProtocolVersion, device.ProductInstance));
			}
			orAdd?.UpdateInstance(device.ProductInstance);
			return orAdd;
		}

		public override string ToString()
		{
			try
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Network contains ");
				defaultInterpolatedStringHandler.AppendFormatted(TotalDevices);
				defaultInterpolatedStringHandler.AppendLiteral(" devices in ");
				defaultInterpolatedStringHandler.AppendFormatted(TotalProducts);
				defaultInterpolatedStringHandler.AppendLiteral(" products");
				string text = defaultInterpolatedStringHandler.ToStringAndClear();
				using (IEnumerator<IRemoteProduct> enumerator = GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						RemoteProduct remoteProduct = (RemoteProduct)enumerator.Current;
						text = text + "\n\t" + remoteProduct.Name + " [";
						int num = 0;
						foreach (IRemoteDevice item in remoteProduct)
						{
							if (num++ > 0)
							{
								text += ", ";
							}
							text += item.ToShortString(show_address: false);
						}
						text += "]";
					}
				}
				return text;
			}
			catch (Exception)
			{
			}
			return null;
		}
	}
}
