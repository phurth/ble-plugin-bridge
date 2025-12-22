using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.Events;
using IDS.Core.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IDS.Core
{
	public static class CAN
	{
		public interface IAdapter : Comm.IAdapter, IEventSender, IDisposableManager, IDisposable, System.IDisposable
		{
			int BaudRate { get; }
		}

		public interface IAdapter<T> : Comm.IAdapter<T>, Comm.IAdapter, IEventSender, IDisposableManager, IDisposable, System.IDisposable, IAdapter where T : IMessage
		{
			bool Transmit(ID id, PAYLOAD payload = default(PAYLOAD));
		}

		public abstract class Adapter<T> : Comm.Adapter<T>, IAdapter<T>, Comm.IAdapter<T>, Comm.IAdapter, IEventSender, IDisposableManager, IDisposable, System.IDisposable, IAdapter where T : MessageBuffer, new()
		{
			private class TransmitRateManager
			{
				private const int MAX_PENDING_BITS_MS = 20;

				private readonly int Limit;

				private int AccumulatedBits;

				public bool TransmitAllowed => AccumulatedBits <= Limit;

				public TransmitRateManager(IAdapter adapter)
				{
					TransmitRateManager transmitRateManager = this;
					Limit = adapter.BaudRate * 20 / 1000;
					Task.Run(async delegate
					{
						Timer timer = new Timer();
						while (!adapter.IsDisposed)
						{
							int val = (int)((double)adapter.BaudRate * timer.GetElapsedTimeAndReset().TotalSeconds);
							val = Math.Min(val, transmitRateManager.AccumulatedBits);
							Interlocked.Add(ref transmitRateManager.AccumulatedBits, -val);
							await Task.Delay(10).ConfigureAwait(false);
						}
					});
				}

				public void AddBits(MessageBuffer buffer)
				{
					Interlocked.Add(ref AccumulatedBits, buffer.EstimateNumberOfBitsInMessage());
				}
			}

			private readonly TransmitRateManager RateManger;

			public int BaudRate { get; private set; }

			public Adapter(string name, int baud_rate)
				: this(name, baud_rate, verbose: false)
			{
			}

			public Adapter(string name, int baud_rate, bool verbose)
				: base(name, verbose)
			{
				BaudRate = baud_rate;
				RateManger = new TransmitRateManager(this);
			}

			public override bool Transmit(T message)
			{
				if (!RateManger.TransmitAllowed)
				{
					return false;
				}
				if (!base.Transmit(message))
				{
					return false;
				}
				RateManger.AddBits(message);
				return true;
			}

			public virtual bool Transmit(ID id, PAYLOAD payload = default(PAYLOAD))
			{
				if (!RateManger.TransmitAllowed)
				{
					return false;
				}
				T @object = ResourcePool<T>.GetObject();
				if (@object == null)
				{
					return false;
				}
				try
				{
					@object.ID = id;
					@object.Payload = payload;
					@object.SetTimeStamp();
					return Transmit(@object);
				}
				finally
				{
					@object?.ReturnToPool();
				}
			}
		}

		[JsonConverter(typeof(IdConverter))]
		public struct ID : IComparable<ID>, IEquatable<ID>
		{
			private class IdConverter : JsonConverter
			{
				public override bool CanConvert(Type objectType)
				{
					return objectType == typeof(ID);
				}

				public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
				{
					JToken jToken = JToken.Load(reader);
					if (jToken.Type == JTokenType.Null)
					{
						return null;
					}
					return new ID(jToken.ToString());
				}

				public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
				{
					if (value is ID iD)
					{
						JToken.FromObject(iD.JsonString).WriteTo(writer);
						return;
					}
					throw new ArgumentException();
				}
			}

			private const uint EXTENDED = 2147483648u;

			private readonly uint mValue;

			private string JsonString
			{
				get
				{
					if (IsExtended)
					{
						return Value.ToString("X8");
					}
					return Value.ToString("X3");
				}
			}

			public uint Value => mValue & 0x1FFFFFFFu;

			public bool IsExtended => mValue > 2047;

			public ID(uint value, bool isExtended)
			{
				if (isExtended)
				{
					mValue = (value & 0x1FFFFFFFu) | 0x80000000u;
				}
				else
				{
					mValue = value & 0x7FFu;
				}
			}

			private ID(string json)
			{
				mValue = uint.Parse(json, NumberStyles.HexNumber);
				if (json.Length == 8)
				{
					mValue |= 2147483648u;
				}
			}

			public override string ToString()
			{
				if (IsExtended)
				{
					return "x" + Value.ToString("X8") + "h";
				}
				return Value.ToString("X3") + "h";
			}

			public int CompareTo(ID other)
			{
				return mValue.CompareTo(other.mValue);
			}

			public bool Equals(ID other)
			{
				return mValue.Equals(other.mValue);
			}

			public static explicit operator uint(ID id)
			{
				return id.Value;
			}
		}

		[JsonConverter(typeof(PayloadConverter))]
		public struct PAYLOAD : Comm.IByteBuffer, Comm.IByteList, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>, IComparable<PAYLOAD>, IEquatable<PAYLOAD>
		{
			private class PayloadConverter : JsonConverter
			{
				public override bool CanConvert(Type objectType)
				{
					return objectType == typeof(PAYLOAD);
				}

				public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
				{
					JToken jToken = JToken.Load(reader);
					if (jToken.Type == JTokenType.Null)
					{
						return null;
					}
					return new PAYLOAD(jToken.ToString());
				}

				public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
				{
					if (value is PAYLOAD pAYLOAD)
					{
						JToken.FromObject(pAYLOAD.JsonString).WriteTo(writer);
						return;
					}
					throw new ArgumentException();
				}
			}

			public const int CAPACITY = 8;

			private int mLength;

			private byte b0;

			private byte b1;

			private byte b2;

			private byte b3;

			private byte b4;

			private byte b5;

			private byte b6;

			private byte b7;

			public int Count => Length;

			public int Capacity
			{
				get
				{
					return 8;
				}
				set
				{
					if (value != 8)
					{
						throw new InvalidOperationException("CAN.PAYLOAD Capacity cannot be set");
					}
				}
			}

			private string JsonString
			{
				get
				{
					string text = "";
					using IEnumerator<byte> enumerator = GetEnumerator();
					while (enumerator.MoveNext())
					{
						byte current = enumerator.Current;
						text += current.HexString();
					}
					return text;
				}
			}

			public int Length
			{
				get
				{
					return mLength;
				}
				set
				{
					if (value < 0 || value > 8)
					{
						throw new ArgumentException("Length");
					}
					mLength = value;
				}
			}

			public byte this[int index]
			{
				get
				{
					return index switch
					{
						0 => b0, 
						1 => b1, 
						2 => b2, 
						3 => b3, 
						4 => b4, 
						5 => b5, 
						6 => b6, 
						7 => b7, 
						_ => throw new ArgumentOutOfRangeException("len"), 
					};
				}
				set
				{
					switch (index)
					{
					default:
						throw new ArgumentOutOfRangeException("len");
					case 0:
						b0 = value;
						break;
					case 1:
						b1 = value;
						break;
					case 2:
						b2 = value;
						break;
					case 3:
						b3 = value;
						break;
					case 4:
						b4 = value;
						break;
					case 5:
						b5 = value;
						break;
					case 6:
						b6 = value;
						break;
					case 7:
						b7 = value;
						break;
					}
				}
			}

			public void Clear()
			{
				Length = 0;
			}

			public PAYLOAD(int length)
			{
				this = default(PAYLOAD);
				Length = length;
			}

			public PAYLOAD(IReadOnlyList<byte> data)
			{
				this = default(PAYLOAD);
				Append(data);
			}

			public PAYLOAD(IReadOnlyList<byte> data, int count)
			{
				this = default(PAYLOAD);
				Append(data, count);
			}

			public PAYLOAD(IReadOnlyList<byte> data, int index, int count)
			{
				this = default(PAYLOAD);
				Append(data, index, count);
			}

			public PAYLOAD(IEnumerable<byte> something)
			{
				this = default(PAYLOAD);
				Append(something);
			}

			public static PAYLOAD FromArgs(params object[] args)
			{
				PAYLOAD result = default(PAYLOAD);
				foreach (object obj in args)
				{
					if (obj is byte value)
					{
						result.Append(value);
						continue;
					}
					if (obj is sbyte value2)
					{
						result.Append(value2);
						continue;
					}
					if (obj is char value3)
					{
						result.Append(value3);
						continue;
					}
					if (obj is ushort value4)
					{
						result.Append(value4);
						continue;
					}
					if (obj is short value5)
					{
						result.Append(value5);
						continue;
					}
					if (obj is UInt24 value6)
					{
						result.Append(value6);
						continue;
					}
					if (obj is Int24 value7)
					{
						result.Append(value7);
						continue;
					}
					if (obj is uint value8)
					{
						result.Append(value8);
						continue;
					}
					if (obj is int value9)
					{
						result.Append(value9);
						continue;
					}
					if (obj is UInt40 value10)
					{
						result.Append(value10);
						continue;
					}
					if (obj is Int40 value11)
					{
						result.Append(value11);
						continue;
					}
					if (obj is UInt48 value12)
					{
						result.Append(value12);
						continue;
					}
					if (obj is Int48 value13)
					{
						result.Append(value13);
						continue;
					}
					if (obj is UInt56 value14)
					{
						result.Append(value14);
						continue;
					}
					if (obj is Int56 value15)
					{
						result.Append(value15);
						continue;
					}
					if (obj is ulong value16)
					{
						result.Append(value16);
						continue;
					}
					if (obj is long value17)
					{
						result.Append(value17);
						continue;
					}
					if (obj is IEnumerable<byte> bytes)
					{
						result.Append(bytes);
						continue;
					}
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(9, 2);
					defaultInterpolatedStringHandler.AppendFormatted<object>(obj);
					defaultInterpolatedStringHandler.AppendLiteral(" is type ");
					defaultInterpolatedStringHandler.AppendFormatted(obj.GetType());
					throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				return result;
			}

			public PAYLOAD(string json)
			{
				if (json.Length < 0 || json.Length > 16 || ((uint)json.Length & (true ? 1u : 0u)) != 0)
				{
					throw new ArgumentException();
				}
				this = default(PAYLOAD);
				if (json.Length > 0)
				{
					ulong num = ulong.Parse(json, NumberStyles.HexNumber);
					Length = json.Length >> 1;
					for (int num2 = Length - 1; num2 >= 0; num2--)
					{
						this[num2] = (byte)num;
						num >>= 8;
					}
				}
			}

			public static bool operator ==(PAYLOAD s1, PAYLOAD s2)
			{
				if (s1.mLength != s2.mLength)
				{
					return false;
				}
				for (int i = 0; i < s1.mLength; i++)
				{
					if (s1[i] != s2[i])
					{
						return false;
					}
				}
				return true;
			}

			public static bool operator !=(PAYLOAD s1, PAYLOAD s2)
			{
				return !(s1 == s2);
			}

			public override bool Equals(object obj)
			{
				if (obj is PAYLOAD pAYLOAD)
				{
					return pAYLOAD == this;
				}
				return false;
			}

			public bool Equals(PAYLOAD other)
			{
				return this == other;
			}

			public override int GetHashCode()
			{
				int num = mLength;
				for (int i = 0; i < mLength; i++)
				{
					num = num * 31 + this[i];
				}
				return num;
			}

			public int CompareTo(PAYLOAD other)
			{
				if (Length != other.Length)
				{
					return Length.CompareTo(other.Length);
				}
				for (int i = 0; i < Length; i++)
				{
					if (this[i] < other[i])
					{
						return -1;
					}
					if (this[i] > other[i])
					{
						return 1;
					}
				}
				return 0;
			}

			public void CopyTo(byte[] array, int index)
			{
				for (int i = 0; i < Length; i++)
				{
					array[index++] = this[i];
				}
			}

			public void CopyRangeTo(int sourceIndex, int count, byte[] array, int destIndex)
			{
				if (sourceIndex + count > Length)
				{
					throw new ArgumentOutOfRangeException();
				}
				for (int i = 0; i < count; i++)
				{
					array[destIndex++] = this[sourceIndex++];
				}
			}

			public IEnumerator<byte> GetEnumerator()
			{
				for (int i = 0; i < Length; i++)
				{
					yield return this[i];
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}

			public void Append(byte value)
			{
				if (Length >= 8)
				{
					throw new InvalidOperationException("Can't append more than 8 bytes to a CAN.PAYLOAD");
				}
				this[Length++] = value;
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
				Append((ushort)(value >> 32));
				Append((uint)value);
			}

			public void Append(UInt48 value)
			{
				Append((ushort)(value >> 32));
				Append((uint)value);
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

			public void Append(IEnumerable<byte> bytes)
			{
				foreach (byte @byte in bytes)
				{
					Append(@byte);
				}
			}

			public void Append(IReadOnlyList<byte> buffer)
			{
				foreach (byte item in buffer)
				{
					Append(item);
				}
			}

			public void Append(IReadOnlyList<byte> buffer, int count)
			{
				for (int i = 0; i < count; i++)
				{
					Append(buffer[i]);
				}
			}

			public void Append(IReadOnlyList<byte> buffer, int index, int count)
			{
				for (int i = 0; i < count; i++)
				{
					Append(buffer[index++]);
				}
			}

			public void Append(Comm.IByteList buffer)
			{
				foreach (byte item in buffer)
				{
					Append(item);
				}
			}

			public void Append(Comm.IByteList buffer, int count)
			{
				for (int i = 0; i < count; i++)
				{
					Append(buffer[i]);
				}
			}

			public void Append(Comm.IByteList buffer, int index, int count)
			{
				for (int i = 0; i < count; i++)
				{
					Append(buffer[index++]);
				}
			}

			public void Append(byte[] buffer)
			{
				Append(buffer, 0, buffer.Length);
			}

			public void Append(byte[] buffer, int count)
			{
				Append(buffer, 0, count);
			}

			public void Append(byte[] buffer, int index, int count)
			{
				for (int i = 0; i < count; i++)
				{
					Append(buffer[index++]);
				}
			}

			public override string ToString()
			{
				return ToString(dataonly: false);
			}

			public string ToString(bool dataonly)
			{
				return Comm.ToString(this, dataonly);
			}
		}

		public struct PACKET
		{
			public ID ID { get; set; }

			public PAYLOAD Payload { get; set; }

			public PACKET(ID id, PAYLOAD payload)
			{
				ID = id;
				Payload = payload;
			}
		}

		public interface IReadOnlyPacket : Comm.ITimeStamp
		{
			ID ID { get; }

			PAYLOAD Payload { get; }
		}

		public interface IPacket : IReadOnlyPacket, Comm.ITimeStamp
		{
			new ID ID { get; set; }

			new PAYLOAD Payload { get; set; }

			new TimeSpan Timestamp { get; set; }
		}

		public interface IMessage : Comm.IMessage, Comm.IByteList, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>, Comm.ITimeStamp, IReadOnlyPacket
		{
		}

		public interface IMessageBuffer : Comm.IMessageBuffer, Comm.IMessage, Comm.IByteList, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>, Comm.ITimeStamp, Comm.IByteBuffer, IMessage, IReadOnlyPacket, IPacket
		{
		}

		public class MessageBuffer : Comm.MessageBuffer, IMessageBuffer, Comm.IMessageBuffer, Comm.IMessage, Comm.IByteList, IReadOnlyList<byte>, IEnumerable<byte>, IEnumerable, IReadOnlyCollection<byte>, Comm.ITimeStamp, Comm.IByteBuffer, IMessage, IReadOnlyPacket, IPacket
		{
			public ID ID { get; set; }

			public PAYLOAD Payload
			{
				get
				{
					return new PAYLOAD(this);
				}
				set
				{
					base.Length = 0;
					for (int i = 0; i < value.Length; i++)
					{
						Append(value[i]);
					}
				}
			}

			public override int Capacity
			{
				get
				{
					return 8;
				}
				set
				{
					if (value != 8)
					{
						throw new InvalidOperationException("CAN.MessageBuffer.Capacity cannot be changed");
					}
				}
			}

			public MessageBuffer()
				: base(8)
			{
			}

			protected override void ResetPoolObjectState()
			{
				ID = default(ID);
				base.ResetPoolObjectState();
			}

			public override void Clear()
			{
				ID = default(ID);
				base.Clear();
			}

			public override void CopyFrom(Comm.IMessage message)
			{
				if (message is IMessage message2)
				{
					CopyFrom(message2);
					return;
				}
				throw new InvalidOperationException("Cannot copy Core.IMessage into CAN.MessageBuffer");
			}

			public virtual void CopyFrom(IMessage message)
			{
				ID = message.ID;
				base.CopyFrom(message);
			}

			public override string ToString()
			{
				return CAN.ToString((IReadOnlyPacket)this);
			}

			public override string ToString(bool dataonly)
			{
				if (dataonly)
				{
					return base.ToString(dataonly: true);
				}
				return ToString();
			}
		}

		public const uint _11_BIT_ID_MASK = 2047u;

		public const uint _29_BIT_ID_MASK = 536870911u;

		public static int EstimateNumberOfBitsInMessage(ID id, int dlc)
		{
			if (!id.IsExtended)
			{
				return 47 + 9 * dlc;
			}
			return 69 + 9 * dlc;
		}

		private static string ToString(ID id, PAYLOAD payload)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(7, 2);
			defaultInterpolatedStringHandler.AppendLiteral("ID = ");
			defaultInterpolatedStringHandler.AppendFormatted(id);
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted(payload.ToString(dataonly: false));
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		private static string ToString(IReadOnlyPacket packet)
		{
			return ToString(packet.ID, packet.Payload);
		}
	}
}
