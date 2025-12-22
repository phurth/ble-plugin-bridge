using System;
using System.Globalization;
using System.Numerics;

namespace IDS.Core.Types
{
	public struct Int56 : IComparable<Int56>, IEquatable<Int56>, IFormattable
	{
		public static readonly Int56 Zero = 0;

		public static readonly Int56 MinValue = (Int56)(-36028797018963968L);

		public static readonly Int56 MaxValue = (Int56)36028797018963967L;

		private const long SIGN = 36028797018963968L;

		private const long SIGN_EXTEND = -36028797018963968L;

		private readonly long Value;

		private Int56(long value)
		{
			if ((value & 0x80000000000000L) != 0L)
			{
				Value = value | -36028797018963968L;
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

		public static Int56 Parse(string s)
		{
			if (!TryParse(s, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static Int56 Parse(string s, NumberStyles style)
		{
			return Parse(s, style, NumberFormatInfo.CurrentInfo);
		}

		public static Int56 Parse(string s, IFormatProvider provider)
		{
			return Parse(s, NumberStyles.Integer, provider);
		}

		public static Int56 Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			if (!TryParse(s, style, provider, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static bool TryParse(string s, out Int56 result)
		{
			return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
		}

		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out Int56 result)
		{
			if (!long.TryParse(s, style, provider, out var result2))
			{
				result = Zero;
				return false;
			}
			result = new Int56(result2);
			return true;
		}

		public int CompareTo(Int56 other)
		{
			return Value.CompareTo(other.Value);
		}

		public bool Equals(Int56 other)
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

		public static implicit operator Int56(sbyte a)
		{
			return new Int56(a);
		}

		public static implicit operator Int56(byte a)
		{
			return new Int56(a);
		}

		public static implicit operator Int56(short a)
		{
			return new Int56(a);
		}

		public static implicit operator Int56(ushort a)
		{
			return new Int56(a);
		}

		public static implicit operator Int56(Int24 a)
		{
			return new Int56(a);
		}

		public static implicit operator Int56(UInt24 a)
		{
			return new Int56(a);
		}

		public static implicit operator Int56(int a)
		{
			return new Int56(a);
		}

		public static implicit operator Int56(uint a)
		{
			return new Int56(a);
		}

		public static implicit operator Int56(Int40 a)
		{
			return new Int56(a);
		}

		public static implicit operator Int56(UInt40 a)
		{
			return new Int56(a);
		}

		public static implicit operator Int56(Int48 a)
		{
			return new Int56(a);
		}

		public static implicit operator Int56(UInt48 a)
		{
			return new Int56(a);
		}

		public static explicit operator Int56(long a)
		{
			return new Int56(a);
		}

		public static explicit operator Int56(UInt56 a)
		{
			return new Int56(a);
		}

		public static explicit operator Int56(ulong a)
		{
			return new Int56((long)a);
		}

		public static explicit operator Int56(float a)
		{
			return new Int56((long)a);
		}

		public static explicit operator Int56(double a)
		{
			return new Int56((long)a);
		}

		public static explicit operator Int56(decimal a)
		{
			return new Int56((long)a);
		}

		public static explicit operator Int56(BigInteger a)
		{
			return new Int56((long)a);
		}

		public static implicit operator long(Int56 a)
		{
			return a.Value;
		}

		public static implicit operator float(Int56 a)
		{
			return a.Value;
		}

		public static implicit operator double(Int56 a)
		{
			return a.Value;
		}

		public static implicit operator decimal(Int56 a)
		{
			return a.Value;
		}

		public static implicit operator BigInteger(Int56 a)
		{
			return a.Value;
		}

		public static explicit operator sbyte(Int56 a)
		{
			return (sbyte)a.Value;
		}

		public static explicit operator short(Int56 a)
		{
			return (short)a.Value;
		}

		public static explicit operator Int24(Int56 a)
		{
			return (Int24)a.Value;
		}

		public static explicit operator int(Int56 a)
		{
			return (int)a.Value;
		}

		public static explicit operator Int40(Int56 a)
		{
			return (Int40)a.Value;
		}

		public static explicit operator Int48(Int56 a)
		{
			return (Int48)a.Value;
		}

		public static explicit operator byte(Int56 a)
		{
			return (byte)a.Value;
		}

		public static explicit operator ushort(Int56 a)
		{
			return (ushort)a.Value;
		}

		public static explicit operator UInt24(Int56 a)
		{
			return (UInt24)a.Value;
		}

		public static explicit operator uint(Int56 a)
		{
			return (uint)a.Value;
		}

		public static explicit operator UInt40(Int56 a)
		{
			return (UInt40)a.Value;
		}

		public static explicit operator UInt48(Int56 a)
		{
			return (UInt48)a.Value;
		}

		public static explicit operator UInt56(Int56 a)
		{
			return (UInt56)a.Value;
		}

		public static explicit operator ulong(Int56 a)
		{
			return (ulong)a.Value;
		}

		public static explicit operator UInt128(Int56 a)
		{
			return (UInt128)a.Value;
		}

		public static Int56 operator <<(Int56 a, int b)
		{
			return new Int56(a.Value << b);
		}

		public static Int56 operator >>(Int56 a, int b)
		{
			return new Int56(a.Value >> b);
		}

		public static Int56 operator &(Int56 a, Int56 b)
		{
			return new Int56(a.Value & b.Value);
		}

		public static Int56 operator |(Int56 a, Int56 b)
		{
			return new Int56(a.Value | b.Value);
		}

		public static Int56 operator ^(Int56 a, Int56 b)
		{
			return new Int56(a.Value ^ b.Value);
		}

		public static Int56 operator ~(Int56 a)
		{
			return new Int56(~a.Value);
		}

		public static Int56 operator +(Int56 a)
		{
			return a;
		}

		public static Int56 operator -(Int56 a)
		{
			return new Int56(-a.Value);
		}

		public static Int56 operator +(Int56 a, Int56 b)
		{
			return new Int56(a.Value + b.Value);
		}

		public static Int56 operator ++(Int56 a)
		{
			return new Int56(a.Value + 1);
		}

		public static Int56 operator -(Int56 a, Int56 b)
		{
			return new Int56(a.Value - b.Value);
		}

		public static Int56 operator --(Int56 a)
		{
			return new Int56(a.Value - 1);
		}

		public static Int56 operator *(Int56 a, Int56 b)
		{
			return new Int56(a.Value * b.Value);
		}

		public static Int56 operator /(Int56 a, Int56 b)
		{
			return new Int56(a.Value / b.Value);
		}

		public static Int56 operator %(Int56 a, Int56 b)
		{
			return new Int56(a.Value % b.Value);
		}
	}
}
