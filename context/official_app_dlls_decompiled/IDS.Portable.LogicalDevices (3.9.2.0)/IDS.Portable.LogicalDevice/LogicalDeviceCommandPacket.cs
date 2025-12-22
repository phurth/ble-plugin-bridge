using System;
using System.Collections.Generic;
using System.Linq;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceCommandPacket : IDeviceCommandPacket, IDeviceDataPacket, IEquatable<LogicalDeviceCommandPacket>
	{
		public const int DefaultCommandResponseTimeMs = 200;

		private readonly int _hashCode;

		public bool HasData => true;

		protected byte[] Data { get; private set; }

		public IReadOnlyList<byte> RawData => Data;

		public uint MaxSize => (uint)Data.Length;

		public uint MinSize => (uint)Data.Length;

		public uint Size => (uint)Data.Length;

		public int CommandResponseTimeMs { get; }

		public byte CommandByte { get; }

		public static void SetBit(ref byte data, BasicBitMask bit, bool value)
		{
			if (value)
			{
				data |= (byte)bit;
			}
			else
			{
				data &= (byte)(~(int)bit);
			}
		}

		public bool GetBit(BasicBitMask bit)
		{
			return ((uint)Data[0] & (uint)bit) != 0;
		}

		public ushort MsbUInt16(uint startOffset)
		{
			return (ushort)((ushort)(Data[startOffset] << 8) | Data[1 + startOffset]);
		}

		public uint MsbUInt24(uint startOffset)
		{
			return (uint)((Data[startOffset] << 16) | (Data[1 + startOffset] << 8) | Data[2 + startOffset]);
		}

		public LogicalDeviceCommandPacket(byte commandByte, IReadOnlyList<byte> data, int commandResponseTimeMs = 200)
		{
			CommandResponseTimeMs = commandResponseTimeMs;
			CommandByte = commandByte;
			Data = Enumerable.ToArray(data);
			_hashCode = 17.Hash(Data).Hash(CommandByte);
		}

		public LogicalDeviceCommandPacket(byte commandByte, byte[] data, int commandResponseTimeMs = 200)
			: this(commandByte, (IReadOnlyList<byte>)data, commandResponseTimeMs)
		{
		}

		public LogicalDeviceCommandPacket(byte commandByte, byte data, int commandResponseTimeMs = 200)
		{
			CommandResponseTimeMs = commandResponseTimeMs;
			CommandByte = commandByte;
			Data = new byte[1];
			Data[0] = data;
			_hashCode = 17.Hash(Data).Hash(CommandByte);
		}

		public LogicalDeviceCommandPacket(byte commandByte, byte byte0, byte byte1, int commandResponseTimeMs = 200)
		{
			CommandResponseTimeMs = commandResponseTimeMs;
			CommandByte = commandByte;
			Data = new byte[2];
			Data[0] = byte0;
			Data[1] = byte1;
			_hashCode = 17.Hash(Data).Hash(CommandByte);
		}

		public LogicalDeviceCommandPacket(byte commandByte, byte byte0, byte byte1, byte byte2, int commandResponseTimeMs = 200)
		{
			CommandResponseTimeMs = commandResponseTimeMs;
			CommandByte = commandByte;
			Data = new byte[3];
			Data[0] = byte0;
			Data[1] = byte1;
			Data[2] = byte2;
			_hashCode = 17.Hash(Data).Hash(CommandByte);
		}

		public LogicalDeviceCommandPacket(byte commandByte, ushort msbValue, int commandResponseTimeMs = 200)
		{
			CommandResponseTimeMs = commandResponseTimeMs;
			CommandByte = commandByte;
			Data = new byte[2];
			Data[0] = (byte)((msbValue & 0xFF00) >> 8);
			Data[1] = (byte)(msbValue & 0xFFu);
			_hashCode = 17.Hash(Data).Hash(CommandByte);
		}

		public LogicalDeviceCommandPacket(byte commandByte, int commandResponseTimeMs = 200)
		{
			CommandResponseTimeMs = commandResponseTimeMs;
			CommandByte = commandByte;
			Data = new byte[0];
			_hashCode = 17.Hash(Data).Hash(CommandByte);
		}

		public byte[] CopyCurrentData()
		{
			uint num = Size;
			if (num > Data.Length)
			{
				num = (uint)Data.Length;
			}
			byte[] array = new byte[num];
			if (num != 0)
			{
				Buffer.BlockCopy(Data, 0, array, 0, (int)num);
			}
			return array;
		}

		public bool Equals(LogicalDeviceCommandPacket other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			if (Size != other.Size)
			{
				return false;
			}
			if (CommandByte == other.CommandByte && CommandResponseTimeMs == other.CommandResponseTimeMs)
			{
				return ArrayCommon.ArraysEqual(Data, other.Data, (int)Size);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (this == obj)
			{
				return true;
			}
			if (obj is LogicalDeviceCommandPacket other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}
	}
}
