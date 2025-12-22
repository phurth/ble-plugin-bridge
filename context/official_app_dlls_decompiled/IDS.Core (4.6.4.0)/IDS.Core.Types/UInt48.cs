using System;
using System.Globalization;
using System.Numerics;

namespace IDS.Core.Types
{
	public struct UInt48 : IComparable<UInt48>, IEquatable<UInt48>, IFormattable
	{
		public static readonly UInt48 Zero = (byte)0;

		public static readonly UInt48 MinValue = Zero;

		public static readonly UInt48 MaxValue = ~MinValue;

		private const ulong MASK = 281474976710655uL;

		private readonly ulong Value;

		private UInt48(ulong value)
		{
			Value = value & 0xFFFFFFFFFFFFuL;
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static UInt48 Parse(string s)
		{
			if (!TryParse(s, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static UInt48 Parse(string s, NumberStyles style)
		{
			return Parse(s, style, NumberFormatInfo.CurrentInfo);
		}

		public static UInt48 Parse(string s, IFormatProvider provider)
		{
			return Parse(s, NumberStyles.Integer, provider);
		}

		public static UInt48 Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			if (!TryParse(s, style, provider, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static bool TryParse(string s, out UInt48 result)
		{
			return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
		}

		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out UInt48 result)
		{
			if (!ulong.TryParse(s, style, provider, out var result2))
			{
				result = Zero;
				return false;
			}
			result = new UInt48(result2);
			return true;
		}

		public int CompareTo(UInt48 other)
		{
			return Value.CompareTo(other.Value);
		}

		public bool Equals(UInt48 other)
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

		public static implicit operator UInt48(byte a)
		{
			return new UInt48(a);
		}

		public static implicit operator UInt48(ushort a)
		{
			return new UInt48(a);
		}

		public static implicit operator UInt48(UInt24 a)
		{
			return new UInt48(a);
		}

		public static implicit operator UInt48(uint a)
		{
			return new UInt48(a);
		}

		public static implicit operator UInt48(UInt40 a)
		{
			return new UInt48(a);
		}

		public static explicit operator UInt48(sbyte a)
		{
			return new UInt48((ulong)a);
		}

		public static explicit operator UInt48(short a)
		{
			return new UInt48((ulong)a);
		}

		public static explicit operator UInt48(Int24 a)
		{
			return new UInt48((ulong)a);
		}

		public static explicit operator UInt48(int a)
		{
			return new UInt48((ulong)a);
		}

		public static explicit operator UInt48(Int40 a)
		{
			return new UInt48((ulong)a);
		}

		public static explicit operator UInt48(Int48 a)
		{
			return new UInt48((ulong)a);
		}

		public static explicit operator UInt48(Int56 a)
		{
			return new UInt48((ulong)a);
		}

		public static explicit operator UInt48(UInt56 a)
		{
			return new UInt48(a);
		}

		public static explicit operator UInt48(long a)
		{
			return new UInt48((ulong)a);
		}

		public static explicit operator UInt48(ulong a)
		{
			return new UInt48(a);
		}

		public static explicit operator UInt48(float a)
		{
			return new UInt48((ulong)a);
		}

		public static explicit operator UInt48(double a)
		{
			return new UInt48((ulong)a);
		}

		public static explicit operator UInt48(decimal a)
		{
			return new UInt48((ulong)a);
		}

		public static explicit operator UInt48(BigInteger a)
		{
			return new UInt48((ulong)a);
		}

		public static implicit operator Int56(UInt48 a)
		{
			return (Int56)a.Value;
		}

		public static implicit operator UInt56(UInt48 a)
		{
			return (UInt56)a.Value;
		}

		public static implicit operator long(UInt48 a)
		{
			return (long)a.Value;
		}

		public static implicit operator ulong(UInt48 a)
		{
			return a.Value;
		}

		public static implicit operator UInt128(UInt48 a)
		{
			return a.Value;
		}

		public static implicit operator float(UInt48 a)
		{
			return a.Value;
		}

		public static implicit operator double(UInt48 a)
		{
			return a.Value;
		}

		public static implicit operator decimal(UInt48 a)
		{
			return a.Value;
		}

		public static implicit operator BigInteger(UInt48 a)
		{
			return a.Value;
		}

		public static explicit operator sbyte(UInt48 a)
		{
			return (sbyte)a.Value;
		}

		public static explicit operator short(UInt48 a)
		{
			return (short)a.Value;
		}

		public static explicit operator Int24(UInt48 a)
		{
			return (Int24)a.Value;
		}

		public static explicit operator int(UInt48 a)
		{
			return (int)a.Value;
		}

		public static explicit operator Int40(UInt48 a)
		{
			return (Int40)a.Value;
		}

		public static explicit operator Int48(UInt48 a)
		{
			return (Int48)a.Value;
		}

		public static explicit operator byte(UInt48 a)
		{
			return (byte)a.Value;
		}

		public static explicit operator ushort(UInt48 a)
		{
			return (ushort)a.Value;
		}

		public static explicit operator UInt24(UInt48 a)
		{
			return (UInt24)a.Value;
		}

		public static explicit operator uint(UInt48 a)
		{
			return (uint)a.Value;
		}

		public static explicit operator UInt40(UInt48 a)
		{
			return (UInt40)a.Value;
		}

		public static UInt48 operator <<(UInt48 a, int b)
		{
			return new UInt48(a.Value << b);
		}

		public static UInt48 operator >>(UInt48 a, int b)
		{
			return new UInt48(a.Value >> b);
		}

		public static UInt48 operator &(UInt48 a, UInt48 b)
		{
			return new UInt48(a.Value & b.Value);
		}

		public static UInt48 operator |(UInt48 a, UInt48 b)
		{
			return new UInt48(a.Value | b.Value);
		}

		public static UInt48 operator ^(UInt48 a, UInt48 b)
		{
			return new UInt48(a.Value ^ b.Value);
		}

		public static UInt48 operator ~(UInt48 a)
		{
			return new UInt48(~a.Value);
		}

		public static UInt48 operator +(UInt48 a)
		{
			return a;
		}

		public static UInt48 operator -(UInt48 a)
		{
			return new UInt48(0L - a.Value);
		}

		public static UInt48 operator +(UInt48 a, UInt48 b)
		{
			return new UInt48(a.Value + b.Value);
		}

		public static UInt48 operator ++(UInt48 a)
		{
			return new UInt48(a.Value + 1);
		}

		public static UInt48 operator -(UInt48 a, UInt48 b)
		{
			return new UInt48(a.Value - b.Value);
		}

		public static UInt48 operator --(UInt48 a)
		{
			return new UInt48(a.Value - 1);
		}

		public static UInt48 operator *(UInt48 a, UInt48 b)
		{
			return new UInt48(a.Value * b.Value);
		}

		public static UInt48 operator /(UInt48 a, UInt48 b)
		{
			return new UInt48(a.Value / b.Value);
		}

		public static UInt48 operator %(UInt48 a, UInt48 b)
		{
			return new UInt48(a.Value % b.Value);
		}
	}
}
