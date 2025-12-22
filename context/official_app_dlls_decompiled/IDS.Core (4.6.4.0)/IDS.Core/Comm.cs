using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.Events;
using IDS.Core.Tasks;
using IDS.Core.Types;

namespace IDS.Core
{
	public static class Comm
	{
		public interface IPhysicalAddress : IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>, IEquatable<PhysicalAddress>, IComparable, IComparable<PhysicalAddress>
		{
		}

		public class PhysicalAddress : IPhysicalAddress, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>, IEquatable<PhysicalAddress>, IComparable, IComparable<PhysicalAddress>
		{
			protected readonly byte[] Buffer;

			public int Count => Buffer.Length;

			public byte this[int index] => Buffer[index];

			public PhysicalAddress(int size)
			{
				Buffer = new byte[size];
			}

			public PhysicalAddress(byte[] buffer)
				: this(buffer.Length)
			{
				CopyFrom(buffer);
			}

			public PhysicalAddress(IReadOnlyList<byte> mac)
				: this(mac.Count)
			{
				CopyFrom(mac);
			}

			public PhysicalAddress(IPhysicalAddress mac)
				: this(mac.Count)
			{
				CopyFrom(mac);
			}

			public IEnumerator<byte> GetEnumerator()
			{
				return ((IEnumerable<byte>)Buffer).GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public void SetRandomMACValue()
			{
				ThreadLocalRandom.NextBytes(Buffer);
			}

			public override int GetHashCode()
			{
				int num = Buffer.Length;
				byte[] buffer = Buffer;
				foreach (byte b in buffer)
				{
					num = num * 31 + b;
				}
				return num;
			}

			public static bool operator ==(PhysicalAddress a1, PhysicalAddress a2)
			{
				return a1?.Equals(a2) ?? ((object)a2 == null);
			}

			public static bool operator !=(PhysicalAddress a1, PhysicalAddress a2)
			{
				return !(a1 == a2);
			}

			public bool Equals(PhysicalAddress other)
			{
				return CompareTo(other) == 0;
			}

			public override bool Equals(object obj)
			{
				if (obj == null || !(obj is PhysicalAddress))
				{
					return false;
				}
				return Equals(obj as PhysicalAddress);
			}

			public static int Compare(byte[] mac1, byte[] mac2)
			{
				int num = Math.Min(mac1.Length, mac2.Length);
				for (int i = 0; i < num; i++)
				{
					if (mac1[i] > mac2[i])
					{
						return 1;
					}
					if (mac1[i] < mac2[i])
					{
						return -1;
					}
				}
				if (mac1.Length < mac2.Length)
				{
					return -1;
				}
				if (mac1.Length > mac2.Length)
				{
					return 1;
				}
				return 0;
			}

			public int CompareTo(PhysicalAddress other)
			{
				if ((object)other == null)
				{
					return 1;
				}
				return Compare(Buffer, other.Buffer);
			}

			public int CompareTo(object obj)
			{
				if (obj != null && !(obj is PhysicalAddress))
				{
					throw new ArgumentException("Object must be of type PhysicalAddress.");
				}
				return CompareTo(obj as PhysicalAddress);
			}

			public void Clear()
			{
				for (int i = 0; i < Buffer.Length; i++)
				{
					Buffer[i] = 0;
				}
			}

			public void CopyFrom(byte[] mac)
			{
				Clear();
				int num = Math.Min(Buffer.Length, mac.Length);
				for (int i = 0; i < num; i++)
				{
					Buffer[i] = mac[i];
				}
			}

			public void CopyFrom(IReadOnlyList<byte> mac)
			{
				Clear();
				int num = Math.Min(Buffer.Length, mac.Count);
				for (int i = 0; i < num; i++)
				{
					Buffer[i] = mac[i];
				}
			}

			public void CopyFrom(IPhysicalAddress mac)
			{
				for (int i = 0; i < Buffer.Length; i++)
				{
					Buffer[i] = mac[i];
				}
			}

			public static implicit operator ulong(PhysicalAddress mac)
			{
				ulong num = 0uL;
				for (int i = 0; i < mac.Buffer.Length; i++)
				{
					num <<= 8;
					num += mac.Buffer[i];
				}
				return num;
			}

			public override string ToString()
			{
				string text = "";
				for (int i = 0; i < Buffer.Length; i++)
				{
					if (i > 0)
					{
						text += ":";
					}
					text += Buffer[i].HexString();
				}
				return text;
			}
		}

		public interface IAdapter : IEventSender, IDisposableManager, IDisposable, System.IDisposable
		{
			ITreeNode TreeNode { get; }

			string Name { get; }

			IPhysicalAddress MAC { get; }

			int BackgroundTxMessagesPerSecond { get; set; }

			bool IsConnected { get; }

			TimeSpan TimeSinceAdapterOpened { get; }

			long BytesSent { get; }

			long BytesReceived { get; }

			long MessagesSent { get; }

			long MessagesReceived { get; }

			TimeSpan TimeSinceLastMessageTx { get; }

			TimeSpan TimeSinceLastMessageRx { get; }

			Task<bool> OpenAsync(AsyncOperation obj);

			Task<bool> CloseAsync(AsyncOperation obj);
		}

		public interface IAdapter<T> : IAdapter, IEventSender, IDisposableManager, IDisposable, System.IDisposable where T : IMessage
		{
			bool Transmit(T message);
		}

		public abstract class Adapter : DisposableManager, IAdapter, IEventSender, IDisposableManager, IDisposable, System.IDisposable
		{
			public enum ICON
			{
				DISCONNECTED = 1,
				CONNECTED = 3
			}

			private class TxSerializer : Disposable
			{
				public readonly Adapter Adapter;

				private readonly object CriticalSection = new object();

				private PeriodicTask TxSerializerTask;

				private int mMessagesPerSecond;

				private RoundRobinPublisher RoundRobin;

				private TransmitTurnEvent NextTurnEvent;

				private int MessagesSent;

				private Timer Timer = new Timer();

				private static readonly TimeSpan DEBUG_TIME = TimeSpan.FromSeconds(1.0);

				public int MessagesPerSecond
				{
					get
					{
						return mMessagesPerSecond;
					}
					set
					{
						value = Math.Max(0, value);
						if (mMessagesPerSecond == value || base.IsDisposed)
						{
							return;
						}
						mMessagesPerSecond = value;
						if (TxSerializerTask != null || value <= 0)
						{
							return;
						}
						lock (CriticalSection)
						{
							if (TxSerializerTask == null)
							{
								TxSerializerTask = new PeriodicTask(BackgroundTransmitTask);
							}
						}
					}
				}

				internal TxSerializer(Adapter adapter)
				{
					Adapter = adapter;
					Adapter.AddDisposable(this);
					RoundRobin = Adapter.Events.CreateRoundRobinPublisher<TransmitTurnEvent>();
					NextTurnEvent = new TransmitTurnEvent(Adapter);
				}

				private TimeSpan BackgroundTransmitTask()
				{
					if (Adapter.IsDisposed || !Adapter.IsConnected || RoundRobin.SubscriberCount <= 0)
					{
						Timer.Reset();
						return TimeSpan.FromMilliseconds(25.0);
					}
					if (Timer.ElapsedTime >= DEBUG_TIME)
					{
						Timer.ElapsedTime -= DEBUG_TIME;
						MessagesSent = 0;
					}
					int num = (int)((double)MessagesPerSecond * Timer.ElapsedTime.TotalSeconds);
					int subscriberCount = RoundRobin.SubscriberCount;
					for (int i = 0; i < subscriberCount; i++)
					{
						if (MessagesSent > num)
						{
							break;
						}
						try
						{
							NextTurnEvent.Handled = false;
							RoundRobin.PublishNext(NextTurnEvent);
							if (NextTurnEvent.Handled)
							{
								MessagesSent++;
							}
						}
						catch (Exception)
						{
						}
					}
					if (MessagesSent >= num)
					{
						return TimeSpan.FromSeconds((1.0 + (double)MessagesSent) / (double)MessagesPerSecond) - Timer.ElapsedTime;
					}
					return TimeSpan.FromMilliseconds(2.0);
				}

				public override void Dispose(bool disposing)
				{
					if (disposing)
					{
						lock (CriticalSection)
						{
							TxSerializerTask?.Dispose();
						}
					}
				}
			}

			protected class MessageRateMetrics
			{
				private class Item
				{
					private long mTotal;

					private int Count;

					public long Total => Interlocked.Read(ref mTotal);

					public int PerSecond { get; private set; }

					public void Reset()
					{
						int count = (PerSecond = 0);
						mTotal = (Count = count);
					}

					public void Update(int value)
					{
						Interlocked.Add(ref mTotal, value);
						int num = Interlocked.Add(ref Count, value);
						if (PerSecond < num)
						{
							PerSecond = num;
						}
					}

					public void Task1sec()
					{
						PerSecond = Interlocked.Exchange(ref Count, 0);
					}
				}

				private readonly string Text;

				private readonly bool Verbose;

				private readonly Timer mTimeSinceLastMessage = new Timer();

				private readonly Item Messages = new Item();

				private readonly Item Bytes = new Item();

				private bool Paused = true;

				public long TotalMessages => Messages.Total;

				public long TotalBytes => Bytes.Total;

				public int MessagesPerSecond => Messages.PerSecond;

				public int BytesPerSecond => Bytes.PerSecond;

				public TimeSpan TimeSinceLastMessage => mTimeSinceLastMessage.ElapsedTime;

				public MessageRateMetrics(Adapter owner, string text, bool verbose)
				{
					MessageRateMetrics messageRateMetrics = this;
					Text = text;
					Verbose = verbose;
					Task.Run(async delegate
					{
						while (!owner.IsDisposed)
						{
							if (messageRateMetrics.Paused)
							{
								await Task.Delay(100).ConfigureAwait(false);
							}
							else
							{
								messageRateMetrics.Messages.Task1sec();
								messageRateMetrics.Bytes.Task1sec();
								_ = owner.Verbose;
								await Task.Delay(1000).ConfigureAwait(false);
							}
						}
					});
				}

				public void Reset()
				{
					Messages.Reset();
					Bytes.Reset();
				}

				public void Stop()
				{
					Paused = true;
				}

				public void Start()
				{
					Paused = false;
				}

				public void OnMessageReceived(int length)
				{
					mTimeSinceLastMessage.Reset();
					Messages.Update(1);
					Bytes.Update(length);
				}
			}

			public readonly bool Verbose;

			protected readonly object EventLock = new object();

			protected readonly SubscriptionManager Subscriptions = new SubscriptionManager();

			protected readonly MessageRateMetrics RxMetrics;

			protected readonly MessageRateMetrics TxMetrics;

			private readonly TxSerializer TransmitSerializer;

			private readonly AdapterOpenedEvent mAdapterOpenedEvent;

			private readonly AdapterClosedEvent mAdapterClosedEvent;

			private readonly Timer AdapterOpenedTime = new Timer();

			public IEventPublisher Events { get; private set; }

			public string Name { get; private set; }

			public ITreeNode TreeNode { get; private set; }

			public bool IsConnected { get; private set; }

			public TimeSpan TimeSinceAdapterOpened => AdapterOpenedTime.ElapsedTime;

			public int BackgroundTxMessagesPerSecond
			{
				get
				{
					return TransmitSerializer.MessagesPerSecond;
				}
				set
				{
					TransmitSerializer.MessagesPerSecond = value;
				}
			}

			public abstract IPhysicalAddress MAC { get; }

			public long BytesSent => TxMetrics.TotalBytes;

			public long BytesReceived => RxMetrics.TotalBytes;

			public long MessagesSent => TxMetrics.TotalMessages;

			public long MessagesReceived => RxMetrics.TotalMessages;

			public TimeSpan TimeSinceLastMessageTx => TxMetrics.TimeSinceLastMessage;

			public TimeSpan TimeSinceLastMessageRx => RxMetrics.TimeSinceLastMessage;

			static Adapter()
			{
				ImageCache.RegisterEnumImageReferences(typeof(ICON));
			}

			public Adapter(string name)
				: this(name, verbose: false)
			{
			}

			public Adapter(string name, bool verbose)
			{
				Name = name;
				Verbose = verbose;
				IsConnected = false;
				Events = new EventPublisher("IDS.Core.Communications.Adapter.Events");
				mAdapterOpenedEvent = new AdapterOpenedEvent(this);
				mAdapterClosedEvent = new AdapterClosedEvent(this);
				RxMetrics = new MessageRateMetrics(this, "Rx", verbose);
				TxMetrics = new MessageRateMetrics(this, "Tx", verbose);
				TransmitSerializer = new TxSerializer(this);
				TreeNode = IDS.Core.TreeNode.Create(this);
				AddDisposable(TreeNode);
				TreeNode.Text = Name;
				TreeNode.Icon = ICON.DISCONNECTED;
				TreeNode.Data = this;
			}

			protected abstract Task<bool> ConnectAsync(AsyncOperation obj);

			protected abstract Task<bool> DisconnectAsync(AsyncOperation obj);

			public async Task<bool> OpenAsync(AsyncOperation obj)
			{
				if (base.IsDisposed)
				{
					return false;
				}
				return await ConnectAsync(obj);
			}

			public async Task<bool> CloseAsync(AsyncOperation obj)
			{
				if (base.IsDisposed)
				{
					return true;
				}
				return await DisconnectAsync(obj);
			}

			protected void RaiseAdapterOpened()
			{
				if (base.IsDisposed || IsConnected)
				{
					return;
				}
				lock (EventLock)
				{
					if (!base.IsDisposed && !IsConnected)
					{
						IsConnected = true;
						TreeNode.Icon = ICON.CONNECTED;
						AdapterOpenedTime.Reset();
						RxMetrics.Reset();
						RxMetrics.Start();
						TxMetrics.Reset();
						TxMetrics.Start();
						mAdapterOpenedEvent.Publish();
					}
				}
			}

			protected void RaiseAdapterClosed()
			{
				if (base.IsDisposed || !IsConnected)
				{
					return;
				}
				lock (EventLock)
				{
					if (!base.IsDisposed && IsConnected)
					{
						IsConnected = false;
						TreeNode.Icon = ICON.DISCONNECTED;
						RxMetrics.Stop();
						TxMetrics.Stop();
						mAdapterClosedEvent.Publish();
					}
				}
			}

			public override async void Dispose(bool disposing)
			{
				if (disposing)
				{
					await CloseAsync(new AsyncOperation(TimeSpan.FromSeconds(5.0)));
					IsConnected = false;
					TransmitSerializer.Dispose();
					lock (EventLock)
					{
						new AdapterDisposedEvent(this).Publish();
						Events.Dispose();
					}
					Subscriptions.Dispose();
				}
				base.Dispose(disposing);
			}
		}

		public abstract class Adapter<T> : Adapter, IAdapter<T>, IAdapter, IEventSender, IDisposableManager, IDisposable, System.IDisposable where T : IMessageBuffer, new()
		{
			private readonly IMessageEncoder<T> Encoder;

			private readonly MessageDecoder<T> Decoder;

			private readonly AdapterRxEvent<T> AdapterRxEvent;

			private readonly AdapterRxEvent<T> DecodedRxEvent;

			private readonly AdapterTxEvent<T> AdapterTxEvent;

			private readonly MessageRateMetrics LowLevelRxMetrics;

			private readonly MessageRateMetrics LowLevelTxMetrics;

			public long LowLevelBytesSent => LowLevelTxMetrics.TotalBytes;

			public long LowLevelBytesReceived => LowLevelRxMetrics.TotalBytes;

			public long LowLevelMessagesSent => LowLevelTxMetrics.TotalMessages;

			public long LowLevelMessagesReceived => LowLevelRxMetrics.TotalMessages;

			public Adapter(string name)
				: this(name, (IMessageEncoder<T>)null, (MessageDecoder<T>)null, verbose: false)
			{
			}

			public Adapter(string name, IMessageEncoder<T> encoder)
				: this(name, encoder, (MessageDecoder<T>)null, verbose: false)
			{
			}

			public Adapter(string name, MessageDecoder<T> decoder)
				: this(name, (IMessageEncoder<T>)null, decoder, verbose: false)
			{
			}

			public Adapter(string name, bool verbose)
				: this(name, (IMessageEncoder<T>)null, (MessageDecoder<T>)null, verbose)
			{
			}

			public Adapter(string name, IMessageEncoder<T> encoder, bool verbose)
				: this(name, encoder, (MessageDecoder<T>)null, verbose)
			{
			}

			public Adapter(string name, MessageDecoder<T> decoder, bool verbose)
				: this(name, (IMessageEncoder<T>)null, decoder, verbose)
			{
			}

			public Adapter(string name, IMessageEncoder<T> encoder, MessageDecoder<T> decoder)
				: this(name, encoder, decoder, verbose: false)
			{
			}

			public Adapter(string name, IMessageEncoder<T> encoder, MessageDecoder<T> decoder, bool verbose)
				: base(name, verbose)
			{
				AdapterRxEvent = new AdapterRxEvent<T>(this);
				DecodedRxEvent = new AdapterRxEvent<T>(this);
				AdapterTxEvent = new AdapterTxEvent<T>(this);
				Encoder = encoder;
				if (Encoder != null)
				{
					AddDisposable(Encoder);
				}
				Decoder = decoder;
				if (Decoder != null)
				{
					AddDisposable(Decoder);
					Decoder.Action = OnDecodedMessageRx;
				}
				LowLevelTxMetrics = ((Encoder == null) ? TxMetrics : new MessageRateMetrics(this, "EncodedTx", verbose));
				LowLevelRxMetrics = ((Decoder == null) ? RxMetrics : new MessageRateMetrics(this, "EncodedRx", verbose));
				base.Events.Subscribe<AdapterOpenedEvent>(OnAdapterOpened, SubscriptionType.Strong, Subscriptions);
				base.Events.Subscribe<AdapterClosedEvent>(OnAdapterClosed, SubscriptionType.Strong, Subscriptions);
			}

			protected abstract bool TransmitRaw(T message);

			private void OnAdapterOpened(AdapterOpenedEvent message)
			{
				LowLevelRxMetrics.Reset();
				LowLevelRxMetrics.Start();
				LowLevelTxMetrics.Reset();
				LowLevelTxMetrics.Start();
			}

			private void OnAdapterClosed(AdapterClosedEvent message)
			{
				LowLevelRxMetrics.Stop();
				LowLevelTxMetrics.Stop();
			}

			protected void RaiseMessageRx(T rx, bool echo)
			{
				if (base.IsDisposed || !base.IsConnected)
				{
					return;
				}
				lock (EventLock)
				{
					if (!base.IsDisposed && base.IsConnected)
					{
						if (Decoder == null)
						{
							RxMetrics.OnMessageReceived(rx.Length);
							AdapterRxEvent.Publish(rx, echo);
						}
						else
						{
							LowLevelRxMetrics.OnMessageReceived(rx.Length);
							Decoder.DecodeFromStream(rx, echo, rx.Timestamp);
						}
					}
				}
			}

			private void OnDecodedMessageRx(T decoded, bool echo)
			{
				RxMetrics.OnMessageReceived(decoded.Length);
				DecodedRxEvent.Publish(decoded, echo);
			}

			public virtual bool Transmit(T message)
			{
				if (!base.IsConnected)
				{
					return false;
				}
				bool flag = false;
				if (Encoder != null)
				{
					T val = Encoder.Encode(message);
					try
					{
						flag = TransmitRaw(val);
						if (flag)
						{
							LowLevelTxMetrics.OnMessageReceived(val.Length);
						}
					}
					finally
					{
						if ((object)val is ResourcePool.IObject @object && @object.IsMemberOfPool)
						{
							@object.ReturnToPool();
						}
					}
				}
				else
				{
					flag = TransmitRaw(message);
				}
				if (flag && message != null)
				{
					TxMetrics.OnMessageReceived(message.Length);
					AdapterTxEvent.Publish(message);
				}
				return flag;
			}
		}

		public static class COBS
		{
			public class Decoder<T> : MessageDecoder<T> where T : MessageBuffer, new()
			{
				private T Message = ResourcePool<T>.GetObject();

				private int CodeByte;

				public override void Reset()
				{
					Message?.Clear();
					CodeByte = 0;
				}

				public override void DecodeFromStream(IReadOnlyCollection<byte> stream, bool echo, TimeSpan timestamp)
				{
					foreach (byte item in stream)
					{
						T val = DecodeByte(item);
						if (val == null)
						{
							continue;
						}
						val.Timestamp = timestamp;
						try
						{
							base.Action?.Invoke(val, echo);
						}
						finally
						{
							if (val.IsMemberOfPool)
							{
								val.ReturnToPool();
							}
						}
					}
				}

				private T DecodeByte(byte b)
				{
					if (base.IsDisposed)
					{
						return null;
					}
					if (b == 0)
					{
						int codeByte = CodeByte;
						CodeByte = 0;
						if (codeByte == 0 && Message.Length > 1)
						{
							byte b2 = Message[Message.Length - 1];
							Message.Length--;
							if (CRC8.Calculate(Message) == b2)
							{
								T val = Interlocked.Exchange(ref Message, ResourcePool<T>.GetObject());
								if (val == null)
								{
									Message.ReturnToPool();
								}
								return val;
							}
						}
						Message.Clear();
						return null;
					}
					if (CodeByte <= 0)
					{
						CodeByte = b & 0xFF;
					}
					else
					{
						CodeByte--;
						Message?.Append(b);
					}
					if ((CodeByte & 0x3F) == 0)
					{
						while (CodeByte > 0)
						{
							Message?.Append((byte)0);
							CodeByte -= 64;
						}
					}
					return null;
				}

				public override void Dispose(bool disposing)
				{
					if (disposing)
					{
						T msg = Interlocked.Exchange(ref Message, null);
						Task.Run(async delegate
						{
							await Task.Delay(1000).ConfigureAwait(false);
							msg?.ReturnToPool();
						});
					}
				}
			}

			public class Encoder<T> : Disposable, IMessageEncoder<T>, IDisposable, System.IDisposable where T : MessageBuffer, new()
			{
				public T Encode(T src)
				{
					if (base.IsDisposed)
					{
						return null;
					}
					T @object = ResourcePool<T>.GetObject();
					@object.Append((byte)0);
					if (src == null || src.Length <= 0)
					{
						return @object;
					}
					byte b = CRC8.Calculate(src);
					int num = 0;
					do
					{
						int length = @object.Length;
						int num2 = 0;
						@object.Append((byte)0);
						do
						{
							byte b2 = ((num < src.Length) ? src[num] : b);
							if (b2 == 0)
							{
								break;
							}
							num++;
							@object.Append(b2);
						}
						while (++num2 < 63 && num <= src.Length);
						while (num <= src.Length && ((num < src.Length) ? src[num] : b) == 0)
						{
							num++;
							num2 += 64;
							if (num2 >= 192)
							{
								break;
							}
						}
						@object[length] = (byte)num2;
					}
					while (num <= src.Length);
					@object.Append((byte)0);
					return @object;
				}

				public override void Dispose(bool disposing)
				{
				}
			}

			private const byte FRAME_CHARACTER = 0;

			private const int DATA_BIT_COUNT = 6;

			private const int FRAME_BYTE_COUNT_LSB = 64;

			private const int MAX_DATA_BYTES = 63;
		}

		public class AdapterOpenedEvent : Event
		{
			public readonly IAdapter Adapter;

			public AdapterOpenedEvent(IAdapter adapter)
				: base(adapter)
			{
				Adapter = adapter;
			}
		}

		public class AdapterClosedEvent : Event
		{
			public readonly IAdapter Adapter;

			public AdapterClosedEvent(IAdapter adapter)
				: base(adapter)
			{
				Adapter = adapter;
			}
		}

		public class AdapterDisposedEvent : Event
		{
			public readonly IAdapter Adapter;

			public AdapterDisposedEvent(IAdapter adapter)
				: base(adapter)
			{
				Adapter = adapter;
			}
		}

		public class AdapterRxEvent<T> : Event, IMessage, IByteList, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>, ITimeStamp where T : IMessage, new()
		{
			public readonly IAdapter<T> Adapter;

			public T Message { get; private set; }

			public bool Echo { get; private set; }

			public int Length => Message.Length;

			public int Count => Message.Count;

			public TimeSpan Timestamp => Message.Timestamp;

			public byte this[int index] => Message[index];

			public AdapterRxEvent(IAdapter<T> adapter)
				: base(adapter)
			{
				Adapter = adapter;
			}

			public void Publish(T message, bool echo)
			{
				Message = message;
				Echo = echo;
				Publish();
			}

			public void CopyTo(byte[] array, int index)
			{
				Message.CopyTo(array, index);
			}

			public void CopyRangeTo(int sourceIndex, int count, byte[] array, int destIndex)
			{
				Message.CopyRangeTo(sourceIndex, count, array, destIndex);
			}

			public string ToString(bool dataonly)
			{
				return Message.ToString();
			}

			public IEnumerator<byte> GetEnumerator()
			{
				return Message.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return Message.GetEnumerator();
			}
		}

		public class AdapterTxEvent<T> : Event, IMessage, IByteList, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>, ITimeStamp where T : IMessage, new()
		{
			public readonly IAdapter<T> Adapter;

			public T Message { get; private set; }

			public int Length => Message.Length;

			public int Count => Message.Count;

			public TimeSpan Timestamp => Message.Timestamp;

			public byte this[int index] => Message[index];

			public AdapterTxEvent(IAdapter<T> adapter)
				: base(adapter)
			{
				Adapter = adapter;
			}

			public void Publish(T message)
			{
				Message = message;
				Publish();
			}

			public void CopyTo(byte[] array, int index)
			{
				Message.CopyTo(array, index);
			}

			public void CopyRangeTo(int sourceIndex, int count, byte[] array, int destIndex)
			{
				Message.CopyRangeTo(sourceIndex, count, array, destIndex);
			}

			public string ToString(bool dataonly)
			{
				return Message.ToString();
			}

			public IEnumerator<byte> GetEnumerator()
			{
				return Message.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return Message.GetEnumerator();
			}
		}

		public class TransmitTurnEvent : Event
		{
			public readonly IAdapter Adapter;

			public bool Handled;

			public TransmitTurnEvent(IAdapter adapter)
				: base(adapter)
			{
				Adapter = adapter;
			}
		}

		public interface ITimeStamp
		{
			TimeSpan Timestamp { get; }
		}

		public interface IByteList : IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>
		{
			int Length { get; }

			void CopyTo(byte[] array, int index);

			void CopyRangeTo(int sourceIndex, int count, byte[] array, int destIndex);

			string ToString(bool dataonly);
		}

		public interface IByteBuffer : IByteList, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>
		{
			new int Length { get; set; }

			new byte this[int index] { get; set; }

			int Capacity { get; set; }

			void Clear();

			void Append(byte value);

			void Append(sbyte value);

			void Append(char value);

			void Append(short value);

			void Append(ushort value);

			void Append(Int24 value);

			void Append(UInt24 value);

			void Append(int value);

			void Append(uint value);

			void Append(Int40 value);

			void Append(UInt40 value);

			void Append(Int48 value);

			void Append(UInt48 value);

			void Append(Int56 value);

			void Append(UInt56 value);

			void Append(long value);

			void Append(ulong value);

			void Append(IByteList buffer);

			void Append(IByteList buffer, int count);

			void Append(IByteList buffer, int index, int count);

			void Append(byte[] buffer);

			void Append(byte[] buffer, int count);

			void Append(byte[] buffer, int index, int count);
		}

		public interface IMessage : IByteList, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>, ITimeStamp
		{
		}

		public interface IMessageBuffer : IMessage, IByteList, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>, ITimeStamp, IByteBuffer
		{
			new TimeSpan Timestamp { get; set; }
		}

		public class MessageBuffer : ResourcePool.Object, IMessageBuffer, IMessage, IByteList, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>, ITimeStamp, IByteBuffer
		{
			private const int DEFAULT_CAPACITY = 64;

			private static readonly double StartTicks_sec = Timer.RawTicks_sec;

			public int Length { get; set; }

			public int Count => Length;

			public TimeSpan Timestamp { get; set; } = TimeSpan.Zero;


			public byte[] Data { get; private set; }

			public virtual int Capacity
			{
				get
				{
					return Data.Length;
				}
				set
				{
					if (value > Capacity)
					{
						byte[] array = new byte[value];
						if (Length > 0)
						{
							Array.Copy(Data, array, Length);
						}
						Data = array;
					}
				}
			}

			public byte this[int index]
			{
				get
				{
					return Data[index];
				}
				set
				{
					Data[index] = value;
				}
			}

			public void SetTimeStamp()
			{
				Timestamp = TimeSpan.FromSeconds(Timer.RawTicks_sec - StartTicks_sec);
			}

			public MessageBuffer()
				: this(64)
			{
			}

			public MessageBuffer(int capacity)
			{
				if (capacity < 1)
				{
					throw new ArgumentException("MessageBuffer.Capacity must be at least 1 byte");
				}
				Data = new byte[capacity];
			}

			public IEnumerator<byte> GetEnumerator()
			{
				for (int i = 0; i < Length; i++)
				{
					yield return Data[i];
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public virtual void Clear()
			{
				Length = 0;
				Timestamp = TimeSpan.Zero;
			}

			protected override void ResetPoolObjectState()
			{
				Clear();
			}

			public void CopyTo(byte[] array, int index)
			{
				Array.Copy(Data, 0, array, index, Length);
			}

			public void CopyRangeTo(int sourceIndex, int count, byte[] array, int destIndex)
			{
				Array.Copy(Data, sourceIndex, array, destIndex, count);
			}

			public virtual void CopyFrom(IMessage src)
			{
				Length = 0;
				Append(src);
				Timestamp = src.Timestamp;
			}

			public virtual void CopyFrom(byte[] buffer)
			{
				Length = 0;
				Append(buffer);
			}

			public virtual void CopyFrom(byte[] buffer, int count)
			{
				Length = 0;
				Append(buffer, count);
			}

			public void Append(byte value)
			{
				EnsureCapacity(Length + 1);
				Data[Length++] = value;
			}

			public void Append(sbyte value)
			{
				Append((byte)value);
			}

			public void Append(char value)
			{
				Append((byte)value);
			}

			public void Append(short value)
			{
				Append((byte)(value >> 8));
				Append((byte)value);
			}

			public void Append(ushort value)
			{
				Append((byte)(value >> 8));
				Append((byte)value);
			}

			public void Append(Int24 value)
			{
				Append((byte)(value >> 16));
				Append((ushort)value);
			}

			public void Append(UInt24 value)
			{
				Append((byte)(value >> 16));
				Append((ushort)value);
			}

			public void Append(int value)
			{
				Append((ushort)(value >> 16));
				Append((ushort)value);
			}

			public void Append(uint value)
			{
				Append((ushort)(value >> 16));
				Append((ushort)value);
			}

			public void Append(Int40 value)
			{
				Append((uint)(value >> 8));
				Append((byte)value);
			}

			public void Append(UInt40 value)
			{
				Append((uint)(value >> 8));
				Append((byte)value);
			}

			public void Append(Int48 value)
			{
				Append((uint)(value >> 16));
				Append((ushort)value);
			}

			public void Append(UInt48 value)
			{
				Append((uint)(value >> 16));
				Append((ushort)value);
			}

			public void Append(Int56 value)
			{
				Append((uint)(value >> 24));
				Append((ushort)(value >> 8));
				Append((byte)value);
			}

			public void Append(UInt56 value)
			{
				Append((uint)(value >> 24));
				Append((ushort)(value >> 8));
				Append((byte)value);
			}

			public void Append(long value)
			{
				Append((uint)(value >> 32));
				Append((uint)value);
			}

			public void Append(ulong value)
			{
				Append((uint)(value >> 32));
				Append((uint)value);
			}

			public void Append(IByteList buffer)
			{
				Append(buffer, buffer.Count);
			}

			public void Append(IByteList buffer, int count)
			{
				Append(buffer, 0, count);
			}

			public void Append(IByteList buffer, int index, int count)
			{
				EnsureCapacity(Length + count);
				buffer.CopyRangeTo(index, count, Data, Length);
				Length += count;
			}

			public void Append(byte[] buffer)
			{
				Append(buffer, buffer.Length);
			}

			public void Append(byte[] buffer, int count)
			{
				Append(buffer, 0, count);
			}

			public void Append(byte[] buffer, int index, int count)
			{
				EnsureCapacity(Length + count);
				Array.Copy(buffer, index, Data, Length, count);
				Length += count;
			}

			private void EnsureCapacity(int min)
			{
				if (Capacity < min)
				{
					Capacity = Math.Max(min, Capacity * 2);
				}
			}

			public override string ToString()
			{
				return Comm.ToString((IReadOnlyList<byte>)this);
			}

			public virtual string ToString(bool dataonly)
			{
				return Comm.ToString((IReadOnlyList<byte>)this, dataonly);
			}
		}

		public abstract class MessageDecoder<T> : Disposable where T : IMessage
		{
			public Action<T, bool> Action { get; set; }

			public abstract void Reset();

			public abstract void DecodeFromStream(IReadOnlyCollection<byte> stream, bool echo, TimeSpan timestamp);
		}

		public interface IMessageEncoder<T> : IDisposable, System.IDisposable where T : IMessage
		{
			T Encode(T src);
		}

		public class SocketClient<T> : Adapter<T> where T : MessageBuffer, new()
		{
			private class NetworkInterfaceCardAddress : PhysicalAddress
			{
				public NetworkInterfaceCardAddress()
					: base(6)
				{
					SetRandomMACValue();
				}
			}

			private class ConnectionManager : Disposable
			{
				private const int RAW_RX_BUF_SIZE = 65536;

				private const int RAW_TX_BUF_SIZE = 2048;

				private readonly bool Verbose;

				private SocketClient<T> Adapter;

				private TcpClient tcpClient;

				private CancellationTokenSource masterCTS = new CancellationTokenSource();

				private bool IsConnected;

				private readonly Task healthTask;

				private ConcurrentQueue<T> TxQueue = new ConcurrentQueue<T>();

				public ConnectionManager(SocketClient<T> adapter, bool verbose)
				{
					Adapter = adapter;
					Adapter.AddDisposable(this);
					Verbose = verbose;
					healthTask = Task.Run((Func<Task>)HealthTask, masterCTS.Token);
				}

				private void ClearTxQueue()
				{
					T val;
					while (TxQueue.TryDequeue(out val))
					{
						val?.ReturnToPool();
					}
				}

				public bool Transmit(T msg)
				{
					if (base.IsDisposed)
					{
						return false;
					}
					if (!msg.IsMemberOfPool)
					{
						throw new InvalidOperationException("only pooled Communications.MessageBuffers buffers are accepted by this Adapter");
					}
					if (IsConnected)
					{
						msg.Retain();
						TxQueue.Enqueue(msg);
						return true;
					}
					return false;
				}

				private async Task HealthTask()
				{
					Task rxTask = null;
					Task txTask = null;
					CancellationTokenSource cts = null;
					try
					{
						_ = 2;
						try
						{
							while (!base.IsDisposed)
							{
								IsConnected = false;
								while (true)
								{
									if (!IsConnected)
									{
										if (base.IsDisposed)
										{
											return;
										}
										try
										{
											tcpClient?.Dispose();
										}
										catch (Exception)
										{
											_ = Verbose;
										}
										tcpClient = null;
										try
										{
											tcpClient = new TcpClient();
											await tcpClient.ConnectAsync(Adapter.Address, Adapter.Port);
											if (base.IsDisposed)
											{
												return;
											}
											ClearTxQueue();
											IsConnected = true;
											goto IL_0172;
										}
										catch (Exception)
										{
											_ = Verbose;
											goto IL_0172;
										}
									}
									if (base.IsDisposed)
									{
										return;
									}
									Adapter.RaiseAdapterOpened();
									cts = new CancellationTokenSource();
									rxTask = Task.Run(() => RxBackgroundTask(cts.Token), cts.Token);
									txTask = Task.Run(() => TxBackgroundTask(cts.Token), cts.Token);
									while (true)
									{
										if (!IsConnected)
										{
											_ = Verbose;
											cts.Cancel();
											try
											{
												Task.WaitAll(rxTask, txTask);
											}
											catch (Exception)
											{
												_ = Verbose;
											}
											cts.Dispose();
											cts = null;
											rxTask = null;
											txTask = null;
											break;
										}
										await Task.Delay(10, masterCTS.Token);
										if (!base.IsDisposed)
										{
											continue;
										}
										goto end_IL_0000;
									}
									break;
									IL_0172:
									if (!IsConnected)
									{
										await Task.Delay(10, masterCTS.Token);
									}
								}
							}
							end_IL_0000:;
						}
						catch (Exception)
						{
							_ = Verbose;
						}
					}
					finally
					{
						IsConnected = false;
						_ = Verbose;
						if (tcpClient != null)
						{
							try
							{
								tcpClient.Dispose();
							}
							catch (Exception)
							{
								_ = Verbose;
							}
							tcpClient = null;
						}
						if (cts != null)
						{
							try
							{
								_ = Verbose;
								cts.Cancel();
							}
							catch (Exception)
							{
								_ = Verbose;
							}
							if (rxTask != null)
							{
								try
								{
									_ = Verbose;
									await rxTask.ConfigureAwait(false);
								}
								catch (Exception)
								{
									_ = Verbose;
								}
							}
							if (txTask != null)
							{
								try
								{
									_ = Verbose;
									await txTask.ConfigureAwait(false);
								}
								catch (Exception)
								{
									_ = Verbose;
								}
							}
							try
							{
								_ = Verbose;
								cts.Dispose();
							}
							catch (Exception)
							{
								_ = Verbose;
							}
							cts = null;
						}
						_ = Verbose;
					}
				}

				private async Task RxBackgroundTask(CancellationToken ct)
				{
					try
					{
						T rx_buf = new T
						{
							Capacity = 65536
						};
						tcpClient.GetStream();
						while (IsConnected && !ct.IsCancellationRequested)
						{
							T val = rx_buf;
							val.Length = await tcpClient.GetStream().ReadAsync(rx_buf.Data, 0, rx_buf.Data.Length, ct).ConfigureAwait(false);
							if (rx_buf.Length == 0)
							{
								break;
							}
							if (!base.IsDisposed)
							{
								rx_buf.SetTimeStamp();
								Adapter.RaiseMessageRx(rx_buf, echo: false);
							}
						}
					}
					catch (Exception)
					{
						_ = Verbose;
					}
					finally
					{
						IsConnected = false;
					}
				}

				private async Task TxBackgroundTask(CancellationToken ct)
				{
					_ = 2;
					try
					{
						MessageBuffer txbuf = new MessageBuffer(2048);
						Timer flushtimer = new Timer();
						while (IsConnected && !ct.IsCancellationRequested && !base.IsDisposed)
						{
							T val;
							while (txbuf.Length < 1024 && TxQueue.TryDequeue(out val))
							{
								try
								{
									if (txbuf.Length == 0)
									{
										flushtimer.Reset();
									}
									txbuf.Append(val);
								}
								finally
								{
									val?.ReturnToPool();
								}
							}
							if (flushtimer.ElapsedTime >= SocketClient<T>.WRITE_FLUSH_TIME || txbuf.Length >= 512)
							{
								await tcpClient.GetStream().WriteAsync(txbuf.Data, 0, txbuf.Length).ConfigureAwait(false);
								await tcpClient.GetStream().FlushAsync(ct).ConfigureAwait(false);
								txbuf.Length = 0;
							}
							if (TxQueue.Count <= 0)
							{
								int num = 5;
								if (txbuf.Length > 0)
								{
									num = 10 - (int)flushtimer.ElapsedTime.TotalMilliseconds;
								}
								if (num > 0)
								{
									await Task.Delay(num, ct).ConfigureAwait(false);
								}
							}
						}
					}
					catch (Exception)
					{
						_ = Verbose;
					}
					finally
					{
						IsConnected = false;
					}
				}

				public override void Dispose(bool disposing)
				{
					if (disposing)
					{
						_ = Verbose;
						masterCTS?.Cancel();
						IsConnected = false;
						SocketClient<T> adapter = Adapter;
						Adapter = null;
						try
						{
							tcpClient?.Dispose();
						}
						catch
						{
						}
						tcpClient = null;
						_ = Verbose;
						try
						{
							healthTask.Wait();
						}
						catch (Exception)
						{
							_ = Verbose;
						}
						masterCTS?.Dispose();
						masterCTS = null;
						_ = Verbose;
						adapter?.RaiseAdapterClosed();
						ClearTxQueue();
					}
				}
			}

			private static readonly TimeSpan WRITE_FLUSH_TIME = TimeSpan.FromMilliseconds(10.0);

			private readonly object CriticalSection = new object();

			private readonly PhysicalAddress mMac = new NetworkInterfaceCardAddress();

			private ConnectionManager mConnection;

			public string Address { get; private set; }

			public int Port { get; private set; }

			private ConnectionManager Connection
			{
				get
				{
					return mConnection;
				}
				set
				{
					if (mConnection == value)
					{
						return;
					}
					lock (CriticalSection)
					{
						if (mConnection == value)
						{
							return;
						}
						if (mConnection != null)
						{
							_ = Verbose;
							try
							{
								mConnection.Dispose();
							}
							catch
							{
							}
							mConnection = null;
							_ = Verbose;
							RaiseAdapterClosed();
						}
						mConnection = value;
					}
				}
			}

			public override IPhysicalAddress MAC => mMac;

			public SocketClient(string address, int port)
				: this(address, port, (IMessageEncoder<T>)null, (MessageDecoder<T>)null)
			{
			}

			public SocketClient(string address, int port, IMessageEncoder<T> encoder)
				: this(address, port, encoder, (MessageDecoder<T>)null)
			{
			}

			public SocketClient(string address, int port, MessageDecoder<T> decoder)
				: this(address, port, (IMessageEncoder<T>)null, decoder)
			{
			}

			public SocketClient(string address, int port, IMessageEncoder<T> encoder, MessageDecoder<T> decoder)
				: this(address, port, encoder, decoder, verbose: false)
			{
			}

			public SocketClient(string address, int port, IMessageEncoder<T> encoder, MessageDecoder<T> decoder, bool verbose)
				: this(address.ToString() + ":" + port, address, port, encoder, decoder, verbose)
			{
			}

			public SocketClient(string name, string address, int port, IMessageEncoder<T> encoder, MessageDecoder<T> decoder)
				: this(name, address, port, encoder, decoder, verbose: false)
			{
			}

			public SocketClient(string name, string address, int port, IMessageEncoder<T> encoder, MessageDecoder<T> decoder, bool verbose)
				: base(name, encoder, decoder, verbose)
			{
				Address = address;
				Port = port;
			}

			protected override async Task<bool> ConnectAsync(AsyncOperation obj)
			{
				if (Connection == null)
				{
					lock (CriticalSection)
					{
						if (base.IsDisposed)
						{
							return false;
						}
						if (Connection == null)
						{
							Connection = new ConnectionManager(this, Verbose);
						}
					}
				}
				try
				{
					while (true)
					{
						obj.ThrowIfCancellationRequested();
						if (base.IsConnected)
						{
							return true;
						}
						if (base.IsDisposed)
						{
							return false;
						}
						if (Connection == null)
						{
							break;
						}
						await Task.Delay(33).ConfigureAwait(false);
					}
					return false;
				}
				finally
				{
					if (!base.IsConnected)
					{
						Connection = null;
					}
				}
			}

			protected override async Task<bool> DisconnectAsync(AsyncOperation obj)
			{
				Connection = null;
				if (Connection == null)
				{
					return true;
				}
				while (true)
				{
					obj.ThrowIfCancellationRequested();
					if (Connection == null)
					{
						return true;
					}
					if (base.IsDisposed)
					{
						break;
					}
					await Task.Delay(33).ConfigureAwait(false);
				}
				return true;
			}

			protected override bool TransmitRaw(T buffer)
			{
				return Connection?.Transmit(buffer) ?? false;
			}

			public override void Dispose(bool disposing)
			{
				if (disposing)
				{
					Connection = null;
				}
				base.Dispose(disposing);
			}
		}

		private static StringBuilder sb = new StringBuilder();

		public static string ToString(IReadOnlyList<byte> msg)
		{
			return ToString(msg, dataonly: false);
		}

		public static string ToString(IReadOnlyList<byte> msg, bool dataonly)
		{
			if (msg.Count <= 0)
			{
				if (dataonly)
				{
					return "EMPTY";
				}
				return "Data[0] = [EMPTY]";
			}
			lock (sb)
			{
				sb.Clear();
				if (!dataonly)
				{
					StringBuilder stringBuilder = sb;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder);
					handler.AppendLiteral("Data[");
					handler.AppendFormatted(msg.Count);
					handler.AppendLiteral("] = [");
					stringBuilder.Append(ref handler);
				}
				int num = 0;
				foreach (byte item in msg)
				{
					if (num++ > 0)
					{
						sb.Append(' ');
					}
					sb.Append(item.HexString());
				}
				if (!dataonly)
				{
					sb.Append(']');
				}
				return sb.ToString();
			}
		}
	}
}
