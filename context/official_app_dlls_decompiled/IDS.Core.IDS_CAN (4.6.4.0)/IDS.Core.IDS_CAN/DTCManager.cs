using System;
using System.Collections;
using System.Collections.Generic;
using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	internal class DTCManager : Disposable, IDTCManager, IEnumerable<IProductDTC>, IEnumerable
	{
		private static readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(1.0);

		private readonly IAdapter Adapter;

		private ProductDTC[] DTCs;

		private readonly List<IProductDTC> SortedList = new List<IProductDTC>();

		private readonly Dictionary<DTC_ID, ProductDTC> Lookup = new Dictionary<DTC_ID, ProductDTC>();

		private readonly SubscriptionManager Subscriptions = new SubscriptionManager();

		private readonly Timer RetryTimer = new Timer();

		private readonly Timer OperationTimer = new Timer();

		private bool ReadingList;

		private ushort DtcIndex;

		private ProductDTCsChangedEvent ProductDTCsChangedEvent;

		public IRemoteProduct Product { get; private set; }

		public bool AreSupported
		{
			get
			{
				ProductDTC[] dTCs = DTCs;
				if (dTCs == null)
				{
					return false;
				}
				return dTCs.Length != 0;
			}
		}

		public bool HasActiveDTCs => GetDeviceAtProductAddress()?.NetworkStatus.HasActiveDTCs ?? false;

		public bool HasStoredDTCs => GetDeviceAtProductAddress()?.NetworkStatus.HasStoredDTCs ?? false;

		public int Count => SortedList.Count;

		public IEnumerator<IProductDTC> ActiveDTCs => GetActiveEnumerator();

		public IEnumerator<IProductDTC> StoredDTCs => GetStoredEnumerator();

		public int ActiveCount { get; private set; }

		public int StoredCount { get; private set; }

		private ProductDTC this[DTC_ID id]
		{
			get
			{
				Lookup.TryGetValue(id, out var value);
				return value;
			}
		}

		public DTCManager(IRemoteProduct product)
		{
			Product = product;
			Adapter = Product.Adapter;
			ProductDTCsChangedEvent = new ProductDTCsChangedEvent(Product);
			RetryTimer.ElapsedTime = TIMEOUT;
			Adapter.Events.Subscribe<Comm.TransmitTurnEvent>(OnTransmitNextMessage, SubscriptionType.Weak, Subscriptions);
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Subscriptions.Dispose();
			}
		}

		private IRemoteDevice GetDeviceAtProductAddress()
		{
			return Adapter.Devices.GetDeviceByAddress(Product.Address);
		}

		public IEnumerator<IProductDTC> GetEnumerator()
		{
			return SortedList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public IEnumerator<IProductDTC> GetActiveEnumerator()
		{
			using IEnumerator<IProductDTC> enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				IProductDTC current = enumerator.Current;
				if (current != null && current.IsActive)
				{
					yield return current;
				}
			}
		}

		public IEnumerator<IProductDTC> GetStoredEnumerator()
		{
			using IEnumerator<IProductDTC> enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				IProductDTC current = enumerator.Current;
				if (current != null && current.IsStored)
				{
					yield return current;
				}
			}
		}

		private void Publish(IProductDTC dtc)
		{
			int num = 0;
			int num2 = 0;
			ProductDTC[] dTCs = DTCs;
			foreach (ProductDTC obj in dTCs)
			{
				if (obj.IsActive)
				{
					num++;
				}
				if (obj.IsStored)
				{
					num2++;
				}
			}
			ActiveCount = num;
			StoredCount = num2;
			ProductDTCsChangedEvent.Publish(dtc);
		}

		public void QueryProduct()
		{
			if (!ReadingList && (DTCs == null || DTCs.Length != 0))
			{
				OperationTimer.Reset();
				ReadingList = true;
				DtcIndex = 0;
				RetryTimer.ElapsedTime = TIMEOUT;
			}
		}

		public bool Contains(DTC_ID id)
		{
			return this[id] != null;
		}

		public IProductDTC GetDTC(DTC_ID id)
		{
			return this[id];
		}

		public void OnProductTx(AdapterRxEvent tx)
		{
			IRemoteDevice deviceAtProductAddress = GetDeviceAtProductAddress();
			if (deviceAtProductAddress != null && deviceAtProductAddress.IsOnline && (byte)tx.MessageType == 129 && tx.TargetAddress == Adapter.LocalHost.Address)
			{
				switch (tx.MessageData)
				{
				case 49:
					OnContinuousDTCCommand(tx);
					break;
				case 48:
					OnReadContinuousDTCs(tx);
					break;
				}
			}
		}

		private void BadResponse(REQUEST request, RESPONSE response)
		{
			ReadingList = false;
			DTCs = new ProductDTC[0];
			SortedList.Clear();
			ActiveCount = 0;
			StoredCount = 0;
		}

		private void OnContinuousDTCCommand(AdapterRxEvent tx)
		{
			if (tx.Count == 1)
			{
				if (DTCs == null)
				{
					BadResponse((byte)49, (RESPONSE)tx[0]);
				}
			}
			else if (tx.Count == 8)
			{
				int uINT = tx.GetUINT16(2);
				if (DTCs == null)
				{
					DTCs = new ProductDTC[uINT];
				}
			}
		}

		private void OnReadContinuousDTCs(AdapterRxEvent tx)
		{
			if (!AreSupported || !ReadingList || DTCs == null)
			{
				return;
			}
			switch (tx.Count)
			{
			case 1:
				BadResponse((byte)48, (RESPONSE)tx[0]);
				break;
			case 8:
			{
				ushort uINT = tx.GetUINT16(0);
				int num = uINT * 2;
				int num2 = 2;
				while (num2 < 8 && num < DTCs.Length)
				{
					DTC_ID uINT2 = (DTC_ID)tx.GetUINT16(num2);
					if (uINT2 != 0)
					{
						byte status = tx[num2 + 2];
						if (DTCs[num] == null)
						{
							DTCs[num] = new ProductDTC(Product, uINT2, status);
						}
						else if (DTCs[num].Update(status) && Count > 0)
						{
							Publish(DTCs[num]);
						}
					}
					num2 += 3;
					num++;
				}
				if (num >= DTCs.Length)
				{
					ReadingList = false;
					if (SortedList.Count == DTCs.Length)
					{
						break;
					}
					SortedList.Clear();
					ProductDTC[] dTCs = DTCs;
					foreach (ProductDTC productDTC in dTCs)
					{
						Lookup.Add(productDTC.ID, productDTC);
						SortedList.Add(productDTC);
					}
					SortedList.Sort((IProductDTC first, IProductDTC second) => first.ID.CompareTo(second.ID));
					using IEnumerator<IProductDTC> enumerator = GetEnumerator();
					while (enumerator.MoveNext())
					{
						IProductDTC current = enumerator.Current;
						Publish(current);
					}
					break;
				}
				if (ReadingList)
				{
					if (uINT == DtcIndex)
					{
						DtcIndex++;
					}
					RetryTimer.ElapsedTime = TIMEOUT;
				}
				break;
			}
			}
		}

		private void OnTransmitNextMessage(Comm.TransmitTurnEvent e)
		{
			if (!Product.IsOnline || !Adapter.LocalHost.IsOnline)
			{
				return;
			}
			IRemoteDevice deviceAtProductAddress = GetDeviceAtProductAddress();
			if (deviceAtProductAddress == null || !deviceAtProductAddress.IsOnline)
			{
				return;
			}
			if (Count > 0)
			{
				if (!deviceAtProductAddress.NetworkStatus.HasDTCs)
				{
					if (ActiveCount <= 0 && StoredCount <= 0)
					{
						return;
					}
					ProductDTC[] dTCs = DTCs;
					foreach (ProductDTC productDTC in dTCs)
					{
						if (productDTC.Update(0))
						{
							Publish(productDTC);
						}
					}
					return;
				}
				if (!HasActiveDTCs && ActiveCount > 0)
				{
					ProductDTC[] dTCs = DTCs;
					foreach (ProductDTC productDTC2 in dTCs)
					{
						if (productDTC2.Update((byte)(productDTC2.Status & 0x7Fu)))
						{
							Publish(productDTC2);
						}
					}
				}
			}
			if (RetryTimer.ElapsedTime < TIMEOUT)
			{
				return;
			}
			if (DTCs == null)
			{
				if (ReadingList || Adapter.Options.HasFlag(ADAPTER_OPTIONS.AUTO_READ_DTC_COUNT))
				{
					e.Handled = Adapter.LocalHost.Transmit29((byte)128, 49, deviceAtProductAddress, CAN.PAYLOAD.FromArgs((byte)0));
					if (e.Handled)
					{
						RetryTimer.Reset();
					}
				}
			}
			else if (ReadingList && DTCs.Length != 0)
			{
				e.Handled = Adapter.LocalHost.Transmit29((byte)128, 48, deviceAtProductAddress, CAN.PAYLOAD.FromArgs(DtcIndex));
				if (e.Handled)
				{
					RetryTimer.Reset();
				}
			}
		}
	}
}
