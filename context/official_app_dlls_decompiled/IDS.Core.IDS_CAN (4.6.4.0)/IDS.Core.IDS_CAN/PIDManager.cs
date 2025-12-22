using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using IDS.Core.Events;
using IDS.Core.Tasks;

namespace IDS.Core.IDS_CAN
{
	internal class PIDManager : RemoteDevice.Child, IPIDManager, IEnumerable<IDevicePID>, IEnumerable
	{
		private static readonly List<DevicePID> EmptyList = new List<DevicePID>();

		private static readonly TimeSpan READ_LIST_TX_RETRY_TIME = TimeSpan.FromMilliseconds(500.0);

		private readonly Dictionary<PID, DevicePID> Dictionary = new Dictionary<PID, DevicePID>();

		private readonly Dictionary<PID, DevicePID> HiddenPIDs = new Dictionary<PID, DevicePID>();

		private readonly List<DevicePID> SortedList = new List<DevicePID>();

		private readonly Timer TxTimer = new Timer();

		private bool NeedsRead = true;

		private bool ReadListFromDevice;

		private ushort PidIndex;

		private ushort ReportedCount;

		public int Count
		{
			get
			{
				if (NeedsRead)
				{
					return 0;
				}
				return SortedList.Count;
			}
		}

		public bool DeviceQueryComplete => !NeedsRead;

		private DevicePID this[PID id]
		{
			get
			{
				Dictionary.TryGetValue(id, out var value);
				return value;
			}
		}

		public PIDManager(RemoteDevice device)
			: base(device)
		{
			Clear();
			base.Adapter.Events.Subscribe<Comm.TransmitTurnEvent>(OnTransmitNextMessage, SubscriptionType.Weak, Subscriptions);
			ReadListFromDevice = base.Adapter.Options.HasFlag(ADAPTER_OPTIONS.AUTO_READ_DEVICE_PID_LIST);
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Clear();
			}
		}

		public IEnumerator<IDevicePID> GetEnumerator()
		{
			if (NeedsRead)
			{
				return EmptyList.GetEnumerator();
			}
			return SortedList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public async Task<PIDValue> ReadAsync(PID id, AsyncOperation operation)
		{
			if (base.Device != null && base.Device.IsOnline)
			{
				DevicePID pid = this[id];
				if (pid == null && !HiddenPIDs.TryGetValue(id, out pid))
				{
					pid = new DevicePID(base.Device, id);
					HiddenPIDs.Add(id, pid);
				}
				if (await pid.ReadAsync(operation))
				{
					return new PIDValue(base.Device, id, pid.Value, 0, 0u);
				}
			}
			return new PIDValue(base.Device, id);
		}

		public async Task<PIDValue> ReadAsync(PID id, ushort address, AsyncOperation operation)
		{
			if (base.Device != null && base.Device.IsOnline)
			{
				DevicePID pid = this[id];
				if (pid == null && !HiddenPIDs.TryGetValue(id, out pid))
				{
					pid = new DevicePID(base.Device, id);
					HiddenPIDs.Add(id, pid);
				}
				if (await pid.ReadAsync(address, operation))
				{
					return new PIDValue(base.Device, id, pid.Value, pid.Address, pid.Data);
				}
			}
			return new PIDValue(base.Device, id);
		}

		public void QueryDevice()
		{
			if (NeedsRead)
			{
				ReadListFromDevice = true;
			}
		}

		public bool Contains(PID id)
		{
			return this[id] != null;
		}

		public IDevicePID GetPID(PID id)
		{
			return this[id];
		}

		public bool IsPIDSupported(PID id)
		{
			return Dictionary.ContainsKey(id);
		}

		private void Clear()
		{
			TxTimer.ElapsedTime = TimeSpan.FromSeconds(-0.25);
			PidIndex = 0;
			SortedList.Clear();
			foreach (DevicePID value in Dictionary.Values)
			{
				value.Dispose();
			}
			Dictionary.Clear();
			foreach (DevicePID value2 in HiddenPIDs.Values)
			{
				value2.Dispose();
			}
			HiddenPIDs.Clear();
		}

		public override void BackgroundTask()
		{
		}

		public override void OnDeviceTx(AdapterRxEvent tx)
		{
			if (!base.Device.IsOnline || (byte)tx.MessageType != 129 || tx.TargetAddress != base.Adapter.LocalHost.Address)
			{
				return;
			}
			switch (tx.MessageData)
			{
			case 17:
				if (tx.Count >= 2)
				{
					ushort uINT = tx.GetUINT16(0);
					this[uINT]?.OnMessageRx(tx);
					if (HiddenPIDs.TryGetValue(uINT, out var value))
					{
						value.OnMessageRx(tx);
					}
				}
				break;
			case 16:
				if (!ReadListFromDevice)
				{
					break;
				}
				if (tx.Count == 1)
				{
					NeedsRead = (ReadListFromDevice = false);
					ReportedCount = 0;
					SortedList.Clear();
					Dictionary.Clear();
					_ = tx[0];
				}
				else
				{
					if (tx.Count != 8 || tx.GetUINT16(0) != PidIndex)
					{
						break;
					}
					int num = 2;
					int i = PidIndex * 2;
					if (PidIndex == 0)
					{
						ReportedCount = tx.GetUINT16(num);
						SortedList.Clear();
						Dictionary.Clear();
						num += 3;
					}
					else
					{
						i--;
					}
					for (; i < ReportedCount; i++)
					{
						if (num >= 8)
						{
							break;
						}
						PID pID = tx.GetUINT16(num);
						if (pID != PID.UNKNOWN)
						{
							if (Dictionary.ContainsKey(pID))
							{
								Clear();
								return;
							}
							DevicePID devicePID = new DevicePID(base.Device, pID, tx[num + 2]);
							SortedList.Add(devicePID);
							Dictionary.Add(pID, devicePID);
						}
						num += 3;
					}
					if (i >= ReportedCount)
					{
						SortedList.Sort((DevicePID first, DevicePID second) => first.ID.Value.CompareTo(second.ID.Value));
						NeedsRead = (ReadListFromDevice = false);
					}
					PidIndex++;
					TxTimer.ElapsedTime = TimeSpan.FromSeconds(1.0);
				}
				break;
			}
		}

		private void OnTransmitNextMessage(Comm.TransmitTurnEvent message)
		{
			if (ReadListFromDevice && base.Adapter.LocalHost.IsOnline && base.Device.IsOnline && !(TxTimer.ElapsedTime < READ_LIST_TX_RETRY_TIME))
			{
				message.Handled = base.Adapter.LocalHost.Transmit29((byte)128, 16, base.Device, CAN.PAYLOAD.FromArgs(PidIndex));
				if (message.Handled)
				{
					TxTimer.Reset();
				}
			}
		}
	}
}
