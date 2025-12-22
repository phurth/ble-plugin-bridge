using System;
using System.Globalization;
using System.Numerics;

namespace IDS.Core.Types
{
	public struct UInt24 : IComparable<UInt24>, IEquatable<UInt24>, IFormattable
	{
		public static readonly UInt24 Zero = (byte)0;

		public static readonly UInt24 MinValue = Zero;

		public static readonly UInt24 MaxValue = ~MinValue;

		private const uint MASK = 16777215u;

		private readonly uint Value;

		private UInt24(uint value)
		{
			Value = value & 0xFFFFFFu;
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static UInt24 Parse(string s)
		{
			if (!TryParse(s, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static UInt24 Parse(string s, NumberStyles style)
		{
			return Parse(s, style, NumberFormatInfo.CurrentInfo);
		}

		public static UInt24 Parse(string s, IFormatProvider provider)
		{
			return Parse(s, NumberStyles.Integer, provider);
		}

		public static UInt24 Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			if (!TryParse(s, style, provider, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static bool TryParse(string s, out UInt24 result)
		{
			return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
		}

		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out UInt24 result)
		{
			if (!uint.TryParse(s, style, provider, out var result2))
			{
				result = Zero;
				return false;
			}
			result = new UInt24(result2);
			return true;
		}

		public int CompareTo(UInt24 other)
		{
			return Value.CompareTo(other.Value);
		}

		public bool Equals(UInt24 other)
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

		public static implicit operator UInt24(byte a)
		{
			return new UInt24(a);
		}

		public static implicit operator UInt24(ushort a)
		{
			return new UInt24(a);
		}

		public static explicit operator UInt24(sbyte a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(short a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(Int24 a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(int a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(Int40 a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(Int48 a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(Int56 a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(long a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(uint a)
		{
			return new UInt24(a);
		}

		public static explicit operator UInt24(UInt40 a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(UInt48 a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(UInt56 a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(ulong a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(float a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(double a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(decimal a)
		{
			return new UInt24((uint)a);
		}

		public static explicit operator UInt24(BigInteger a)
		{
			return new UInt24((uint)a);
		}

		public static implicit operator int(UInt24 a)
		{
			return (int)a.Value;
		}

		public static implicit operator uint(UInt24 a)
		{
			return a.Value;
		}

		public static implicit operator Int40(UInt24 a)
		{
			return a.Value;
		}

		public static implicit operator UInt40(UInt24 a)
		{
			return a.Value;
		}

		public static implicit operator Int48(UInt24 a)
		{
			return a.Value;
		}

		public static implicit operator UInt48(UInt24 a)
		{
			return a.Value;
		}

		public static implicit operator Int56(UInt24 a)
		{
			return a.Value;
		}

		public static implicit operator UInt56(UInt24 a)
		{
			return a.Value;
		}

		public static implicit operator long(UInt24 a)
		{
			return a.Value;
		}

		public static implicit operator ulong(UInt24 a)
		{
			return a.Value;
		}

		public static implicit operator UInt128(UInt24 a)
		{
			return a.Value;
		}

		public static implicit operator float(UInt24 a)
		{
			return a.Value;
		}

		public static implicit operator double(UInt24 a)
		{
			return a.Value;
		}

		public static implicit operator decimal(UInt24 a)
		{
			return a.Value;
		}

		public static implicit operator BigInteger(UInt24 a)
		{
			return a.Value;
		}

		public static explicit operator sbyte(UInt24 a)
		{
			return (sbyte)a.Value;
		}

		public static explicit operator short(UInt24 a)
		{
			return (short)a.Value;
		}

		public static explicit operator Int24(UInt24 a)
		{
			return (Int24)a.Value;
		}

		public static explicit operator byte(UInt24 a)
		{
			return (byte)a.Value;
		}

		public static explicit operator ushort(UInt24 a)
		{
			return (ushort)a.Value;
		}

		public static UInt24 operator <<(UInt24 a, int b)
		{
			return new UInt24(a.Value << b);
		}

		public static UInt24 operator >>(UInt24 a, int b)
		{
			return new UInt24(a.Value >> b);
		}

		public static UInt24 operator &(UInt24 a, UInt24 b)
		{
			return new UInt24(a.Value & b.Value);
		}

		public static UInt24 operator |(UInt24 a, UInt24 b)
		{
			return new UInt24(a.Value | b.Value);
		}

		public static UInt24 operator ^(UInt24 a, UInt24 b)
		{
			return new UInt24(a.Value ^ b.Value);
		}

		public static UInt24 operator ~(UInt24 a)
		{
			return new UInt24(~a.Value);
		}

		public static UInt24 operator +(UInt24 a)
		{
			return a;
		}

		public static UInt24 operator -(UInt24 a)
		{
			return new UInt24((uint)(0uL - (ulong)a.Value));
		}

		public static UInt24 operator +(UInt24 a, UInt24 b)
		{
			return new UInt24(a.Value + b.Value);
		}

		public static UInt24 operator ++(UInt24 a)
		{
			return new UInt24(a.Value + 1);
		}

		public static UInt24 operator -(UInt24 a, UInt24 b)
		{
			return new UInt24(a.Value - b.Value);
		}

		public static UInt24 operator --(UInt24 a)
		{
			return new UInt24(a.Value - 1);
		}

		public static UInt24 operator *(UInt24 a, UInt24 b)
		{
			return new UInt24(a.Value * b.Value);
		}

		public static UInt24 operator /(UInt24 a, UInt24 b)
		{
			return new UInt24(a.Value / b.Value);
		}

		public static UInt24 operator %(UInt24 a, UInt24 b)
		{
			return new UInt24(a.Value % b.Value);
		}
	}
}
