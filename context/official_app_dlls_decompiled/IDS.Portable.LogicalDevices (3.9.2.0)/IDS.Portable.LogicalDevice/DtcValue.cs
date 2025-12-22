using System;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public readonly struct DtcValue : IDtcValue, IEquatable<DtcValue>
	{
		private const byte RawValueIsActiveBitMask = 128;

		private const byte RawValueCountBitMask = 127;

		public bool IsActive { get; }

		public bool IsStored => PowerCyclesCounter != 0;

		public byte PowerCyclesCounter { get; }

		public DtcValue(bool isActive, byte powerCyclesCount)
		{
			bool flag = isActive;
			byte b = powerCyclesCount;
			IsActive = flag;
			PowerCyclesCounter = b;
		}

		public DtcValue(byte rawDtcValue)
		{
			bool flag = (rawDtcValue & 0x80) != 0;
			byte b = (byte)(rawDtcValue & 0x7Fu);
			IsActive = flag;
			PowerCyclesCounter = b;
		}

		public byte ToRawValue()
		{
			if (!IsActive)
			{
				return PowerCyclesCounter;
			}
			return (byte)(PowerCyclesCounter | 0x80u);
		}

		public bool Equals(DtcValue other)
		{
			if (IsActive == other.IsActive)
			{
				return PowerCyclesCounter == other.PowerCyclesCounter;
			}
			return false;
		}

		public override bool Equals(object? obj)
		{
			if (obj is DtcValue other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return 17.Hash(IsActive).Hash(PowerCyclesCounter);
		}

		public static bool operator ==(DtcValue left, DtcValue right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(DtcValue left, DtcValue right)
		{
			return !left.Equals(right);
		}
	}
}
