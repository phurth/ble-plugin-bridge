using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using IDS.Core.Events;
using IDS.Core.Tasks;

namespace IDS.Core.IDS_CAN
{
	internal class BLOCKManager : RemoteDevice.Child, IBLOCKManager, IEnumerable<IDeviceBLOCK>, IEnumerable
	{
		private static readonly List<DeviceBLOCK> EmptyList = new List<DeviceBLOCK>();

		private static readonly TimeSpan READ_LIST_TX_RETRY_TIME = TimeSpan.FromMilliseconds(500.0);

		private readonly Dictionary<BLOCK_ID, DeviceBLOCK> Dictionary = new Dictionary<BLOCK_ID, DeviceBLOCK>();

		private readonly Dictionary<BLOCK_ID, DeviceBLOCK> HiddenBLOCKs = new Dictionary<BLOCK_ID, DeviceBLOCK>();

		private readonly List<DeviceBLOCK> SortedList = new List<DeviceBLOCK>();

		private readonly Timer TxTimer = new Timer();

		private bool NeedsRead = true;

		private bool ReadListFromDevice;

		private ushort BlockIndex;

		private ushort ReportedCount;

		private BLOCK_ID BlockIdRef = BLOCK_ID.UNKNOWN;

		private byte[] property = new byte[8] { 0, 1, 2, 3, 4, 5, 6, 7 };

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

		private DeviceBLOCK this[BLOCK_ID id]
		{
			get
			{
				Dictionary.TryGetValue(id, out var value);
				return value;
			}
		}

		public BLOCKManager(RemoteDevice device)
			: base(device)
		{
			Clear();
			base.Adapter.Events.Subscribe<Comm.TransmitTurnEvent>(OnTransmitNextMessage, SubscriptionType.Weak, Subscriptions);
			ReadListFromDevice = base.Adapter.Options.HasFlag(ADAPTER_OPTIONS.AUTO_READ_DEVICE_BLOCK_LIST);
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Clear();
			}
		}

		public IEnumerator<IDeviceBLOCK> GetEnumerator()
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

		public async Task<BLOCKValue> ReadPropertyAsync(BLOCK_ID id, AsyncOperation operation)
		{
			byte[] EmptyBuf = new byte[0];
			if (base.Device != null && base.Device.IsOnline)
			{
				DeviceBLOCK block = this[id];
				if (this[id] != null)
				{
					block.SetState(1);
					BLOCKPropertyValue bLOCKPropertyValue = new BLOCKPropertyValue(0uL, Isvaluevalid: false);
					BLOCKValue TmpBLOCKValue = new BLOCKValue
					{
						Device = base.Device,
						ID = id,
						IsValueValid = false,
						EndBulkXferOffset = 0,
						EndBulkXferCRC32 = 0u
					};
					for (int TmpIndex = 0; TmpIndex < 7; TmpIndex++)
					{
						bLOCKPropertyValue.IsValueValid = false;
						bLOCKPropertyValue.PropertyValue = 0uL;
						bLOCKPropertyValue = await block.ReadPropertyAsync(property[TmpIndex], operation);
						TmpBLOCKValue.Response = block.Response;
						if (bLOCKPropertyValue.IsValueValid)
						{
							ulong propertyValue = bLOCKPropertyValue.PropertyValue;
							switch (TmpIndex)
							{
							case 0:
								TmpBLOCKValue.PropertyReadWrite = (byte)propertyValue;
								break;
							case 1:
								TmpBLOCKValue.PropertySessionRead = (ushort)propertyValue;
								break;
							case 2:
								TmpBLOCKValue.PropertySessionWrite = (ushort)propertyValue;
								break;
							case 3:
								TmpBLOCKValue.PropertyBlockCapacity = propertyValue;
								break;
							case 4:
								TmpBLOCKValue.PropertyCurrentBlockSize = propertyValue;
								break;
							case 5:
								TmpBLOCKValue.PropertyCRC32 = (uint)propertyValue;
								break;
							case 6:
								TmpBLOCKValue.PropertyCRC32Verify = (uint)propertyValue;
								TmpBLOCKValue.IsValueValid = true;
								TmpBLOCKValue.BlockData = EmptyBuf;
								block.SetState(0);
								return TmpBLOCKValue;
							}
						}
					}
					block.SetState(0);
				}
			}
			return new BLOCKValue(base.Device, id, Isvaluevalid: false, 0, 0, 0, 0uL, 0uL, 0u, 0u, 0uL, 0u, 0, 0, 0u, 0, EmptyBuf);
		}

		public async Task<BLOCKValue> StartReadData(BLOCK_ID id, uint Offset, byte Size_Msg, byte DelayMs, AsyncOperation operation)
		{
			byte[] blockdata = new byte[0];
			if (base.Device != null && base.Device.IsOnline && id != BLOCK_ID.UNKNOWN && Contains(id))
			{
				DeviceBLOCK block = this[id];
				block.SetState(2);
				bool isvaluevalid = false;
				if (block.PropertyValues[4].PropertyValue != 0L)
				{
					block.SetReadWriteBuffer(block.PropertyValues[4].PropertyValue);
					isvaluevalid = await block.StartReadData(Offset, Size_Msg, DelayMs, operation);
				}
				block.SetState(0);
				return new BLOCKValue(base.Device, id, isvaluevalid, (byte)block.PropertyValues[0].PropertyValue, (ushort)block.PropertyValues[1].PropertyValue, (ushort)block.PropertyValues[2].PropertyValue, block.PropertyValues[3].PropertyValue, block.PropertyValues[4].PropertyValue, (uint)block.PropertyValues[5].PropertyValue, (uint)block.PropertyValues[6].PropertyValue, (uint)block.PropertyValues[7].PropertyValue, block.blockoffset, block.actualbulktransfersize, block.EndBulkXferOffset, block.EndBulkXferCRC32, block.Response, block.Data);
			}
			return new BLOCKValue(base.Device, id, Isvaluevalid: false, 0, 0, 0, 0uL, 0uL, 0u, 0u, 0uL, 0u, 0, 0, 0u, 0, blockdata);
		}

		public async Task<BLOCKValue> ReadDataBufferReadyAsync(BLOCK_ID id, AsyncOperation operation)
		{
			byte[] blockdata = new byte[0];
			if (base.Device != null && base.Device.IsOnline && id != BLOCK_ID.UNKNOWN && Contains(id))
			{
				DeviceBLOCK block = this[id];
				block.SetState(3);
				bool flag = await block.ReadDataBufferReadyAsync(operation);
				if (flag)
				{
					Array.Resize(ref block.Data, block.NbData);
					uint num = 0u;
					if (block.Data.Length != 0)
					{
						num = CRC32_LE.Calculate((IReadOnlyCollection<byte>)(object)block.Data);
					}
					if (num != block.EndBulkXferCRC32)
					{
						block.SetResponse(18);
						flag = false;
					}
				}
				block.SetState(0);
				return new BLOCKValue(base.Device, id, flag, (byte)block.PropertyValues[0].PropertyValue, (ushort)block.PropertyValues[1].PropertyValue, (ushort)block.PropertyValues[2].PropertyValue, block.PropertyValues[3].PropertyValue, block.PropertyValues[4].PropertyValue, (uint)block.PropertyValues[5].PropertyValue, (uint)block.PropertyValues[6].PropertyValue, (uint)block.PropertyValues[7].PropertyValue, block.blockoffset, block.actualbulktransfersize, block.EndBulkXferOffset, block.EndBulkXferCRC32, block.Response, block.Data);
			}
			return new BLOCKValue(base.Device, id, Isvaluevalid: false, 0, 0, 0, 0uL, 0uL, 0u, 0u, 0uL, 0u, 0, 0, 0u, 0, blockdata);
		}

		public void QueryDevice()
		{
			if (NeedsRead)
			{
				ReadListFromDevice = true;
			}
		}

		public bool Contains(BLOCK_ID id)
		{
			return this[id] != null;
		}

		public IDeviceBLOCK GetBLOCK(BLOCK_ID id)
		{
			return this[id];
		}

		public bool IsBLOCKSupported(BLOCK_ID id)
		{
			return Dictionary.ContainsKey(id);
		}

		private void Clear()
		{
			TxTimer.ElapsedTime = TimeSpan.FromSeconds(-0.25);
			BlockIndex = 0;
			SortedList.Clear();
			foreach (DeviceBLOCK value in Dictionary.Values)
			{
				value.Dispose();
			}
			Dictionary.Clear();
			foreach (DeviceBLOCK value2 in HiddenBLOCKs.Values)
			{
				value2.Dispose();
			}
			HiddenBLOCKs.Clear();
		}

		public override void BackgroundTask()
		{
		}

		public override void OnDeviceTx(AdapterRxEvent tx)
		{
			if (!base.Device.IsOnline || tx.TargetAddress != base.Adapter.LocalHost.Address)
			{
				return;
			}
			switch ((byte)tx.MessageType)
			{
			case 159:
			{
				if (BlockIdRef == BLOCK_ID.UNKNOWN)
				{
					break;
				}
				DeviceBLOCK deviceBLOCK5 = this[BlockIdRef];
				if (deviceBLOCK5 != null)
				{
					_ = deviceBLOCK5.State;
					if (0 == 0 && this[BlockIdRef]?.State != 0)
					{
						_ = tx.MessageData;
						this[BlockIdRef]?.UpdateDataBuffer(tx);
					}
				}
				break;
			}
			case 129:
				switch (tx.MessageData)
				{
				case 32:
				{
					if (!ReadListFromDevice || tx.Count != 8 || tx.GetUINT16(0) != BlockIndex)
					{
						break;
					}
					int num = 2;
					int i = BlockIndex * 3;
					if (BlockIndex == 0)
					{
						ReportedCount = tx.GetUINT16(num);
						SortedList.Clear();
						Dictionary.Clear();
						num += 2;
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
						BLOCK_ID bLOCK_ID = tx.GetUINT16(num);
						if (bLOCK_ID != BLOCK_ID.UNKNOWN)
						{
							if (Dictionary.ContainsKey(bLOCK_ID))
							{
								Clear();
								return;
							}
							DeviceBLOCK deviceBLOCK3 = new DeviceBLOCK(base.Device, bLOCK_ID, Init: false);
							SortedList.Add(deviceBLOCK3);
							Dictionary.Add(bLOCK_ID, deviceBLOCK3);
						}
						num += 2;
					}
					if (i >= ReportedCount)
					{
						SortedList.Sort((DeviceBLOCK first, DeviceBLOCK second) => first.ID.Value.CompareTo(second.ID.Value));
						NeedsRead = (ReadListFromDevice = false);
					}
					BlockIndex++;
					TxTimer.ElapsedTime = TimeSpan.FromSeconds(1.0);
					break;
				}
				case 33:
				{
					ushort uINT7 = tx.GetUINT16(0);
					DeviceBLOCK deviceBLOCK4 = this[uINT7];
					if (deviceBLOCK4 == null)
					{
						break;
					}
					_ = deviceBLOCK4.State;
					if (0 == 0 && this[uINT7]?.State != 0)
					{
						if (tx.Count == 8)
						{
							this[uINT7]?.OnMessagePropertyRx(tx);
						}
						else
						{
							this[uINT7]?.SetResponse(tx.GetUINT8(3));
						}
					}
					break;
				}
				case 34:
				{
					ushort uINT4 = tx.GetUINT16(0);
					DeviceBLOCK deviceBLOCK2 = this[uINT4];
					if (deviceBLOCK2 == null)
					{
						break;
					}
					_ = deviceBLOCK2.State;
					if (0 == 0 && this[uINT4]?.State != 0)
					{
						if (tx.Count == 8)
						{
							uint uINT5 = tx.GetUINT32(2);
							ushort uINT6 = tx.GetUINT16(6);
							this[uINT4]?.OnMessageStartReadDataRx(uINT5, uINT6);
							this[uINT4]?.ResetNbData();
							BlockIdRef = uINT4;
						}
						else
						{
							this[uINT4]?.SetResponse(tx.GetUINT8(6));
						}
					}
					break;
				}
				case 37:
				{
					ushort uINT = tx.GetUINT16(0);
					DeviceBLOCK deviceBLOCK = this[uINT];
					if (deviceBLOCK == null)
					{
						break;
					}
					_ = deviceBLOCK.State;
					if (0 == 0 && this[uINT]?.State != 0)
					{
						if (tx.Count == 8)
						{
							ushort uINT2 = tx.GetUINT16(2);
							uint uINT3 = tx.GetUINT32(4);
							this[uINT]?.OnMessageEndBulkXferRx(uINT2, uINT3);
						}
						else
						{
							this[uINT]?.SetResponse(tx.GetUINT8(4));
						}
						BlockIdRef = BLOCK_ID.UNKNOWN;
					}
					break;
				}
				}
				break;
			}
		}

		private void OnTransmitNextMessage(Comm.TransmitTurnEvent message)
		{
			if (ReadListFromDevice && base.Adapter.LocalHost.IsOnline && base.Device.IsOnline && !(TxTimer.ElapsedTime < READ_LIST_TX_RETRY_TIME))
			{
				message.Handled = base.Adapter.LocalHost.Transmit29((byte)128, 32, base.Device, CAN.PAYLOAD.FromArgs(BlockIndex));
				if (message.Handled)
				{
					TxTimer.Reset();
				}
			}
		}
	}
}
