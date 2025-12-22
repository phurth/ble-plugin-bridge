using System;
using System.Globalization;
using System.Numerics;

namespace IDS.Core.Types
{
	public struct UInt40 : IComparable<UInt40>, IEquatable<UInt40>, IFormattable
	{
		public static readonly UInt40 Zero = (byte)0;

		public static readonly UInt40 MinValue = Zero;

		public static readonly UInt40 MaxValue = ~MinValue;

		private const ulong MASK = 281474976710655uL;

		private readonly ulong Value;

		private UInt40(ulong value)
		{
			Value = value & 0xFFFFFFFFFFFFuL;
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static UInt40 Parse(string s)
		{
			if (!TryParse(s, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static UInt40 Parse(string s, NumberStyles style)
		{
			return Parse(s, style, NumberFormatInfo.CurrentInfo);
		}

		public static UInt40 Parse(string s, IFormatProvider provider)
		{
			return Parse(s, NumberStyles.Integer, provider);
		}

		public static UInt40 Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			if (!TryParse(s, style, provider, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static bool TryParse(string s, out UInt40 result)
		{
			return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
		}

		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out UInt40 result)
		{
			if (!ulong.TryParse(s, style, provider, out var result2))
			{
				result = Zero;
				return false;
			}
			result = new UInt40(result2);
			return true;
		}

		public int CompareTo(UInt40 other)
		{
			return Value.CompareTo(other.Value);
		}

		public bool Equals(UInt40 other)
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

		public static implicit operator UInt40(byte a)
		{
			return new UInt40(a);
		}

		public static implicit operator UInt40(ushort a)
		{
			return new UInt40(a);
		}

		public static implicit operator UInt40(UInt24 a)
		{
			return new UInt40(a);
		}

		public static implicit operator UInt40(uint a)
		{
			return new UInt40(a);
		}

		public static explicit operator UInt40(sbyte a)
		{
			return new UInt40((ulong)a);
		}

		public static explicit operator UInt40(short a)
		{
			return new UInt40((ulong)a);
		}

		public static explicit operator UInt40(Int24 a)
		{
			return new UInt40((ulong)a);
		}

		public static explicit operator UInt40(int a)
		{
			return new UInt40((ulong)a);
		}

		public static explicit operator UInt40(Int40 a)
		{
			return new UInt40((ulong)a);
		}

		public static explicit operator UInt40(Int48 a)
		{
			return new UInt40((ulong)a);
		}

		public static explicit operator UInt40(Int56 a)
		{
			return new UInt40((ulong)a);
		}

		public static explicit operator UInt40(long a)
		{
			return new UInt40((ulong)a);
		}

		public static explicit operator UInt40(UInt48 a)
		{
			return new UInt40(a);
		}

		public static explicit operator UInt40(UInt56 a)
		{
			return new UInt40(a);
		}

		public static explicit operator UInt40(ulong a)
		{
			return new UInt40(a);
		}

		public static explicit operator UInt40(float a)
		{
			return new UInt40((ulong)a);
		}

		public static explicit operator UInt40(double a)
		{
			return new UInt40((ulong)a);
		}

		public static explicit operator UInt40(decimal a)
		{
			return new UInt40((ulong)a);
		}

		public static explicit operator UInt40(BigInteger a)
		{
			return new UInt40((ulong)a);
		}

		public static implicit operator Int48(UInt40 a)
		{
			return (Int48)a.Value;
		}

		public static implicit operator UInt48(UInt40 a)
		{
			return (UInt48)a.Value;
		}

		public static implicit operator Int56(UInt40 a)
		{
			return (Int56)a.Value;
		}

		public static implicit operator UInt56(UInt40 a)
		{
			return (UInt56)a.Value;
		}

		public static implicit operator long(UInt40 a)
		{
			return (long)a.Value;
		}

		public static implicit operator ulong(UInt40 a)
		{
			return a.Value;
		}

		public static implicit operator UInt128(UInt40 a)
		{
			return a.Value;
		}

		public static implicit operator float(UInt40 a)
		{
			return a.Value;
		}

		public static implicit operator double(UInt40 a)
		{
			return a.Value;
		}

		public static implicit operator decimal(UInt40 a)
		{
			return a.Value;
		}

		public static implicit operator BigInteger(UInt40 a)
		{
			return a.Value;
		}

		public static explicit operator sbyte(UInt40 a)
		{
			return (sbyte)a.Value;
		}

		public static explicit operator short(UInt40 a)
		{
			return (short)a.Value;
		}

		public static explicit operator Int24(UInt40 a)
		{
			return (Int24)a.Value;
		}

		public static explicit operator int(UInt40 a)
		{
			return (int)a.Value;
		}

		public static explicit operator Int40(UInt40 a)
		{
			return (Int40)a.Value;
		}

		public static explicit operator byte(UInt40 a)
		{
			return (byte)a.Value;
		}

		public static explicit operator ushort(UInt40 a)
		{
			return (ushort)a.Value;
		}

		public static explicit operator UInt24(UInt40 a)
		{
			return (UInt24)a.Value;
		}

		public static explicit operator uint(UInt40 a)
		{
			return (uint)a.Value;
		}

		public static UInt40 operator <<(UInt40 a, int b)
		{
			return new UInt40(a.Value << b);
		}

		public static UInt40 operator >>(UInt40 a, int b)
		{
			return new UInt40(a.Value >> b);
		}

		public static UInt40 operator &(UInt40 a, UInt40 b)
		{
			return new UInt40(a.Value & b.Value);
		}

		public static UInt40 operator |(UInt40 a, UInt40 b)
		{
			return new UInt40(a.Value | b.Value);
		}

		public static UInt40 operator ^(UInt40 a, UInt40 b)
		{
			return new UInt40(a.Value ^ b.Value);
		}

		public static UInt40 operator ~(UInt40 a)
		{
			return new UInt40(~a.Value);
		}

		public static UInt40 operator +(UInt40 a)
		{
			return a;
		}

		public static UInt40 operator -(UInt40 a)
		{
			return new UInt40(0L - a.Value);
		}

		public static UInt40 operator +(UInt40 a, UInt40 b)
		{
			return new UInt40(a.Value + b.Value);
		}

		public static UInt40 operator ++(UInt40 a)
		{
			return new UInt40(a.Value + 1);
		}

		public static UInt40 operator -(UInt40 a, UInt40 b)
		{
			return new UInt40(a.Value - b.Value);
		}

		public static UInt40 operator --(UInt40 a)
		{
			return new UInt40(a.Value - 1);
		}

		public static UInt40 operator *(UInt40 a, UInt40 b)
		{
			return new UInt40(a.Value * b.Value);
		}

		public static UInt40 operator /(UInt40 a, UInt40 b)
		{
			return new UInt40(a.Value / b.Value);
		}

		public static UInt40 operator %(UInt40 a, UInt40 b)
		{
			return new UInt40(a.Value % b.Value);
		}
	}
}
