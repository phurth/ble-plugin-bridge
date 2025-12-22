using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceDataPacketMutableDoubleBuffer : CommonNotifyPropertyChanged, IDeviceDataPacketMutable, IDeviceDataPacket
	{
		private readonly object _lock = new object();

		private uint _dataBufferCurrentIndex;

		private readonly byte[][] _dataBuffer;

		public const float FixedPointBy8ScaleFactor = 256f;

		public const int BufferSizeDifferent = -1;

		private const int NoBytesDifferent = -1;

		public bool HasData { get; protected set; }

		public uint MaxSize => (uint)Data.Length;

		public uint MinSize { get; private set; }

		public uint Size { get; private set; }

		public byte[] Data => _dataBuffer[_dataBufferCurrentIndex];

		public bool Bit7
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X80);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X80, value);
			}
		}

		public bool Bit6
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X40);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X40, value);
			}
		}

		public bool Bit5
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X20);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X20, value);
			}
		}

		public bool Bit4
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X10);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X10, value);
			}
		}

		public bool Bit3
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X08);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X08, value);
			}
		}

		public bool Bit2
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X04);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X04, value);
			}
		}

		public bool Bit1
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X02);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X02, value);
			}
		}

		public bool Bit0
		{
			get
			{
				return GetBit(BasicBitMask.BitMask0X01);
			}
			set
			{
				SetBit(BasicBitMask.BitMask0X01, value);
			}
		}

		public bool GetBit(BasicBitMask bit)
		{
			return GetBit(bit, 0);
		}

		public bool GetBit(BasicBitMask bit, int index)
		{
			return ((uint)_dataBuffer[_dataBufferCurrentIndex][index] & (uint)bit) != 0;
		}

		public byte GetByte(byte bitMask, int index)
		{
			return (byte)(_dataBuffer[_dataBufferCurrentIndex][index] & bitMask);
		}

		public byte GetByteValue(int numBits, int startBitIndex, int index)
		{
			uint num = ~(uint.MaxValue >> numBits << numBits) << startBitIndex;
			return (byte)((byte)(_dataBuffer[_dataBufferCurrentIndex][index] & num) >> startBitIndex);
		}

		public uint GetValue(BitPositionValue bitPosition)
		{
			return bitPosition.DecodeValueFromBuffer(_dataBuffer[_dataBufferCurrentIndex]);
		}

		public ushort GetUInt16(uint startOffset)
		{
			return (ushort)((ushort)(_dataBuffer[_dataBufferCurrentIndex][startOffset] << 8) | _dataBuffer[_dataBufferCurrentIndex][1 + startOffset]);
		}

		public short GetInt16(uint startOffset)
		{
			return (short)((short)(_dataBuffer[_dataBufferCurrentIndex][startOffset] << 8) | _dataBuffer[_dataBufferCurrentIndex][1 + startOffset]);
		}

		public UInt24 GetUInt24(uint startOffset)
		{
			_ = (UInt24)(byte)0;
			return (UInt24)(_dataBuffer[_dataBufferCurrentIndex][startOffset] << 16) | (UInt24)(_dataBuffer[_dataBufferCurrentIndex][1 + startOffset] << 8) | _dataBuffer[_dataBufferCurrentIndex][2 + startOffset];
		}

		public static UInt24 GetUInt24(byte[] buffer, uint startOffset)
		{
			_ = (UInt24)(byte)0;
			return (UInt24)(buffer[startOffset] << 16) | (UInt24)(buffer[1 + startOffset] << 8) | buffer[2 + startOffset];
		}

		public uint GetUInt32(uint startOffset)
		{
			return (uint)((_dataBuffer[_dataBufferCurrentIndex][startOffset] << 24) | (_dataBuffer[_dataBufferCurrentIndex][1 + startOffset] << 16) | (_dataBuffer[_dataBufferCurrentIndex][2 + startOffset] << 8) | _dataBuffer[_dataBufferCurrentIndex][3 + startOffset]);
		}

		public void SetBit(BasicBitMask bit, bool value)
		{
			SetBit(bit, value, 0);
		}

		public void SetBit(BasicBitMask bit, bool value, int index)
		{
			HasData = true;
			if (value)
			{
				_dataBuffer[_dataBufferCurrentIndex][index] |= (byte)bit;
			}
			else
			{
				_dataBuffer[_dataBufferCurrentIndex][index] &= (byte)(~(int)bit);
			}
		}

		public void ToggleBit(int byteIndex, int bitIndex)
		{
			_dataBuffer[_dataBufferCurrentIndex][byteIndex] ^= (byte)(1 << bitIndex);
		}

		public void SetByte(byte bitMask, byte value, int index)
		{
			HasData = true;
			_dataBuffer[_dataBufferCurrentIndex][index] &= (byte)(~bitMask);
			_dataBuffer[_dataBufferCurrentIndex][index] |= (byte)(value & bitMask);
		}

		public void SetByteValue(int numBits, int startBitIndex, byte value, int index)
		{
			uint num = (uint)(value << startBitIndex);
			uint num2 = ~(uint.MaxValue >> numBits << numBits) << startBitIndex;
			SetByte((byte)num2, (byte)num, index);
		}

		public void SetValue(uint value, BitPositionValue bitPosition)
		{
			bitPosition.EncodeValueToBuffer(value, _dataBuffer[_dataBufferCurrentIndex]);
		}

		public float GetFixedPoint(FixedPointType fixedPoint, uint startOffset)
		{
			return fixedPoint.GetFixedPointFloat(_dataBuffer[_dataBufferCurrentIndex], startOffset);
		}

		public void SetFixedPoint(FixedPointType fixedPoint, float value, uint startOffset)
		{
			HasData = true;
			fixedPoint.SetFixedPointFloat(_dataBuffer[_dataBufferCurrentIndex], startOffset, value);
		}

		public float GetFixedPointSigned16X8(uint startOffset)
		{
			uint num = 0u;
			num = (uint)(_dataBuffer[_dataBufferCurrentIndex][startOffset] << 16);
			num |= (uint)(_dataBuffer[_dataBufferCurrentIndex][1 + startOffset] << 8);
			num |= _dataBuffer[_dataBufferCurrentIndex][2 + startOffset];
			if ((num & 0x800000u) != 0)
			{
				num |= 0xFF000000u;
			}
			return (float)(int)num / 256f;
		}

		public void SetFixedPointSigned16X8(float value, uint startOffset)
		{
			int num = Convert.ToInt32(value * 256f);
			_dataBuffer[_dataBufferCurrentIndex][startOffset] = Convert.ToByte((0xFF0000 & num) >> 16);
			_dataBuffer[_dataBufferCurrentIndex][1 + startOffset] = Convert.ToByte((0xFF00 & num) >> 8);
			_dataBuffer[_dataBufferCurrentIndex][2 + startOffset] = Convert.ToByte(0xFF & num);
		}

		public void SetUInt16(ushort value, int startOffset)
		{
			_dataBuffer[_dataBufferCurrentIndex][startOffset] = Convert.ToByte((0xFF00 & value) >> 8);
			_dataBuffer[_dataBufferCurrentIndex][1 + startOffset] = Convert.ToByte(0xFF & value);
		}

		public void SetInt16(short value, int startOffset)
		{
			_dataBuffer[_dataBufferCurrentIndex][startOffset] = Convert.ToByte((0xFF00 & value) >> 8);
			_dataBuffer[_dataBufferCurrentIndex][1 + startOffset] = Convert.ToByte(0xFF & value);
		}

		public void SetUInt24(uint value, int startOffset)
		{
			_dataBuffer[_dataBufferCurrentIndex][startOffset] = Convert.ToByte((0xFF0000 & value) >> 16);
			_dataBuffer[_dataBufferCurrentIndex][1 + startOffset] = Convert.ToByte((0xFF00 & value) >> 8);
			_dataBuffer[_dataBufferCurrentIndex][2 + startOffset] = Convert.ToByte(0xFFu & value);
		}

		public void SetUInt32(uint value, int startOffset)
		{
			_dataBuffer[_dataBufferCurrentIndex][startOffset] = Convert.ToByte((0xFF000000u & value) >> 24);
			_dataBuffer[_dataBufferCurrentIndex][1 + startOffset] = Convert.ToByte((0xFF0000 & value) >> 16);
			_dataBuffer[_dataBufferCurrentIndex][2 + startOffset] = Convert.ToByte((0xFF00 & value) >> 8);
			_dataBuffer[_dataBufferCurrentIndex][3 + startOffset] = Convert.ToByte(0xFFu & value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte[] CopyCurrentData()
		{
			if (!HasData)
			{
				return new byte[0];
			}
			byte[] array = new byte[Size];
			if (Size != 0)
			{
				CopyData(array, 0, (int)Size);
			}
			return array;
		}

		public int CopyData(byte[] destinationBuff, int destinationOffset, int maxSize)
		{
			if (maxSize <= 0)
			{
				return 0;
			}
			lock (_lock)
			{
				int num = Math.Min(Data.Length, maxSize);
				if (num < 20)
				{
					for (int i = 0; i < num; i++)
					{
						destinationBuff[i + destinationOffset] = Data[i];
					}
				}
				else
				{
					Buffer.BlockCopy(Data, 0, destinationBuff, destinationOffset, num);
				}
				return num;
			}
		}

		public bool EqualsData(LogicalDeviceDataPacketMutableDoubleBuffer other)
		{
			lock (_lock)
			{
				if (!HasData && !other.HasData)
				{
					return true;
				}
				if (!HasData || !other.HasData)
				{
					return false;
				}
				if (Size != other.Size || Size > other.MaxSize)
				{
					return false;
				}
				byte[] data = other.Data;
				for (int i = 0; i < Size; i++)
				{
					if (Data[i] != data[i])
					{
						return false;
					}
				}
				return true;
			}
		}

		public int Update(byte[] inData, int length, bool forceUpdate = false)
		{
			return Update((IReadOnlyList<byte>)inData, length, forceUpdate);
		}

		public virtual int Update(IReadOnlyList<byte> inData, int length, bool forceUpdate = false)
		{
			int num = -1;
			int num2 = 0;
			if (inData.Count < length)
			{
				throw new LogicalDeviceDataPacketException($"Data Update not possible because inData size {inData.Count} is less then what was specified as the length {length}.");
			}
			if (length > MaxSize)
			{
				throw new LogicalDeviceDataPacketException($"Data Update size {length} is greater then the max size of {MaxSize}.");
			}
			if (length < MinSize)
			{
				throw new LogicalDeviceDataPacketException($"Data Update size {length} is less then minimum size of {MinSize}.");
			}
			lock (this)
			{
				uint num3 = (_dataBufferCurrentIndex + 1) & 1u;
				byte[] array = _dataBuffer[_dataBufferCurrentIndex];
				byte[] array2 = _dataBuffer[num3];
				for (int i = 0; i < length; i++)
				{
					if (array[i] != inData[i] && num == -1)
					{
						num = i + 1;
					}
					array2[i] = inData[i];
				}
				num2 = ((Size != length) ? (-1) : ((num != -1) ? (num - 1) : length));
				if (num2 != length)
				{
					_dataBufferCurrentIndex = num3;
					Size = (uint)length;
				}
			}
			if (num2 != length || forceUpdate || !HasData)
			{
				DidUpdateData();
			}
			return num2;
		}

		public virtual void DidUpdateData()
		{
			if (!HasData)
			{
				HasData = true;
				NotifyPropertyChanged("HasData");
			}
			NotifyPropertyChanged("Data");
		}

		public LogicalDeviceDataPacketMutableDoubleBuffer(uint minSize, uint maxSize, byte bufferFill = 0)
		{
			_dataBufferCurrentIndex = 0u;
			_dataBuffer = new byte[2][];
			_dataBuffer[0] = new byte[maxSize];
			_dataBuffer[1] = new byte[maxSize];
			if (bufferFill != 0 && maxSize != 0)
			{
				FillArray(_dataBuffer[0], bufferFill);
				FillArray(_dataBuffer[1], bufferFill);
			}
			MinSize = minSize;
			Size = minSize;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void FillArray(byte[] array, byte value)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = value;
			}
		}

		public override string ToString()
		{
			if (!HasData)
			{
				return "none";
			}
			return BitConverter.ToString(CopyCurrentData()).Replace("-", " ") ?? "";
		}
	}
}
