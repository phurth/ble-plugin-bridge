using System;
using System.Globalization;
using System.Numerics;

namespace IDS.Core.Types
{
	public struct Int40 : IComparable<Int40>, IEquatable<Int40>, IFormattable
	{
		public static readonly Int40 Zero = 0;

		public static readonly Int40 MinValue = (Int40)(-549755813888L);

		public static readonly Int40 MaxValue = (Int40)549755813887L;

		private const long SIGN = 549755813888L;

		private const long SIGN_EXTEND = -549755813888L;

		private readonly long Value;

		private Int40(long value)
		{
			if ((value & 0x8000000000L) != 0L)
			{
				Value = value | -549755813888L;
			}
			else
			{
				Value = value & MaxValue.Value;
			}
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static Int40 Parse(string s)
		{
			if (!TryParse(s, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static Int40 Parse(string s, NumberStyles style)
		{
			return Parse(s, style, NumberFormatInfo.CurrentInfo);
		}

		public static Int40 Parse(string s, IFormatProvider provider)
		{
			return Parse(s, NumberStyles.Integer, provider);
		}

		public static Int40 Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			if (!TryParse(s, style, provider, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static bool TryParse(string s, out Int40 result)
		{
			return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
		}

		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out Int40 result)
		{
			if (!long.TryParse(s, style, provider, out var result2))
			{
				result = Zero;
				return false;
			}
			result = new Int40(result2);
			return true;
		}

		public int CompareTo(Int40 other)
		{
			return Value.CompareTo(other.Value);
		}

		public bool Equals(Int40 other)
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

		public static implicit operator Int40(sbyte a)
		{
			return new Int40(a);
		}

		public static implicit operator Int40(byte a)
		{
			return new Int40(a);
		}

		public static implicit operator Int40(short a)
		{
			return new Int40(a);
		}

		public static implicit operator Int40(ushort a)
		{
			return new Int40(a);
		}

		public static implicit operator Int40(Int24 a)
		{
			return new Int40(a);
		}

		public static implicit operator Int40(UInt24 a)
		{
			return new Int40(a);
		}

		public static implicit operator Int40(int a)
		{
			return new Int40(a);
		}

		public static implicit operator Int40(uint a)
		{
			return new Int40(a);
		}

		public static explicit operator Int40(Int48 a)
		{
			return new Int40(a);
		}

		public static explicit operator Int40(Int56 a)
		{
			return new Int40(a);
		}

		public static explicit operator Int40(long a)
		{
			return new Int40(a);
		}

		public static explicit operator Int40(UInt40 a)
		{
			return new Int40(a);
		}

		public static explicit operator Int40(UInt48 a)
		{
			return new Int40(a);
		}

		public static explicit operator Int40(UInt56 a)
		{
			return new Int40(a);
		}

		public static explicit operator Int40(ulong a)
		{
			return new Int40((long)a);
		}

		public static explicit operator Int40(float a)
		{
			return new Int40((long)a);
		}

		public static explicit operator Int40(double a)
		{
			return new Int40((long)a);
		}

		public static explicit operator Int40(decimal a)
		{
			return new Int40((long)a);
		}

		public static explicit operator Int40(BigInteger a)
		{
			return new Int40((long)a);
		}

		public static implicit operator Int48(Int40 a)
		{
			return (Int48)a.Value;
		}

		public static implicit operator Int56(Int40 a)
		{
			return (Int56)a.Value;
		}

		public static implicit operator long(Int40 a)
		{
			return a.Value;
		}

		public static implicit operator float(Int40 a)
		{
			return a.Value;
		}

		public static implicit operator double(Int40 a)
		{
			return a.Value;
		}

		public static implicit operator decimal(Int40 a)
		{
			return a.Value;
		}

		public static implicit operator BigInteger(Int40 a)
		{
			return a.Value;
		}

		public static explicit operator sbyte(Int40 a)
		{
			return (sbyte)a.Value;
		}

		public static explicit operator short(Int40 a)
		{
			return (short)a.Value;
		}

		public static explicit operator Int24(Int40 a)
		{
			return (Int24)a.Value;
		}

		public static explicit operator int(Int40 a)
		{
			return (int)a.Value;
		}

		public static explicit operator byte(Int40 a)
		{
			return (byte)a.Value;
		}

		public static explicit operator ushort(Int40 a)
		{
			return (ushort)a.Value;
		}

		public static explicit operator UInt24(Int40 a)
		{
			return (UInt24)a.Value;
		}

		public static explicit operator uint(Int40 a)
		{
			return (uint)a.Value;
		}

		public static explicit operator UInt40(Int40 a)
		{
			return (UInt40)a.Value;
		}

		public static explicit operator UInt48(Int40 a)
		{
			return (UInt48)a.Value;
		}

		public static explicit operator UInt56(Int40 a)
		{
			return (UInt56)a.Value;
		}

		public static explicit operator ulong(Int40 a)
		{
			return (ulong)a.Value;
		}

		public static explicit operator UInt128(Int40 a)
		{
			return (UInt128)a.Value;
		}

		public static Int40 operator <<(Int40 a, int b)
		{
			return new Int40(a.Value << b);
		}

		public static Int40 operator >>(Int40 a, int b)
		{
			return new Int40(a.Value >> b);
		}

		public static Int40 operator &(Int40 a, Int40 b)
		{
			return new Int40(a.Value & b.Value);
		}

		public static Int40 operator |(Int40 a, Int40 b)
		{
			return new Int40(a.Value | b.Value);
		}

		public static Int40 operator ^(Int40 a, Int40 b)
		{
			return new Int40(a.Value ^ b.Value);
		}

		public static Int40 operator ~(Int40 a)
		{
			return new Int40(~a.Value);
		}

		public static Int40 operator +(Int40 a)
		{
			return a;
		}

		public static Int40 operator -(Int40 a)
		{
			return new Int40(-a.Value);
		}

		public static Int40 operator +(Int40 a, Int40 b)
		{
			return new Int40(a.Value + b.Value);
		}

		public static Int40 operator ++(Int40 a)
		{
			return new Int40(a.Value + 1);
		}

		public static Int40 operator -(Int40 a, Int40 b)
		{
			return new Int40(a.Value - b.Value);
		}

		public static Int40 operator --(Int40 a)
		{
			return new Int40(a.Value - 1);
		}

		public static Int40 operator *(Int40 a, Int40 b)
		{
			return new Int40(a.Value * b.Value);
		}

		public static Int40 operator /(Int40 a, Int40 b)
		{
			return new Int40(a.Value / b.Value);
		}

		public static Int40 operator %(Int40 a, Int40 b)
		{
			return new Int40(a.Value % b.Value);
		}
	}
}
