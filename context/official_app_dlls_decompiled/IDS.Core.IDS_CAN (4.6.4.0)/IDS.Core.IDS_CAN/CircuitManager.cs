using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	internal class CircuitManager : Adapter.BackgroundTaskObject, ICircuitManager, IEnumerable<ICircuit>, IEnumerable
	{
		private readonly ConcurrentDictionary<CIRCUIT_ID, Circuit> Circuits = new ConcurrentDictionary<CIRCUIT_ID, Circuit>();

		private readonly CircuitListChangedEvent CircuitListChangedEvent;

		public int Count => Circuits.Count;

		public Circuit this[CIRCUIT_ID id]
		{
			get
			{
				if (base.IsDisposed)
				{
					return null;
				}
				Circuits.TryGetValue(id, out var result);
				return result;
			}
		}

		public int TotalDevices
		{
			get
			{
				int num = 0;
				using IEnumerator<ICircuit> enumerator = GetEnumerator();
				while (enumerator.MoveNext())
				{
					Circuit circuit = (Circuit)enumerator.Current;
					num += circuit.DeviceCount;
				}
				return num;
			}
		}

		public CircuitManager(Adapter adapter)
			: base(adapter)
		{
			CircuitListChangedEvent = new CircuitListChangedEvent(this);
			base.Adapter.Events.Subscribe<Comm.AdapterOpenedEvent>(OnAdapterOpened, SubscriptionType.Strong, base.Subscriptions);
			base.Adapter.Events.Subscribe<Comm.AdapterClosedEvent>(OnAdapterClosed, SubscriptionType.Strong, base.Subscriptions);
			base.Adapter.Events.Subscribe<RemoteDeviceOnlineEvent>(OnRemoteDeviceOnline, SubscriptionType.Strong, base.Subscriptions);
			base.Adapter.Events.Subscribe<RemoteDeviceOfflineEvent>(OnRemoteDeviceOffline, SubscriptionType.Strong, base.Subscriptions);
			base.Adapter.Events.Subscribe<CircuitIDChangedEvent>(OnCircuitIDChanged, SubscriptionType.Strong, base.Subscriptions);
		}

		private void PublishChangeEvent()
		{
			Task.Run(delegate
			{
				CircuitListChangedEvent.Publish();
			});
		}

		public IEnumerator<ICircuit> GetEnumerator()
		{
			return Circuits.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private void OnAdapterOpened(Comm.AdapterOpenedEvent message)
		{
			Clear();
		}

		private void OnAdapterClosed(Comm.AdapterClosedEvent message)
		{
			Clear();
		}

		private void OnRemoteDeviceOnline(RemoteDeviceOnlineEvent message)
		{
			if (LocateOrCreateCircuit(message.Device.CircuitID).AddDeviceToCircuit(message.Device))
			{
				PublishChangeEvent();
			}
		}

		private void OnRemoteDeviceOffline(RemoteDeviceOfflineEvent message)
		{
			Circuit circuit = this[message.Device.CircuitID];
			if (circuit != null && circuit.Cleanup())
			{
				PublishChangeEvent();
			}
		}

		private void OnCircuitIDChanged(CircuitIDChangedEvent message)
		{
			bool? obj = this[message.Prev]?.Cleanup();
			if ((LocateOrCreateCircuit(message.Device.CircuitID).AddDeviceToCircuit(message.Device) | obj) == true)
			{
				PublishChangeEvent();
			}
		}

		private Circuit LocateOrCreateCircuit(CIRCUIT_ID id)
		{
			if (base.IsDisposed)
			{
				return null;
			}
			Circuit circuit = this[id];
			if (circuit != null)
			{
				return circuit;
			}
			return Circuits.GetOrAdd(id, (CIRCUIT_ID k) => new Circuit(id));
		}

		public bool ContainsCircuit(CIRCUIT_ID id)
		{
			return Circuits.ContainsKey(id);
		}

		public void Clear()
		{
			int count = Circuits.Count;
			if (count == 0)
			{
				return;
			}
			Circuits.Clear();
			if (!base.IsDisposed)
			{
				LocateOrCreateCircuit(0u);
				if (count != 1)
				{
					PublishChangeEvent();
				}
			}
		}

		public override void BackgroundTask()
		{
			if (base.IsDisposed)
			{
				return;
			}
			if (!base.Adapter.IsConnected)
			{
				Clear();
				return;
			}
			bool flag = false;
			foreach (Circuit value in Circuits.Values)
			{
				flag |= value.Cleanup();
			}
			foreach (IRemoteDevice device in base.Adapter.Devices)
			{
				flag |= LocateOrCreateCircuit(device.CircuitID).AddDeviceToCircuit(device);
			}
			foreach (Circuit value2 in Circuits.Values)
			{
				if (value2.IsEmpty && (uint)value2.CircuitID != 0)
				{
					Circuits.TryRemove(value2.CircuitID, out var _);
				}
			}
			if (flag)
			{
				PublishChangeEvent();
			}
		}

		public bool DoesCircuitExist(CIRCUIT_ID id)
		{
			return this[id] != null;
		}

		public CIRCUIT_ID GetRandomUnusedCircuitID()
		{
			CIRCUIT_ID cIRCUIT_ID;
			do
			{
				cIRCUIT_ID = (uint)(4294967295.0 * ThreadLocalRandom.NextDouble() + 1.0);
			}
			while ((uint)cIRCUIT_ID == 0 || DoesCircuitExist(cIRCUIT_ID));
			return cIRCUIT_ID;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Network contains ");
			defaultInterpolatedStringHandler.AppendFormatted(TotalDevices);
			defaultInterpolatedStringHandler.AppendLiteral(" devices in ");
			defaultInterpolatedStringHandler.AppendFormatted(Count);
			defaultInterpolatedStringHandler.AppendLiteral(" circuits");
			string text = defaultInterpolatedStringHandler.ToStringAndClear();
			using IEnumerator<ICircuit> enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				Circuit circuit = (Circuit)enumerator.Current;
				string text2 = text;
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 1);
				defaultInterpolatedStringHandler.AppendLiteral("\n\tCircuit ID ");
				defaultInterpolatedStringHandler.AppendFormatted(circuit.CircuitID);
				defaultInterpolatedStringHandler.AppendLiteral(" >> ");
				text = text2 + defaultInterpolatedStringHandler.ToStringAndClear();
				int num = 0;
				foreach (IRemoteDevice item in circuit)
				{
					if (num++ > 0)
					{
						text += ", ";
					}
					text += item.ToShortString(show_address: false);
				}
			}
			return text;
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				base.Subscriptions.Dispose();
				Clear();
			}
		}
	}
}
