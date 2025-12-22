using System;
using System.Globalization;
using System.Numerics;

namespace IDS.Core.Types
{
	public struct UInt56 : IComparable<UInt56>, IEquatable<UInt56>, IFormattable
	{
		public static readonly UInt56 Zero = (byte)0;

		public static readonly UInt56 MinValue = Zero;

		public static readonly UInt56 MaxValue = ~MinValue;

		private const ulong MASK = 72057594037927935uL;

		private readonly ulong Value;

		private UInt56(ulong value)
		{
			Value = value & 0xFFFFFFFFFFFFFFuL;
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static UInt56 Parse(string s)
		{
			if (!TryParse(s, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static UInt56 Parse(string s, NumberStyles style)
		{
			return Parse(s, style, NumberFormatInfo.CurrentInfo);
		}

		public static UInt56 Parse(string s, IFormatProvider provider)
		{
			return Parse(s, NumberStyles.Integer, provider);
		}

		public static UInt56 Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			if (!TryParse(s, style, provider, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static bool TryParse(string s, out UInt56 result)
		{
			return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
		}

		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out UInt56 result)
		{
			if (!ulong.TryParse(s, style, provider, out var result2))
			{
				result = Zero;
				return false;
			}
			result = new UInt56(result2);
			return true;
		}

		public int CompareTo(UInt56 other)
		{
			return Value.CompareTo(other.Value);
		}

		public bool Equals(UInt56 other)
		{
			return Value == other.Value;
		}

		public override string ToString()
		{
			return Value.ToString();
		}

		public string ToString(string format)
		{
			return Value.ToString(format);
		}

		public string ToString(IFormatProvider provider)
		{
			return Value.ToString(null, provider);
		}

		public string ToString(string format, IFormatProvider provider)
		{
			return Value.ToString(format, provider);
		}

		public static implicit operator UInt56(byte a)
		{
			return new UInt56(a);
		}

		public static implicit operator UInt56(ushort a)
		{
			return new UInt56(a);
		}

		public static implicit operator UInt56(UInt24 a)
		{
			return new UInt56(a);
		}

		public static implicit operator UInt56(uint a)
		{
			return new UInt56(a);
		}

		public static implicit operator UInt56(UInt40 a)
		{
			return new UInt56(a);
		}

		public static implicit operator UInt56(UInt48 a)
		{
			return new UInt56(a);
		}

		public static explicit operator UInt56(sbyte a)
		{
			return new UInt56((ulong)a);
		}

		public static explicit operator UInt56(short a)
		{
			return new UInt56((ulong)a);
		}

		public static explicit operator UInt56(Int24 a)
		{
			return new UInt56((ulong)a);
		}

		public static explicit operator UInt56(int a)
		{
			return new UInt56((ulong)a);
		}

		public static explicit operator UInt56(Int40 a)
		{
			return new UInt56((ulong)a);
		}

		public static explicit operator UInt56(Int48 a)
		{
			return new UInt56((ulong)a);
		}

		public static explicit operator UInt56(Int56 a)
		{
			return new UInt56((ulong)a);
		}

		public static explicit operator UInt56(long a)
		{
			return new UInt56((ulong)a);
		}

		public static explicit operator UInt56(ulong a)
		{
			return new UInt56(a);
		}

		public static explicit operator UInt56(float a)
		{
			return new UInt56((ulong)a);
		}

		public static explicit operator UInt56(double a)
		{
			return new UInt56((ulong)a);
		}

		public static explicit operator UInt56(decimal a)
		{
			return new UInt56((ulong)a);
		}

		public static explicit operator UInt56(BigInteger a)
		{
			return new UInt56((ulong)a);
		}

		public static implicit operator long(UInt56 a)
		{
			return (long)a.Value;
		}

		public static implicit operator ulong(UInt56 a)
		{
			return a.Value;
		}

		public static implicit operator UInt128(UInt56 a)
		{
			return a.Value;
		}

		public static implicit operator float(UInt56 a)
		{
			return a.Value;
		}

		public static implicit operator double(UInt56 a)
		{
			return a.Value;
		}

		public static implicit operator decimal(UInt56 a)
		{
			return a.Value;
		}

		public static implicit operator BigInteger(UInt56 a)
		{
			return a.Value;
		}

		public static explicit operator sbyte(UInt56 a)
		{
			return (sbyte)a.Value;
		}

		public static explicit operator short(UInt56 a)
		{
			return (short)a.Value;
		}

		public static explicit operator Int24(UInt56 a)
		{
			return (Int24)a.Value;
		}

		public static explicit operator int(UInt56 a)
		{
			return (int)a.Value;
		}

		public static explicit operator Int40(UInt56 a)
		{
			return (Int40)a.Value;
		}

		public static explicit operator Int48(UInt56 a)
		{
			return (Int48)a.Value;
		}

		public static explicit operator Int56(UInt56 a)
		{
			return (Int56)a.Value;
		}

		public static explicit operator byte(UInt56 a)
		{
			return (byte)a.Value;
		}

		public static explicit operator ushort(UInt56 a)
		{
			return (ushort)a.Value;
		}

		public static explicit operator UInt24(UInt56 a)
		{
			return (UInt24)a.Value;
		}

		public static explicit operator uint(UInt56 a)
		{
			return (uint)a.Value;
		}

		public static explicit operator UInt40(UInt56 a)
		{
			return (UInt40)a.Value;
		}

		public static explicit operator UInt48(UInt56 a)
		{
			return (UInt48)a.Value;
		}

		public static UInt56 operator <<(UInt56 a, int b)
		{
			return new UInt56(a.Value << b);
		}

		public static UInt56 operator >>(UInt56 a, int b)
		{
			return new UInt56(a.Value >> b);
		}

		public static UInt56 operator &(UInt56 a, UInt56 b)
		{
			return new UInt56(a.Value & b.Value);
		}

		public static UInt56 operator |(UInt56 a, UInt56 b)
		{
			return new UInt56(a.Value | b.Value);
		}

		public static UInt56 operator ^(UInt56 a, UInt56 b)
		{
			return new UInt56(a.Value ^ b.Value);
		}

		public static UInt56 operator ~(UInt56 a)
		{
			return new UInt56(~a.Value);
		}

		public static UInt56 operator +(UInt56 a)
		{
			return a;
		}

		public static UInt56 operator -(UInt56 a)
		{
			return new UInt56(0L - a.Value);
		}

		public static UInt56 operator +(UInt56 a, UInt56 b)
		{
			return new UInt56(a.Value + b.Value);
		}

		public static UInt56 operator ++(UInt56 a)
		{
			return new UInt56(a.Value + 1);
		}

		public static UInt56 operator -(UInt56 a, UInt56 b)
		{
			return new UInt56(a.Value - b.Value);
		}

		public static UInt56 operator --(UInt56 a)
		{
			return new UInt56(a.Value - 1);
		}

		public static UInt56 operator *(UInt56 a, UInt56 b)
		{
			return new UInt56(a.Value * b.Value);
		}

		public static UInt56 operator /(UInt56 a, UInt56 b)
		{
			return new UInt56(a.Value / b.Value);
		}

		public static UInt56 operator %(UInt56 a, UInt56 b)
		{
			return new UInt56(a.Value % b.Value);
		}
	}
}
