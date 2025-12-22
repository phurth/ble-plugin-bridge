using System;
using System.Globalization;
using System.Numerics;

namespace IDS.Core.Types
{
	public struct Int48 : IComparable<Int48>, IEquatable<Int48>, IFormattable
	{
		public static readonly Int48 Zero = 0;

		public static readonly Int48 MinValue = (Int48)(-140737488355328L);

		public static readonly Int48 MaxValue = (Int48)140737488355327L;

		private const long SIGN = 140737488355328L;

		private const long SIGN_EXTEND = -140737488355328L;

		private readonly long Value;

		private Int48(long value)
		{
			if ((value & 0x800000000000L) != 0L)
			{
				Value = value | -140737488355328L;
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

		public static Int48 Parse(string s)
		{
			if (!TryParse(s, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static Int48 Parse(string s, NumberStyles style)
		{
			return Parse(s, style, NumberFormatInfo.CurrentInfo);
		}

		public static Int48 Parse(string s, IFormatProvider provider)
		{
			return Parse(s, NumberStyles.Integer, provider);
		}

		public static Int48 Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			if (!TryParse(s, style, provider, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static bool TryParse(string s, out Int48 result)
		{
			return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
		}

		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out Int48 result)
		{
			if (!long.TryParse(s, style, provider, out var result2))
			{
				result = Zero;
				return false;
			}
			result = new Int48(result2);
			return true;
		}

		public int CompareTo(Int48 other)
		{
			return Value.CompareTo(other.Value);
		}

		public bool Equals(Int48 other)
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

		public static implicit operator Int48(sbyte a)
		{
			return new Int48(a);
		}

		public static implicit operator Int48(byte a)
		{
			return new Int48(a);
		}

		public static implicit operator Int48(short a)
		{
			return new Int48(a);
		}

		public static implicit operator Int48(ushort a)
		{
			return new Int48(a);
		}

		public static implicit operator Int48(Int24 a)
		{
			return new Int48(a);
		}

		public static implicit operator Int48(UInt24 a)
		{
			return new Int48(a);
		}

		public static implicit operator Int48(int a)
		{
			return new Int48(a);
		}

		public static implicit operator Int48(uint a)
		{
			return new Int48(a);
		}

		public static implicit operator Int48(Int40 a)
		{
			return new Int48(a);
		}

		public static implicit operator Int48(UInt40 a)
		{
			return new Int48(a);
		}

		public static explicit operator Int48(Int56 a)
		{
			return new Int48(a);
		}

		public static explicit operator Int48(long a)
		{
			return new Int48(a);
		}

		public static explicit operator Int48(UInt48 a)
		{
			return new Int48(a);
		}

		public static explicit operator Int48(UInt56 a)
		{
			return new Int48(a);
		}

		public static explicit operator Int48(ulong a)
		{
			return new Int48((long)a);
		}

		public static explicit operator Int48(float a)
		{
			return new Int48((long)a);
		}

		public static explicit operator Int48(double a)
		{
			return new Int48((long)a);
		}

		public static explicit operator Int48(decimal a)
		{
			return new Int48((long)a);
		}

		public static explicit operator Int48(BigInteger a)
		{
			return new Int48((long)a);
		}

		public static implicit operator Int56(Int48 a)
		{
			return (Int56)a.Value;
		}

		public static implicit operator long(Int48 a)
		{
			return a.Value;
		}

		public static implicit operator float(Int48 a)
		{
			return a.Value;
		}

		public static implicit operator double(Int48 a)
		{
			return a.Value;
		}

		public static implicit operator decimal(Int48 a)
		{
			return a.Value;
		}

		public static implicit operator BigInteger(Int48 a)
		{
			return a.Value;
		}

		public static explicit operator sbyte(Int48 a)
		{
			return (sbyte)a.Value;
		}

		public static explicit operator short(Int48 a)
		{
			return (short)a.Value;
		}

		public static explicit operator Int24(Int48 a)
		{
			return (Int24)a.Value;
		}

		public static explicit operator int(Int48 a)
		{
			return (int)a.Value;
		}

		public static explicit operator Int40(Int48 a)
		{
			return (Int40)a.Value;
		}

		public static explicit operator byte(Int48 a)
		{
			return (byte)a.Value;
		}

		public static explicit operator ushort(Int48 a)
		{
			return (ushort)a.Value;
		}

		public static explicit operator UInt24(Int48 a)
		{
			return (UInt24)a.Value;
		}

		public static explicit operator uint(Int48 a)
		{
			return (uint)a.Value;
		}

		public static explicit operator UInt40(Int48 a)
		{
			return (UInt40)a.Value;
		}

		public static explicit operator UInt48(Int48 a)
		{
			return (UInt48)a.Value;
		}

		public static explicit operator UInt56(Int48 a)
		{
			return (UInt56)a.Value;
		}

		public static explicit operator ulong(Int48 a)
		{
			return (ulong)a.Value;
		}

		public static explicit operator UInt128(Int48 a)
		{
			return (UInt128)a.Value;
		}

		public static Int48 operator <<(Int48 a, int b)
		{
			return new Int48(a.Value << b);
		}

		public static Int48 operator >>(Int48 a, int b)
		{
			return new Int48(a.Value >> b);
		}

		public static Int48 operator &(Int48 a, Int48 b)
		{
			return new Int48(a.Value & b.Value);
		}

		public static Int48 operator |(Int48 a, Int48 b)
		{
			return new Int48(a.Value | b.Value);
		}

		public static Int48 operator ^(Int48 a, Int48 b)
		{
			return new Int48(a.Value ^ b.Value);
		}

		public static Int48 operator ~(Int48 a)
		{
			return new Int48(~a.Value);
		}

		public static Int48 operator +(Int48 a)
		{
			return a;
		}

		public static Int48 operator -(Int48 a)
		{
			return new Int48(-a.Value);
		}

		public static Int48 operator +(Int48 a, Int48 b)
		{
			return new Int48(a.Value + b.Value);
		}

		public static Int48 operator ++(Int48 a)
		{
			return new Int48(a.Value + 1);
		}

		public static Int48 operator -(Int48 a, Int48 b)
		{
			return new Int48(a.Value - b.Value);
		}

		public static Int48 operator --(Int48 a)
		{
			return new Int48(a.Value - 1);
		}

		public static Int48 operator *(Int48 a, Int48 b)
		{
			return new Int48(a.Value * b.Value);
		}

		public static Int48 operator /(Int48 a, Int48 b)
		{
			return new Int48(a.Value / b.Value);
		}

		public static Int48 operator %(Int48 a, Int48 b)
		{
			return new Int48(a.Value % b.Value);
		}
	}
}
