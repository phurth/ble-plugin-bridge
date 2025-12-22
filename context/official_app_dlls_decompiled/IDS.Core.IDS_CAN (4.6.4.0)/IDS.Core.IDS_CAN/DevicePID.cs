using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using IDS.Core.Events;
using IDS.Core.Tasks;

namespace IDS.Core.IDS_CAN
{
	internal class DevicePID : Disposable, IDevicePID, IEventSender
	{
		private class AsyncSignal
		{
			public bool OperationComplete;
		}

		private enum PID_WRITE_STATE
		{
			OPEN_SESSION,
			WRITE,
			VERIFY
		}

		private readonly PIDUpdatedEvent PIDUpdatedEvent;

		private readonly PIDValueChangedEvent PIDValueChangedEvent;

		private ulong LastReadValue;

		private bool ValueValid;

		private static readonly object Lock = new object();

		private ConcurrentQueue<AsyncSignal> ReadSignals;

		public IEventPublisher Events { get; private set; }

		public IRemoteDevice Device { get; private set; }

		public PID ID { get; private set; }

		public byte Flags { get; private set; }

		public string Name => ID.Name;

		public bool IsReadable => (Flags & 1) != 0;

		public bool IsWritable => (Flags & 2) != 0;

		public bool IsNonVolatile => (Flags & 4) != 0;

		public bool IsWithAddress => (Flags & 8) != 0;

		public ulong Value
		{
			get
			{
				if (!IsValueValid)
				{
					return 0uL;
				}
				return LastReadValue;
			}
		}

		public uint Data
		{
			get
			{
				if (!IsValueValid)
				{
					return 0u;
				}
				return (uint)LastReadValue;
			}
		}

		public ushort Address
		{
			get
			{
				if (!IsValueValid)
				{
					return 0;
				}
				return (ushort)(LastReadValue >> 32);
			}
		}

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
				return ID.FormatValue(Value);
			}
		}

		public override string ToString()
		{
			return Name;
		}

		public DevicePID(IRemoteDevice device, PID id, byte flags)
		{
			Device = device;
			ID = id;
			Flags = flags;
			Events = new EventPublisher("IDS.Core.IDS_CAN.PID.Events");
			PIDUpdatedEvent = new PIDUpdatedEvent(this);
			PIDValueChangedEvent = new PIDValueChangedEvent(this);
		}

		public DevicePID(IRemoteDevice device, PID id)
		{
			Device = device;
			ID = id;
			Flags = byte.MaxValue;
			Events = null;
			PIDUpdatedEvent = null;
			PIDValueChangedEvent = null;
		}

		public static implicit operator PID(DevicePID value)
		{
			return value.ID;
		}

		public bool RequestRead()
		{
			if (base.IsDisposed)
			{
				return false;
			}
			if (!IsReadable)
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
			return Device.Adapter.LocalHost.Transmit29((byte)128, 17, Device, CAN.PAYLOAD.FromArgs((ushort)ID));
		}

		public bool RequestRead(ushort Address)
		{
			if (base.IsDisposed)
			{
				return false;
			}
			if (!IsReadable)
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
			return Device.Adapter.LocalHost.Transmit29((byte)128, 17, Device, CAN.PAYLOAD.FromArgs((ushort)ID, Address));
		}

		private bool RequestWrite(long value)
		{
			if (base.IsDisposed)
			{
				return false;
			}
			if (!IsWritable)
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
			return Device.Adapter.LocalHost.Transmit29((byte)128, 17, Device, CAN.PAYLOAD.FromArgs((ushort)ID, (ushort)(value >> 32), (uint)value));
		}

		private bool RequestWrite(ushort address, uint data)
		{
			if (base.IsDisposed)
			{
				return false;
			}
			if (!IsWritable)
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
			return Device.Adapter.LocalHost.Transmit29((byte)128, 17, Device, CAN.PAYLOAD.FromArgs((ushort)ID, address, data));
		}

		public void OnMessageRx(CAN.IMessage message)
		{
			if (base.IsDisposed)
			{
				return;
			}
			ulong num = 0uL;
			for (int i = 2; i < message.Length; i++)
			{
				num <<= 8;
				num += message[i];
			}
			ulong lastReadValue = LastReadValue;
			LastReadValue = num;
			bool isValueValid = IsValueValid;
			ValueValid = true;
			if (PIDUpdatedEvent != null)
			{
				PIDUpdatedEvent.Publish(num);
			}
			if (PIDValueChangedEvent != null && (lastReadValue != LastReadValue || !isValueValid))
			{
				PIDValueChangedEvent.Publish(num);
			}
			if (ReadSignals != null)
			{
				AsyncSignal asyncSignal;
				while (ReadSignals.TryDequeue(out asyncSignal))
				{
					asyncSignal.OperationComplete = true;
				}
			}
		}

		public async Task<bool> ReadAsync(AsyncOperation operation)
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
					operation.ReportProgress("ReadAsync failed: PID is disposed");
					return false;
				}
				if (!IsReadable)
				{
					operation.ReportProgress("ReadAsync failed: PID is not readable");
					return false;
				}
				if (!Device.IsOnline)
				{
					operation.ReportProgress("ReadAsync failed: Device is offline");
					return false;
				}
				if (tx_timer.ElapsedTime >= ReadRequestTime && RequestRead())
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

		public async Task<bool> ReadAsync(ushort address, AsyncOperation operation)
		{
			if (!IsWithAddress)
			{
				return false;
			}
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
					operation.ReportProgress("ReadAsync failed: PID is disposed");
					return false;
				}
				if (!IsReadable)
				{
					operation.ReportProgress("ReadAsync failed: PID is not readable");
					return false;
				}
				if (!Device.IsOnline)
				{
					operation.ReportProgress("ReadAsync failed: Device is offline");
					return false;
				}
				if (tx_timer.ElapsedTime >= ReadRequestTime && RequestRead(address))
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

		public async Task<bool> WriteAsync(ulong value, ISessionClient session, AsyncOperation operation)
		{
			PID_WRITE_STATE state = PID_WRITE_STATE.OPEN_SESSION;
			if (session != null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Opening ");
				defaultInterpolatedStringHandler.AppendFormatted(session.SessionID);
				defaultInterpolatedStringHandler.AppendLiteral(" session...");
				operation.ReportProgress(0f, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			Timer StateTime = new Timer();
			Timer tx_timer = new Timer();
			Timer progress_timer = new Timer();
			tx_timer.ElapsedTime = TimeSpan.FromSeconds(1.0);
			bool skip = true;
			double basepercent = 0.0;
			TimeSpan retryTime = TimeSpan.FromMilliseconds(250.0);
			while (true)
			{
				operation.ThrowIfCancellationRequested();
				if (!skip)
				{
					await Task.Delay(10).ConfigureAwait(false);
				}
				else
				{
					skip = false;
				}
				if (base.IsDisposed)
				{
					operation.ReportProgress("WritePIDAsync failed: object is disposed");
					return false;
				}
				if (!Device.IsOnline)
				{
					operation.ReportProgress("Failed: Device went offline");
					return false;
				}
				if (state == PID_WRITE_STATE.VERIFY && IsValueValid && Value == value)
				{
					break;
				}
				float num = (float)(basepercent + (100.0 - basepercent) * (double)StateTime.ElapsedTime.Ticks / (double)operation.ElapsedTime.Ticks);
				if (progress_timer.ElapsedTime > retryTime)
				{
					operation.ReportProgress(num, operation.Status);
					progress_timer.Reset();
				}
				if (session != null)
				{
					session.TryOpenSession();
					if (!session.IsOpen)
					{
						continue;
					}
				}
				if (state == PID_WRITE_STATE.OPEN_SESSION)
				{
					state = PID_WRITE_STATE.WRITE;
					StateTime.Reset();
					basepercent = num;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Writing PID ");
					defaultInterpolatedStringHandler.AppendFormatted(ID);
					defaultInterpolatedStringHandler.AppendLiteral(" = ");
					defaultInterpolatedStringHandler.AppendFormatted(value);
					defaultInterpolatedStringHandler.AppendLiteral("...");
					operation.ReportProgress(num, defaultInterpolatedStringHandler.ToStringAndClear());
					progress_timer.Reset();
				}
				if (tx_timer.ElapsedTime >= retryTime && RequestWrite((long)value))
				{
					tx_timer.Reset();
					if (state == PID_WRITE_STATE.WRITE)
					{
						state = PID_WRITE_STATE.VERIFY;
						StateTime.Reset();
					}
				}
			}
			operation.ReportProgress(100f, "Success!");
			return true;
		}

		public async Task<bool> WriteAsync(ushort address, uint data, ISessionClient session, AsyncOperation operation)
		{
			if (!IsWithAddress)
			{
				return false;
			}
			PID_WRITE_STATE state = PID_WRITE_STATE.OPEN_SESSION;
			if (session != null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Opening ");
				defaultInterpolatedStringHandler.AppendFormatted(session.SessionID);
				defaultInterpolatedStringHandler.AppendLiteral(" session...");
				operation.ReportProgress(0f, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			Timer StateTime = new Timer();
			Timer tx_timer = new Timer();
			Timer progress_timer = new Timer();
			tx_timer.ElapsedTime = TimeSpan.FromSeconds(1.0);
			bool skip = true;
			double basepercent = 0.0;
			TimeSpan retryTime = TimeSpan.FromMilliseconds(250.0);
			while (true)
			{
				operation.ThrowIfCancellationRequested();
				if (!skip)
				{
					await Task.Delay(10).ConfigureAwait(false);
				}
				else
				{
					skip = false;
				}
				if (base.IsDisposed)
				{
					operation.ReportProgress("WritePIDAsync failed: object is disposed");
					return false;
				}
				if (!Device.IsOnline)
				{
					operation.ReportProgress("Failed: Device went offline");
					return false;
				}
				if (state == PID_WRITE_STATE.VERIFY && IsValueValid)
				{
					break;
				}
				float num = (float)(basepercent + (100.0 - basepercent) * (double)StateTime.ElapsedTime.Ticks / (double)operation.ElapsedTime.Ticks);
				if (progress_timer.ElapsedTime > retryTime)
				{
					operation.ReportProgress(num, operation.Status);
					progress_timer.Reset();
				}
				if (session != null)
				{
					session.TryOpenSession();
					if (!session.IsOpen)
					{
						continue;
					}
				}
				if (state == PID_WRITE_STATE.OPEN_SESSION)
				{
					state = PID_WRITE_STATE.WRITE;
					StateTime.Reset();
					basepercent = num;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Writing PID ");
					defaultInterpolatedStringHandler.AppendFormatted(ID);
					defaultInterpolatedStringHandler.AppendLiteral(" = ");
					defaultInterpolatedStringHandler.AppendFormatted(address);
					defaultInterpolatedStringHandler.AppendLiteral("...");
					operation.ReportProgress(num, defaultInterpolatedStringHandler.ToStringAndClear());
					progress_timer.Reset();
				}
				if (tx_timer.ElapsedTime >= retryTime && RequestWrite(address, data))
				{
					tx_timer.Reset();
					if (state == PID_WRITE_STATE.WRITE)
					{
						state = PID_WRITE_STATE.VERIFY;
						StateTime.Reset();
					}
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
