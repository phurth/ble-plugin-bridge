using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using IDS.Core.Events;
using IDS.Core.Tasks;

namespace IDS.Core.IDS_CAN
{
	internal class DeviceBLOCK : Disposable, IDeviceBLOCK, IEventSender
	{
		public enum BLOCK_STATE : byte
		{
			BLOCK_IDLE,
			BLOCK_READ_PROPERTIES,
			BLOCK_START_TO_READ_DATA,
			BLOCK_WAIT_END_TRANSFER
		}

		private class AsyncSignal
		{
			public bool OperationComplete;
		}

		public byte[] Data;

		public BLOCKPropertyValue[] PropertyValues = new BLOCKPropertyValue[8];

		public int NbData;

		public bool IsReadDataReady;

		private bool ValueValid;

		private static readonly object Lock = new object();

		private ConcurrentQueue<AsyncSignal> ReadSignals;

		public IEventPublisher Events { get; private set; }

		public IRemoteDevice Device { get; private set; }

		public BLOCK_ID ID { get; private set; }

		public uint blockoffset { get; set; }

		public ushort actualbulktransfersize { get; set; }

		public ushort EndBulkXferOffset { get; set; }

		public uint EndBulkXferCRC32 { get; set; }

		public byte State { get; private set; }

		public byte Response { get; set; }

		public string Name => ID.Name;

		public bool IsValueValid
		{
			get
			{
				ValueValid &= Device.IsOnline;
				return ValueValid;
			}
		}

		public string ValueString
		{
			get
			{
				if (!IsValueValid)
				{
					return "UNKNOWN";
				}
				return ID.Name;
			}
		}

		public byte GetData(int nb)
		{
			return Data[nb];
		}

		public void Setblockoffset(uint Param)
		{
			blockoffset = Param;
		}

		public void Setactualbulktransfersize(ushort Param)
		{
			actualbulktransfersize = Param;
		}

		public void SetEndBulkXferOffset(ushort Param)
		{
			EndBulkXferOffset = Param;
		}

		public void SetEndBulkXferCRC32(uint Param)
		{
			EndBulkXferCRC32 = Param;
		}

		public void SetState(byte Param)
		{
			State = Param;
		}

		public void SetResponse(byte Param)
		{
			Response = Param;
		}

		public override string ToString()
		{
			return Name;
		}

		public DeviceBLOCK(IRemoteDevice device, BLOCK_ID id, bool Init)
		{
			Device = device;
			ID = id;
			if (Init)
			{
				Events = null;
			}
			else
			{
				Events = new EventPublisher("IDS.Core.IDS_CAN.BLOCK.Events");
			}
		}

		public void OnMessageEndBulkXferRx(ushort BlockOffset, uint Crc32)
		{
			IsReadDataReady = true;
			SetEndBulkXferOffset(BlockOffset);
			SetEndBulkXferCRC32(Crc32);
		}

		public void ResetNbData()
		{
			NbData = 0;
			IsReadDataReady = false;
		}

		public BLOCKPropertyValue GetPropertyValue(byte Property)
		{
			return PropertyValues[Property];
		}

		public void SetPropertyValue(byte Property, BLOCKPropertyValue Param)
		{
			PropertyValues[Property] = Param;
		}

		public static implicit operator BLOCK_ID(DeviceBLOCK value)
		{
			return value.ID;
		}

		public bool RequestRead()
		{
			if (base.IsDisposed)
			{
				return false;
			}
			if (!Device.Adapter.LocalHost.IsOnline)
			{
				return false;
			}
			if (!Device.IsOnline)
			{
				return false;
			}
			return Device.Adapter.LocalHost.Transmit29((byte)128, 34, Device, CAN.PAYLOAD.FromArgs((ushort)ID));
		}

		public bool RequestRead(byte Property)
		{
			if (base.IsDisposed)
			{
				return false;
			}
			if (!Device.Adapter.LocalHost.IsOnline)
			{
				return false;
			}
			if (!Device.IsOnline)
			{
				return false;
			}
			return Device.Adapter.LocalHost.Transmit29((byte)128, 33, Device, CAN.PAYLOAD.FromArgs((ushort)ID, Property));
		}

		public bool StartReadData(uint Offset, byte Size_Msg, byte DelayMs)
		{
			if (base.IsDisposed)
			{
				return false;
			}
			if (!Device.Adapter.LocalHost.IsOnline)
			{
				return false;
			}
			if (!Device.IsOnline)
			{
				return false;
			}
			return Device.Adapter.LocalHost.Transmit29((byte)128, 34, Device, CAN.PAYLOAD.FromArgs((ushort)ID, Offset, Size_Msg, DelayMs));
		}

		public void SetReadWriteBuffer(ulong capacity)
		{
			Data = new byte[capacity];
		}

		public void UpdateDataBuffer(CAN.IMessage message)
		{
			if (!base.IsDisposed && Data != null)
			{
				for (int i = 0; i < message.Length; i++)
				{
					Data[NbData++] = message[i];
				}
			}
		}

		public void OnMessagePropertyRx(CAN.IMessage message)
		{
			if (base.IsDisposed)
			{
				return;
			}
			byte b = message[2];
			ulong num = 0uL;
			for (int i = 3; i < message.Length; i++)
			{
				num <<= 8;
				num += message[i];
			}
			PropertyValues[b].IsValueValid = true;
			PropertyValues[b].PropertyValue = num;
			if (ReadSignals != null)
			{
				AsyncSignal asyncSignal;
				while (ReadSignals.TryDequeue(out asyncSignal))
				{
					asyncSignal.OperationComplete = true;
				}
			}
		}

		public void OnMessageStartReadDataRx(uint blockoffset, ushort bulktransfersize)
		{
			if (base.IsDisposed)
			{
				return;
			}
			Setblockoffset(blockoffset);
			Setactualbulktransfersize(bulktransfersize);
			ValueValid = true;
			if (ReadSignals != null)
			{
				AsyncSignal asyncSignal;
				while (ReadSignals.TryDequeue(out asyncSignal))
				{
					asyncSignal.OperationComplete = true;
				}
			}
		}

		public async Task<BLOCKPropertyValue> ReadPropertyAsync(byte Property, AsyncOperation operation)
		{
			operation.ReportProgress(0f, "Reading...");
			BLOCKPropertyValue TmpReturVal = new BLOCKPropertyValue(0uL, Isvaluevalid: false);
			SetPropertyValue(Property, TmpReturVal);
			AsyncSignal signal = new AsyncSignal();
			if (ReadSignals == null)
			{
				lock (Lock)
				{
					if (ReadSignals == null)
					{
						ReadSignals = new ConcurrentQueue<AsyncSignal>();
					}
				}
			}
			ReadSignals.Enqueue(signal);
			Timer tx_timer = new Timer();
			Timer progress_timer = new Timer();
			tx_timer.ElapsedTime = TimeSpan.FromSeconds(1.0);
			progress_timer.ElapsedTime = TimeSpan.FromSeconds(1.0);
			bool skip = true;
			TimeSpan ReadRequestTime = TimeSpan.FromMilliseconds(330.0);
			TimeSpan ReportProgressTime = TimeSpan.FromMilliseconds(250.0);
			while (!signal.OperationComplete)
			{
				operation.ThrowIfCancellationRequested();
				if (!skip)
				{
					await Task.Delay(33).ConfigureAwait(false);
				}
				else
				{
					skip = false;
				}
				if (base.IsDisposed)
				{
					operation.ReportProgress("ReadAsync failed: BLOCK is disposed");
					return TmpReturVal;
				}
				if (!Device.IsOnline)
				{
					operation.ReportProgress("ReadAsync failed: Device is offline");
					return TmpReturVal;
				}
				if (tx_timer.ElapsedTime >= ReadRequestTime && RequestRead(Property))
				{
					tx_timer.Reset();
				}
				if (progress_timer.ElapsedTime > ReportProgressTime)
				{
					progress_timer.Reset();
					operation.ReportProgress(0f, "Reading...");
				}
			}
			operation.ReportProgress(100f, "Success!");
			return GetPropertyValue(Property);
		}

		public async Task<bool> StartReadData(uint Offset, byte Size_Msg, byte DelayMs, AsyncOperation operation)
		{
			operation.ReportProgress(0f, "Reading...");
			AsyncSignal signal = new AsyncSignal();
			if (ReadSignals == null)
			{
				lock (Lock)
				{
					if (ReadSignals == null)
					{
						ReadSignals = new ConcurrentQueue<AsyncSignal>();
					}
				}
			}
			ReadSignals.Enqueue(signal);
			Timer tx_timer = new Timer();
			Timer progress_timer = new Timer();
			tx_timer.ElapsedTime = TimeSpan.FromSeconds(1.0);
			progress_timer.ElapsedTime = TimeSpan.FromSeconds(1.0);
			bool skip = true;
			TimeSpan ReadRequestTime = TimeSpan.FromMilliseconds(330.0);
			TimeSpan ReportProgressTime = TimeSpan.FromMilliseconds(250.0);
			while (!signal.OperationComplete)
			{
				operation.ThrowIfCancellationRequested();
				if (!skip)
				{
					await Task.Delay(33).ConfigureAwait(false);
				}
				else
				{
					skip = false;
				}
				if (base.IsDisposed)
				{
					operation.ReportProgress("ReadAsync failed: BLOCK is disposed");
					return false;
				}
				if (!Device.IsOnline)
				{
					operation.ReportProgress("ReadAsync failed: Device is offline");
					return false;
				}
				if (tx_timer.ElapsedTime >= ReadRequestTime && StartReadData(Offset, Size_Msg, DelayMs))
				{
					tx_timer.Reset();
				}
				if (progress_timer.ElapsedTime > ReportProgressTime)
				{
					progress_timer.Reset();
					operation.ReportProgress(0f, "Reading...");
				}
			}
			operation.ReportProgress(100f, "Success!");
			return true;
		}

		public async Task<bool> ReadDataBufferReadyAsync(AsyncOperation operation)
		{
			operation.ReportProgress(0f, "Reading...");
			AsyncSignal signal = new AsyncSignal();
			if (ReadSignals == null)
			{
				lock (Lock)
				{
					if (ReadSignals == null)
					{
						ReadSignals = new ConcurrentQueue<AsyncSignal>();
					}
				}
			}
			ReadSignals.Enqueue(signal);
			Timer tx_timer = new Timer();
			Timer progress_timer = new Timer();
			tx_timer.ElapsedTime = TimeSpan.FromSeconds(1.0);
			progress_timer.ElapsedTime = TimeSpan.FromSeconds(1.0);
			bool skip = true;
			TimeSpan ReadRequestTime = TimeSpan.FromMilliseconds(330.0);
			TimeSpan ReportProgressTime = TimeSpan.FromMilliseconds(250.0);
			while (!signal.OperationComplete)
			{
				operation.ThrowIfCancellationRequested();
				if (!skip)
				{
					await Task.Delay(33).ConfigureAwait(false);
				}
				else
				{
					skip = false;
				}
				if (base.IsDisposed)
				{
					operation.ReportProgress("ReadAsync failed: BLOCK is disposed");
					return false;
				}
				if (!Device.IsOnline)
				{
					operation.ReportProgress("ReadAsync failed: Device is offline");
					return false;
				}
				if (tx_timer.ElapsedTime >= ReadRequestTime && IsReadDataReady)
				{
					signal.OperationComplete = true;
					tx_timer.Reset();
				}
				if (progress_timer.ElapsedTime > ReportProgressTime)
				{
					progress_timer.Reset();
					operation.ReportProgress(0f, "Reading...");
				}
			}
			operation.ReportProgress(100f, "Success!");
			return true;
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Events?.Dispose();
				ReadSignals = null;
			}
		}
	}
}
