using System;
using System.Globalization;
using System.Numerics;

namespace IDS.Core.Types
{
	public struct Int24 : IComparable<Int24>, IEquatable<Int24>, IFormattable
	{
		public static readonly Int24 Zero = (Int24)0;

		public static readonly Int24 MinValue = (Int24)(-8388608);

		public static readonly Int24 MaxValue = (Int24)8388607;

		private const int SIGN = 8388608;

		private const int SIGN_EXTEND = -8388608;

		private readonly int Value;

		private Int24(int value)
		{
			if (((uint)value & 0x800000u) != 0)
			{
				Value = value | -8388608;
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

		public static Int24 Parse(string s)
		{
			if (!TryParse(s, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static Int24 Parse(string s, NumberStyles style)
		{
			return Parse(s, style, NumberFormatInfo.CurrentInfo);
		}

		public static Int24 Parse(string s, IFormatProvider provider)
		{
			return Parse(s, NumberStyles.Integer, provider);
		}

		public static Int24 Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			if (!TryParse(s, style, provider, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static bool TryParse(string s, out Int24 result)
		{
			return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
		}

		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out Int24 result)
		{
			if (!int.TryParse(s, style, provider, out var result2))
			{
				result = Zero;
				return false;
			}
			result = new Int24(result2);
			return true;
		}

		public int CompareTo(Int24 other)
		{
			return Value.CompareTo(other.Value);
		}

		public bool Equals(Int24 other)
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

		public static implicit operator Int24(sbyte a)
		{
			return new Int24(a);
		}

		public static implicit operator Int24(byte a)
		{
			return new Int24(a);
		}

		public static implicit operator Int24(short a)
		{
			return new Int24(a);
		}

		public static implicit operator Int24(ushort a)
		{
			return new Int24(a);
		}

		public static explicit operator Int24(int a)
		{
			return new Int24(a);
		}

		public static explicit operator Int24(Int40 a)
		{
			return new Int24((int)a);
		}

		public static explicit operator Int24(Int48 a)
		{
			return new Int24((int)a);
		}

		public static explicit operator Int24(Int56 a)
		{
			return new Int24((int)a);
		}

		public static explicit operator Int24(long a)
		{
			return new Int24((int)a);
		}

		public static explicit operator Int24(UInt24 a)
		{
			return new Int24(a);
		}

		public static explicit operator Int24(uint a)
		{
			return new Int24((int)a);
		}

		public static explicit operator Int24(UInt40 a)
		{
			return new Int24((int)a);
		}

		public static explicit operator Int24(UInt48 a)
		{
			return new Int24((int)a);
		}

		public static explicit operator Int24(UInt56 a)
		{
			return new Int24((int)a);
		}

		public static explicit operator Int24(ulong a)
		{
			return new Int24((int)a);
		}

		public static explicit operator Int24(float a)
		{
			return new Int24((int)a);
		}

		public static explicit operator Int24(double a)
		{
			return new Int24((int)a);
		}

		public static explicit operator Int24(decimal a)
		{
			return new Int24((int)a);
		}

		public static explicit operator Int24(BigInteger a)
		{
			return new Int24((int)a);
		}

		public static implicit operator int(Int24 a)
		{
			return a.Value;
		}

		public static implicit operator Int40(Int24 a)
		{
			return a.Value;
		}

		public static implicit operator Int48(Int24 a)
		{
			return a.Value;
		}

		public static implicit operator Int56(Int24 a)
		{
			return a.Value;
		}

		public static implicit operator long(Int24 a)
		{
			return a.Value;
		}

		public static implicit operator float(Int24 a)
		{
			return a.Value;
		}

		public static implicit operator double(Int24 a)
		{
			return a.Value;
		}

		public static implicit operator decimal(Int24 a)
		{
			return a.Value;
		}

		public static implicit operator BigInteger(Int24 a)
		{
			return a.Value;
		}

		public static explicit operator sbyte(Int24 a)
		{
			return (sbyte)a.Value;
		}

		public static explicit operator short(Int24 a)
		{
			return (short)a.Value;
		}

		public static explicit operator byte(Int24 a)
		{
			return (byte)a.Value;
		}

		public static explicit operator ushort(Int24 a)
		{
			return (ushort)a.Value;
		}

		public static explicit operator UInt24(Int24 a)
		{
			return (UInt24)a.Value;
		}

		public static explicit operator uint(Int24 a)
		{
			return (uint)a.Value;
		}

		public static explicit operator UInt40(Int24 a)
		{
			return (UInt40)a.Value;
		}

		public static explicit operator UInt48(Int24 a)
		{
			return (UInt48)a.Value;
		}

		public static explicit operator UInt56(Int24 a)
		{
			return (UInt56)a.Value;
		}

		public static explicit operator ulong(Int24 a)
		{
			return (ulong)a.Value;
		}

		public static explicit operator UInt128(Int24 a)
		{
			return (UInt128)a.Value;
		}

		public static Int24 operator <<(Int24 a, int b)
		{
			return new Int24(a.Value << b);
		}

		public static Int24 operator >>(Int24 a, int b)
		{
			return new Int24(a.Value >> b);
		}

		public static Int24 operator &(Int24 a, Int24 b)
		{
			return new Int24(a.Value & b.Value);
		}

		public static Int24 operator |(Int24 a, Int24 b)
		{
			return new Int24(a.Value | b.Value);
		}

		public static Int24 operator ^(Int24 a, Int24 b)
		{
			return new Int24(a.Value ^ b.Value);
		}

		public static Int24 operator ~(Int24 a)
		{
			return new Int24(~a.Value);
		}

		public static Int24 operator +(Int24 a)
		{
			return a;
		}

		public static Int24 operator -(Int24 a)
		{
			return new Int24(-a.Value);
		}

		public static Int24 operator +(Int24 a, Int24 b)
		{
			return new Int24(a.Value + b.Value);
		}

		public static Int24 operator ++(Int24 a)
		{
			return new Int24(a.Value + 1);
		}

		public static Int24 operator -(Int24 a, Int24 b)
		{
			return new Int24(a.Value - b.Value);
		}

		public static Int24 operator --(Int24 a)
		{
			return new Int24(a.Value - 1);
		}

		public static Int24 operator *(Int24 a, Int24 b)
		{
			return new Int24(a.Value * b.Value);
		}

		public static Int24 operator /(Int24 a, Int24 b)
		{
			return new Int24(a.Value / b.Value);
		}

		public static Int24 operator %(Int24 a, Int24 b)
		{
			return new Int24(a.Value % b.Value);
		}
	}
}
