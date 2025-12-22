using System;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace IDS.Core.Types
{
	public struct UInt128 : IComparable, IComparable<UInt128>, IEquatable<UInt128>, IFormattable
	{
		private ulong hi;

		private ulong lo;

		public static readonly UInt128 MaxValue = ~(UInt128)0;

		public static readonly UInt128 MinValue = (UInt128)0;

		private static readonly UInt128 Zero = (UInt128)0;

		private static readonly double TwoPow64 = 1.8446744073709552E+19;

		private static readonly byte[] BitLengthTable = Enumerable.ToArray(Enumerable.Select(Enumerable.Range(0, 256), delegate(int value)
		{
			int num = 0;
			while (value != 0)
			{
				value >>= 1;
				num++;
			}
			return (byte)num;
		}));

		public ulong Hi64 => hi;

		public ulong Lo64 => lo;

		private uint R0 => (uint)lo;

		private uint R1 => (uint)(lo >> 32);

		private uint R2 => (uint)hi;

		private uint R3 => (uint)(hi >> 32);

		private UInt128(uint hihi, uint hilo, uint lohi, uint lolo)
		{
			lo = ((ulong)lohi << 32) | lolo;
			hi = ((ulong)hihi << 32) | hilo;
		}

		private UInt128(ulong hi, ulong lo)
		{
			this.hi = hi;
			this.lo = lo;
		}

		public UInt128(BigInteger value)
		{
			int sign = value.Sign;
			if (sign == -1)
			{
				value = -value;
			}
			hi = (ulong)(value >> 64);
			lo = (ulong)value;
			if (sign == -1)
			{
				Negate();
			}
		}

		public UInt128(decimal value)
		{
			int[] bits = decimal.GetBits(decimal.Truncate(value));
			UInt128 uInt = new UInt128(0u, (uint)bits[2], (uint)bits[1], (uint)bits[0]);
			if (value < 0m)
			{
				uInt = -uInt;
			}
			hi = uInt.hi;
			lo = uInt.lo;
		}

		public UInt128(double value)
		{
			bool num = value < 0.0;
			if (num)
			{
				value = 0.0 - value;
			}
			if (value <= 1.8446744073709552E+19)
			{
				hi = 0uL;
				lo = (ulong)value;
			}
			else
			{
				hi = 0uL;
				int num2 = Math.Max((int)Math.Ceiling(Math.Log(value, 2.0)) - 63, 0);
				lo = (ulong)(value / Math.Pow(2.0, num2));
				if (num2 >= 64)
				{
					hi = lo << num2 - 64;
					lo = 0uL;
				}
				else if (num2 != 0)
				{
					hi = (hi << num2) | (lo >> 64 - num2);
					lo <<= num2;
				}
			}
			if (num)
			{
				Negate();
			}
		}

		public UInt128(ulong value)
		{
			hi = 0uL;
			lo = value;
		}

		public UInt128(long value)
		{
			hi = (ulong)((value < 0) ? (-1) : 0);
			lo = (ulong)value;
		}

		public override int GetHashCode()
		{
			return lo.GetHashCode() ^ hi.GetHashCode();
		}

		public static UInt128 Parse(string s)
		{
			if (!TryParse(s, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static UInt128 Parse(string s, NumberStyles style)
		{
			return Parse(s, style, NumberFormatInfo.CurrentInfo);
		}

		public static UInt128 Parse(string s, IFormatProvider provider)
		{
			return Parse(s, NumberStyles.Integer, provider);
		}

		public static UInt128 Parse(string s, NumberStyles style, IFormatProvider provider)
		{
			if (!TryParse(s, style, provider, out var result))
			{
				throw new FormatException();
			}
			return result;
		}

		public static bool TryParse(string s, out UInt128 result)
		{
			return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
		}

		public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out UInt128 result)
		{
			if (!BigInteger.TryParse(s, style, provider, out var value))
			{
				result = Zero;
				return false;
			}
			result = new UInt128(value);
			return true;
		}

		public override string ToString()
		{
			return ((BigInteger)this).ToString();
		}

		public string ToString(string format)
		{
			return ((BigInteger)this).ToString(format);
		}

		public string ToString(IFormatProvider provider)
		{
			return ToString(null, provider);
		}

		public string ToString(string format, IFormatProvider provider)
		{
			return ((BigInteger)this).ToString(format, provider);
		}

		public static implicit operator UInt128(byte a)
		{
			return new UInt128(a);
		}

		public static implicit operator UInt128(ushort a)
		{
			return new UInt128(a);
		}

		public static implicit operator UInt128(UInt24 a)
		{
			return new UInt128(a);
		}

		public static implicit operator UInt128(uint a)
		{
			return new UInt128(a);
		}

		public static implicit operator UInt128(UInt48 a)
		{
			return new UInt128(a);
		}

		public static implicit operator UInt128(ulong a)
		{
			return new UInt128(a);
		}

		public static explicit operator UInt128(sbyte a)
		{
			return new UInt128(a);
		}

		public static explicit operator UInt128(short a)
		{
			return new UInt128(a);
		}

		public static explicit operator UInt128(Int24 a)
		{
			return new UInt128(a);
		}

		public static explicit operator UInt128(int a)
		{
			return new UInt128(a);
		}

		public static explicit operator UInt128(Int48 a)
		{
			return new UInt128(a);
		}

		public static explicit operator UInt128(long a)
		{
			return new UInt128(a);
		}

		public static explicit operator UInt128(float a)
		{
			return new UInt128(a);
		}

		public static explicit operator UInt128(double a)
		{
			return new UInt128(a);
		}

		public static explicit operator UInt128(decimal a)
		{
			return new UInt128(a);
		}

		public static explicit operator UInt128(BigInteger a)
		{
			return new UInt128(a);
		}

		public static implicit operator BigInteger(UInt128 a)
		{
			return ((BigInteger)a.hi << 64) | a.lo;
		}

		public static explicit operator decimal(UInt128 a)
		{
			if (a.hi == 0L)
			{
				return a.lo;
			}
			int num = Math.Max(0, 32 - GetNumBits64(a.hi));
			_ = a >> num;
			return new decimal((int)a.R0, (int)a.R1, (int)a.R2, isNegative: false, (byte)num);
		}

		public static explicit operator double(UInt128 a)
		{
			return (double)a.hi * TwoPow64 + (double)a.lo;
		}

		public static explicit operator float(UInt128 a)
		{
			return (float)(double)a;
		}

		public static explicit operator long(UInt128 a)
		{
			return (long)a.lo;
		}

		public static explicit operator ulong(UInt128 a)
		{
			return a.lo;
		}

		public static explicit operator int(UInt128 a)
		{
			return (int)a.lo;
		}

		public static explicit operator uint(UInt128 a)
		{
			return (uint)a.lo;
		}

		public static explicit operator short(UInt128 a)
		{
			return (short)a.lo;
		}

		public static explicit operator ushort(UInt128 a)
		{
			return (ushort)a.lo;
		}

		public static explicit operator byte(UInt128 a)
		{
			return (byte)a.lo;
		}

		public static explicit operator sbyte(UInt128 a)
		{
			return (sbyte)a.lo;
		}

		public static UInt128 operator <<(UInt128 a, int b)
		{
			if (b == 0)
			{
				return a;
			}
			if (b >= 64)
			{
				return new UInt128(a.lo << b - 64, 0uL);
			}
			return new UInt128((a.hi << b) | (a.lo >> 64 - b), a.lo << b);
		}

		public static UInt128 operator >>(UInt128 a, int b)
		{
			if (b > 64)
			{
				return new UInt128(a.hi >> b - 64);
			}
			switch (b)
			{
			case 64:
				return new UInt128(a.hi);
			case 0:
				return a;
			default:
			{
				UInt128 result = default(UInt128);
				result.lo = (a.lo >> b) | (a.hi << 64 - b);
				result.hi = a.hi >> b;
				return result;
			}
			}
		}

		public static UInt128 operator &(UInt128 a, UInt128 b)
		{
			return new UInt128(a.hi & b.hi, a.lo & b.lo);
		}

		public static uint operator &(UInt128 a, uint b)
		{
			return (uint)(int)a.lo & b;
		}

		public static uint operator &(uint a, UInt128 b)
		{
			return a & (uint)(int)b.lo;
		}

		public static ulong operator &(UInt128 a, ulong b)
		{
			return a.lo & b;
		}

		public static ulong operator &(ulong a, UInt128 b)
		{
			return a & b.lo;
		}

		public static UInt128 operator |(UInt128 a, UInt128 b)
		{
			return new UInt128(a.hi | b.hi, a.lo | b.lo);
		}

		public static UInt128 operator ^(UInt128 a, UInt128 b)
		{
			return new UInt128(a.hi ^ b.hi, a.lo ^ b.lo);
		}

		public static UInt128 operator ~(UInt128 value)
		{
			return new UInt128(~value.hi, ~value.lo);
		}

		public static UInt128 operator +(UInt128 value)
		{
			return value;
		}

		public static UInt128 operator -(UInt128 value)
		{
			UInt128 result = value;
			result.Negate();
			return result;
		}

		public static UInt128 operator +(UInt128 a, UInt128 b)
		{
			UInt128 result = default(UInt128);
			result.lo = a.lo + b.lo;
			result.hi = a.hi + b.hi;
			if (result.lo < a.lo && result.lo < b.lo)
			{
				result.hi++;
			}
			return result;
		}

		public static UInt128 operator +(UInt128 a, ulong b)
		{
			UInt128 result = a;
			result.lo += b;
			if (result.lo < a.lo && result.lo < b)
			{
				result.hi++;
			}
			return result;
		}

		public static UInt128 operator +(ulong a, UInt128 b)
		{
			return b + a;
		}

		public static UInt128 operator ++(UInt128 a)
		{
			return a + 1uL;
		}

		public static UInt128 operator -(UInt128 a, UInt128 b)
		{
			UInt128 result = a;
			result.lo -= b.lo;
			result.hi -= b.hi;
			if (a.lo < b.lo)
			{
				result.hi--;
			}
			return result;
		}

		public static UInt128 operator -(UInt128 a, ulong b)
		{
			UInt128 result = a;
			result.lo -= b;
			if (a.lo < b)
			{
				result.hi--;
			}
			return result;
		}

		public static UInt128 operator -(ulong a, UInt128 b)
		{
			UInt128 result = default(UInt128);
			result.lo = a - b.lo;
			result.hi = 0 - b.hi;
			if (a < b.lo)
			{
				result.hi--;
			}
			return result;
		}

		public static UInt128 operator --(UInt128 a)
		{
			return a - 1uL;
		}

		public static UInt128 operator *(UInt128 a, uint b)
		{
			long num = (uint)a.lo;
			ulong num2 = a.lo >> 32;
			ulong num3 = (ulong)(num * b);
			uint num4 = (uint)num3;
			num3 = (num3 >> 32) + num2 * b;
			UInt128 result = default(UInt128);
			result.lo = (num3 << 32) | num4;
			result.hi = num3 >> 32;
			if (a.hi != 0L)
			{
				result.hi += a.hi * b;
			}
			return result;
		}

		public static UInt128 operator *(uint a, UInt128 b)
		{
			return b * a;
		}

		public static UInt128 operator *(UInt128 a, ulong b)
		{
			ulong num = (uint)a.lo;
			ulong num2 = a.lo >> 32;
			ulong num3 = (uint)b;
			ulong num4 = b >> 32;
			ulong num5 = num * num3;
			uint num6 = (uint)num5;
			num5 = (num5 >> 32) + num * num4;
			ulong num7 = num5 >> 32;
			num5 = (uint)num5 + num2 * num3;
			UInt128 result = default(UInt128);
			result.lo = (num5 << 32) | num6;
			result.hi = (num5 >> 32) + num7 + num2 * num4;
			result.hi += a.hi * b;
			return result;
		}

		public static UInt128 operator *(ulong a, UInt128 b)
		{
			return b * a;
		}

		public static UInt128 operator *(UInt128 a, UInt128 b)
		{
			UInt128 result = a * b.lo;
			result.hi += a.lo * b.hi;
			return result;
		}

		public static UInt128 operator /(UInt128 a, UInt128 b)
		{
			if (LessThan(ref a, ref b))
			{
				return Zero;
			}
			if (b.hi == 0L)
			{
				return a / b.lo;
			}
			UInt128 rem;
			if (b.hi <= uint.MaxValue)
			{
				return DivRem96(out rem, ref a, ref b);
			}
			UInt128 rem2;
			return DivRem128(out rem2, ref a, ref b);
		}

		public static UInt128 operator /(UInt128 a, ulong b)
		{
			if (a.hi == 0L)
			{
				return new UInt128(a.lo / b);
			}
			uint num = (uint)b;
			UInt128 result = default(UInt128);
			if (b == num)
			{
				if (a.hi <= uint.MaxValue)
				{
					uint r = a.R2;
					uint num2 = r / num;
					ulong num3 = ((ulong)(r - num2 * num) << 32) | a.R1;
					uint num4 = (uint)(num3 / num);
					uint num5 = (uint)(((num3 - num4 * num << 32) | a.R0) / num);
					result = new UInt128(num2, ((ulong)num4 << 32) | num5);
				}
				else
				{
					uint r2 = a.R3;
					uint num6 = r2 / num;
					ulong num7 = ((ulong)(r2 - num6 * num) << 32) | a.R2;
					uint num8 = (uint)(num7 / num);
					ulong num9 = (num7 - num8 * num << 32) | a.R1;
					uint num10 = (uint)(num9 / num);
					uint num11 = (uint)(((num9 - num10 * num << 32) | a.R0) / num);
					result = new UInt128(((ulong)num6 << 32) | num8, ((ulong)num10 << 32) | num11);
				}
			}
			else if (a.hi <= uint.MaxValue)
			{
				result.lo = (result.hi = 0uL);
				int numBits = GetNumBits32((uint)(b >> 32));
				int num12 = 32 - numBits;
				ulong num13 = b << num12;
				uint v = (uint)(num13 >> 32);
				uint v2 = (uint)num13;
				uint u = a.R0;
				uint u2 = a.R1;
				uint u3 = a.R2;
				uint u4 = 0u;
				if (num12 != 0)
				{
					u4 = u3 >> numBits;
					u3 = (u3 << num12) | (u2 >> numBits);
					u2 = (u2 << num12) | (u >> numBits);
					u <<= num12;
				}
				uint num14 = DivRem(u4, ref u3, ref u2, v, v2);
				uint num15 = DivRem(u3, ref u2, ref u, v, v2);
				result = new UInt128(0uL, ((ulong)num14 << 32) | num15);
			}
			else
			{
				int numBits2 = GetNumBits32((uint)(b >> 32));
				int num16 = 32 - numBits2;
				ulong num17 = b << num16;
				uint v3 = (uint)(num17 >> 32);
				uint v4 = (uint)num17;
				uint u5 = a.R0;
				uint u6 = a.R1;
				uint u7 = a.R2;
				uint u8 = a.R3;
				uint u9 = 0u;
				if (num16 != 0)
				{
					u9 = u8 >> numBits2;
					u8 = (u8 << num16) | (u7 >> numBits2);
					u7 = (u7 << num16) | (u6 >> numBits2);
					u6 = (u6 << num16) | (u5 >> numBits2);
					u5 <<= num16;
				}
				result.hi = DivRem(u9, ref u8, ref u7, v3, v4);
				uint num18 = DivRem(u8, ref u7, ref u6, v3, v4);
				uint num19 = DivRem(u7, ref u6, ref u5, v3, v4);
				result.lo = ((ulong)num18 << 32) | num19;
			}
			return result;
		}

		public static UInt128 operator %(UInt128 a, UInt128 b)
		{
			if (LessThan(ref a, ref b))
			{
				return a;
			}
			if (b.hi == 0L)
			{
				return new UInt128(a % b.lo);
			}
			UInt128 rem;
			if (b.hi <= uint.MaxValue)
			{
				DivRem96(out rem, ref a, ref b);
			}
			else
			{
				DivRem128(out rem, ref a, ref b);
			}
			return rem;
		}

		public static ulong operator %(UInt128 a, ulong b)
		{
			if (a.hi == 0L)
			{
				return a.lo % b;
			}
			uint num = (uint)b;
			if (b == num)
			{
				return (a.hi <= uint.MaxValue) ? Remainder96(ref a, num) : Remainder128(ref a, num);
			}
			int numBits = GetNumBits32((uint)(b >> 32));
			int num2 = 32 - numBits;
			ulong num3 = b << num2;
			uint v = (uint)(num3 >> 32);
			uint v2 = (uint)num3;
			uint u = a.R0;
			uint u2 = a.R1;
			uint u3 = a.R2;
			if (a.hi <= uint.MaxValue)
			{
				uint u4 = 0u;
				if (num2 != 0)
				{
					u4 = u3 >> numBits;
					u3 = (u3 << num2) | (u2 >> numBits);
					u2 = (u2 << num2) | (u >> numBits);
					u <<= num2;
				}
				DivRem(u4, ref u3, ref u2, v, v2);
				DivRem(u3, ref u2, ref u, v, v2);
				return (((ulong)u2 << 32) | u) >> num2;
			}
			uint u5 = a.R3;
			uint u6 = 0u;
			if (num2 != 0)
			{
				u6 = u5 >> numBits;
				u5 = (u5 << num2) | (u3 >> numBits);
				u3 = (u3 << num2) | (u2 >> numBits);
				u2 = (u2 << num2) | (u >> numBits);
				u <<= num2;
			}
			DivRem(u6, ref u5, ref u3, v, v2);
			DivRem(u5, ref u3, ref u2, v, v2);
			DivRem(u3, ref u2, ref u, v, v2);
			return (((ulong)u2 << 32) | u) >> num2;
		}

		public static ulong operator %(UInt128 a, uint b)
		{
			if (a.hi == 0L)
			{
				return (uint)(a.lo % b);
			}
			if (a.hi <= uint.MaxValue)
			{
				return Remainder96(ref a, b);
			}
			return Remainder128(ref a, b);
		}

		public static bool operator <(UInt128 a, UInt128 b)
		{
			return LessThan(ref a, ref b);
		}

		public static bool operator <(UInt128 a, int b)
		{
			return LessThan(ref a, b);
		}

		public static bool operator <(int a, UInt128 b)
		{
			return LessThan(a, ref b);
		}

		public static bool operator <(UInt128 a, uint b)
		{
			return LessThan(ref a, b);
		}

		public static bool operator <(uint a, UInt128 b)
		{
			return LessThan(a, ref b);
		}

		public static bool operator <(UInt128 a, long b)
		{
			return LessThan(ref a, b);
		}

		public static bool operator <(long a, UInt128 b)
		{
			return LessThan(a, ref b);
		}

		public static bool operator <(UInt128 a, ulong b)
		{
			return LessThan(ref a, b);
		}

		public static bool operator <(ulong a, UInt128 b)
		{
			return LessThan(a, ref b);
		}

		public static bool operator <=(UInt128 a, UInt128 b)
		{
			return !LessThan(ref b, ref a);
		}

		public static bool operator <=(UInt128 a, int b)
		{
			return !LessThan(b, ref a);
		}

		public static bool operator <=(int a, UInt128 b)
		{
			return !LessThan(ref b, a);
		}

		public static bool operator <=(UInt128 a, uint b)
		{
			return !LessThan(b, ref a);
		}

		public static bool operator <=(uint a, UInt128 b)
		{
			return !LessThan(ref b, a);
		}

		public static bool operator <=(UInt128 a, long b)
		{
			return !LessThan(b, ref a);
		}

		public static bool operator <=(long a, UInt128 b)
		{
			return !LessThan(ref b, a);
		}

		public static bool operator <=(UInt128 a, ulong b)
		{
			return !LessThan(b, ref a);
		}

		public static bool operator <=(ulong a, UInt128 b)
		{
			return !LessThan(ref b, a);
		}

		public static bool operator >(UInt128 a, UInt128 b)
		{
			return LessThan(ref b, ref a);
		}

		public static bool operator >(UInt128 a, int b)
		{
			return LessThan(b, ref a);
		}

		public static bool operator >(int a, UInt128 b)
		{
			return LessThan(ref b, a);
		}

		public static bool operator >(UInt128 a, uint b)
		{
			return LessThan(b, ref a);
		}

		public static bool operator >(uint a, UInt128 b)
		{
			return LessThan(ref b, a);
		}

		public static bool operator >(UInt128 a, long b)
		{
			return LessThan(b, ref a);
		}

		public static bool operator >(long a, UInt128 b)
		{
			return LessThan(ref b, a);
		}

		public static bool operator >(UInt128 a, ulong b)
		{
			return LessThan(b, ref a);
		}

		public static bool operator >(ulong a, UInt128 b)
		{
			return LessThan(ref b, a);
		}

		public static bool operator >=(UInt128 a, UInt128 b)
		{
			return !LessThan(ref a, ref b);
		}

		public static bool operator >=(UInt128 a, int b)
		{
			return !LessThan(ref a, b);
		}

		public static bool operator >=(int a, UInt128 b)
		{
			return !LessThan(a, ref b);
		}

		public static bool operator >=(UInt128 a, uint b)
		{
			return !LessThan(ref a, b);
		}

		public static bool operator >=(uint a, UInt128 b)
		{
			return !LessThan(a, ref b);
		}

		public static bool operator >=(UInt128 a, long b)
		{
			return !LessThan(ref a, b);
		}

		public static bool operator >=(long a, UInt128 b)
		{
			return !LessThan(a, ref b);
		}

		public static bool operator >=(UInt128 a, ulong b)
		{
			return !LessThan(ref a, b);
		}

		public static bool operator >=(ulong a, UInt128 b)
		{
			return !LessThan(a, ref b);
		}

		public static bool operator ==(UInt128 a, UInt128 b)
		{
			return a.Equals(b);
		}

		public static bool operator ==(UInt128 a, int b)
		{
			return a.Equals(b);
		}

		public static bool operator ==(int a, UInt128 b)
		{
			return b.Equals(a);
		}

		public static bool operator ==(UInt128 a, uint b)
		{
			return a.Equals(b);
		}

		public static bool operator ==(uint a, UInt128 b)
		{
			return b.Equals(a);
		}

		public static bool operator ==(UInt128 a, long b)
		{
			return a.Equals(b);
		}

		public static bool operator ==(long a, UInt128 b)
		{
			return b.Equals(a);
		}

		public static bool operator ==(UInt128 a, ulong b)
		{
			return a.Equals(b);
		}

		public static bool operator ==(ulong a, UInt128 b)
		{
			return b.Equals(a);
		}

		public static bool operator !=(UInt128 a, UInt128 b)
		{
			return !a.Equals(b);
		}

		public static bool operator !=(UInt128 a, int b)
		{
			return !a.Equals(b);
		}

		public static bool operator !=(int a, UInt128 b)
		{
			return !b.Equals(a);
		}

		public static bool operator !=(UInt128 a, uint b)
		{
			return !a.Equals(b);
		}

		public static bool operator !=(uint a, UInt128 b)
		{
			return !b.Equals(a);
		}

		public static bool operator !=(UInt128 a, long b)
		{
			return !a.Equals(b);
		}

		public static bool operator !=(long a, UInt128 b)
		{
			return !b.Equals(a);
		}

		public static bool operator !=(UInt128 a, ulong b)
		{
			return !a.Equals(b);
		}

		public static bool operator !=(ulong a, UInt128 b)
		{
			return !b.Equals(a);
		}

		public int CompareTo(UInt128 other)
		{
			if (hi == other.hi)
			{
				return lo.CompareTo(other.lo);
			}
			return hi.CompareTo(other.hi);
		}

		public int CompareTo(int other)
		{
			if (hi == 0L && other >= 0)
			{
				return lo.CompareTo((ulong)other);
			}
			return 1;
		}

		public int CompareTo(uint other)
		{
			if (hi == 0L)
			{
				return lo.CompareTo(other);
			}
			return 1;
		}

		public int CompareTo(long other)
		{
			if (hi == 0L && other >= 0)
			{
				return lo.CompareTo((ulong)other);
			}
			return 1;
		}

		public int CompareTo(ulong other)
		{
			if (hi == 0L)
			{
				return lo.CompareTo(other);
			}
			return 1;
		}

		public int CompareTo(object obj)
		{
			if (obj == null)
			{
				return 1;
			}
			if (!(obj is UInt128 other))
			{
				throw new ArgumentException();
			}
			return CompareTo(other);
		}

		private static bool LessThan(ref UInt128 a, long b)
		{
			if (b >= 0 && a.hi == 0L)
			{
				return a.lo < (ulong)b;
			}
			return false;
		}

		private static bool LessThan(long a, ref UInt128 b)
		{
			if (a >= 0 && b.hi == 0L)
			{
				return (ulong)a < b.lo;
			}
			return true;
		}

		private static bool LessThan(ref UInt128 a, ulong b)
		{
			if (a.hi == 0L)
			{
				return a.lo < b;
			}
			return false;
		}

		private static bool LessThan(ulong a, ref UInt128 b)
		{
			if (b.hi == 0L)
			{
				return a < b.lo;
			}
			return true;
		}

		private static bool LessThan(ref UInt128 a, ref UInt128 b)
		{
			if (a.hi != b.hi)
			{
				return a.hi < b.hi;
			}
			return a.lo < b.lo;
		}

		public static bool Equals(ref UInt128 a, ref UInt128 b)
		{
			if (a.lo == b.lo)
			{
				return a.hi == b.hi;
			}
			return false;
		}

		public bool Equals(UInt128 other)
		{
			if (lo == other.lo)
			{
				return hi == other.hi;
			}
			return false;
		}

		public bool Equals(int other)
		{
			if (other >= 0 && lo == (uint)other)
			{
				return hi == 0;
			}
			return false;
		}

		public bool Equals(uint other)
		{
			if (lo == other)
			{
				return hi == 0;
			}
			return false;
		}

		public bool Equals(long other)
		{
			if (other >= 0 && lo == (ulong)other)
			{
				return hi == 0;
			}
			return false;
		}

		public bool Equals(ulong other)
		{
			if (lo == other)
			{
				return hi == 0;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is UInt128))
			{
				return false;
			}
			return Equals((UInt128)obj);
		}

		private void Negate()
		{
			bool num = lo != 0;
			lo = 0 - lo;
			hi = 0 - hi;
			if (num)
			{
				hi--;
			}
		}

		private static uint Remainder96(ref UInt128 u, uint v)
		{
			return (uint)((((((ulong)(u.R2 % v) << 32) | u.R1) % v << 32) | u.R0) % v);
		}

		private static uint Remainder128(ref UInt128 u, uint v)
		{
			return (uint)((((((((ulong)(u.R3 % v) << 32) | u.R2) % v << 32) | u.R1) % v << 32) | u.R0) % v);
		}

		private static ulong DivRem96(out UInt128 rem, ref UInt128 a, ref UInt128 b)
		{
			int shift = 32 - GetNumBits32(b.R2);
			LeftShift64(out var result, ref b, shift);
			int u = (int)LeftShift64(out rem, ref a, shift);
			uint r = result.R2;
			uint r2 = result.R1;
			uint r3 = result.R0;
			uint u2 = rem.R3;
			uint u3 = rem.R2;
			uint u4 = rem.R1;
			uint u5 = rem.R0;
			uint num = DivRem((uint)u, ref u2, ref u3, ref u4, r, r2, r3);
			uint num2 = DivRem(u2, ref u3, ref u4, ref u5, r, r2, r3);
			rem = new UInt128(0u, u3, u4, u5);
			ulong result2 = ((ulong)num << 32) | num2;
			RightShift64(ref rem, shift);
			return result2;
		}

		private static uint DivRem128(out UInt128 rem, ref UInt128 a, ref UInt128 b)
		{
			int shift = 32 - GetNumBits32(b.R3);
			LeftShift64(out var result, ref b, shift);
			int u = (int)LeftShift64(out rem, ref a, shift);
			uint u2 = rem.R3;
			uint u3 = rem.R2;
			uint u4 = rem.R1;
			uint u5 = rem.R0;
			uint result2 = DivRem((uint)u, ref u2, ref u3, ref u4, ref u5, result.R3, result.R2, result.R1, result.R0);
			rem = new UInt128(u2, u3, u4, u5);
			RightShift64(ref rem, shift);
			return result2;
		}

		private static ulong Q(uint u0, uint u1, uint u2, uint v1, uint v2)
		{
			ulong num = ((ulong)u0 << 32) | u1;
			ulong num2 = ((u0 == v1) ? uint.MaxValue : (num / v1));
			ulong num3 = num - num2 * v1;
			if (num3 == (uint)num3 && v2 * num2 > ((num3 << 32) | u2))
			{
				num2--;
				num3 += v1;
				if (num3 == (uint)num3 && v2 * num2 > ((num3 << 32) | u2))
				{
					num2--;
					num3 += v1;
				}
			}
			return num2;
		}

		private static uint DivRem(uint u0, ref uint u1, ref uint u2, uint v1, uint v2)
		{
			ulong num = Q(u0, u1, u2, v1, v2);
			ulong num2 = num * v2;
			long num3 = (long)u2 - (long)(uint)num2;
			num2 >>= 32;
			u2 = (uint)num3;
			num3 >>= 32;
			num2 += num * v1;
			num3 += (long)u1 - (long)(uint)num2;
			num2 >>= 32;
			u1 = (uint)num3;
			num3 >>= 32;
			num3 += (long)u0 - (long)(uint)num2;
			if (num3 != 0L)
			{
				num--;
				num2 = (ulong)u2 + (ulong)v2;
				u2 = (uint)num2;
				num2 >>= 32;
				num2 += (ulong)((long)u1 + (long)v1);
				u1 = (uint)num2;
			}
			return (uint)num;
		}

		private static uint DivRem(uint u0, ref uint u1, ref uint u2, ref uint u3, uint v1, uint v2, uint v3)
		{
			ulong num = Q(u0, u1, u2, v1, v2);
			ulong num2 = num * v3;
			long num3 = (long)u3 - (long)(uint)num2;
			num2 >>= 32;
			u3 = (uint)num3;
			num3 >>= 32;
			num2 += num * v2;
			num3 += (long)u2 - (long)(uint)num2;
			num2 >>= 32;
			u2 = (uint)num3;
			num3 >>= 32;
			num2 += num * v1;
			num3 += (long)u1 - (long)(uint)num2;
			num2 >>= 32;
			u1 = (uint)num3;
			num3 >>= 32;
			num3 += (long)u0 - (long)(uint)num2;
			if (num3 != 0L)
			{
				num--;
				num2 = (ulong)u3 + (ulong)v3;
				u3 = (uint)num2;
				num2 >>= 32;
				num2 += (ulong)((long)u2 + (long)v2);
				u2 = (uint)num2;
				num2 >>= 32;
				num2 += (ulong)((long)u1 + (long)v1);
				u1 = (uint)num2;
			}
			return (uint)num;
		}

		private static uint DivRem(uint u0, ref uint u1, ref uint u2, ref uint u3, ref uint u4, uint v1, uint v2, uint v3, uint v4)
		{
			ulong num = Q(u0, u1, u2, v1, v2);
			ulong num2 = num * v4;
			long num3 = (long)u4 - (long)(uint)num2;
			num2 >>= 32;
			u4 = (uint)num3;
			num3 >>= 32;
			num2 += num * v3;
			num3 += (long)u3 - (long)(uint)num2;
			num2 >>= 32;
			u3 = (uint)num3;
			num3 >>= 32;
			num2 += num * v2;
			num3 += (long)u2 - (long)(uint)num2;
			num2 >>= 32;
			u2 = (uint)num3;
			num3 >>= 32;
			num2 += num * v1;
			num3 += (long)u1 - (long)(uint)num2;
			num2 >>= 32;
			u1 = (uint)num3;
			num3 >>= 32;
			num3 += (long)u0 - (long)(uint)num2;
			if (num3 != 0L)
			{
				num--;
				num2 = (ulong)u4 + (ulong)v4;
				u4 = (uint)num2;
				num2 >>= 32;
				num2 += (ulong)((long)u3 + (long)v3);
				u3 = (uint)num2;
				num2 >>= 32;
				num2 += (ulong)((long)u2 + (long)v2);
				u2 = (uint)num2;
				num2 >>= 32;
				num2 += (ulong)((long)u1 + (long)v1);
				u1 = (uint)num2;
			}
			return (uint)num;
		}

		private static ulong LeftShift64(out UInt128 result, ref UInt128 value, int shift)
		{
			if (shift == 0)
			{
				result = value;
				return 0uL;
			}
			int num = 64 - shift;
			result.hi = (value.hi << shift) | (value.lo >> num);
			result.lo = value.lo << shift;
			return value.hi >> num;
		}

		private static void RightShift64(ref UInt128 value, int shift)
		{
			if (shift != 0)
			{
				value.lo = (value.hi << 64 - shift) | (value.lo >> shift);
				value.hi >>= shift;
			}
		}

		private static int GetNumBits64(ulong value)
		{
			uint num = (uint)(value >> 32);
			if (num == 0)
			{
				return GetNumBits32((uint)value);
			}
			return 32 + GetNumBits32(num);
		}

		private static int GetNumBits32(uint value)
		{
			ushort num = (ushort)(value >> 16);
			if (num == 0)
			{
				return GetNumBits16((ushort)value);
			}
			return 16 + GetNumBits16(num);
		}

		private static int GetNumBits16(ushort value)
		{
			byte b = (byte)(value >> 8);
			if (b == 0)
			{
				return BitLengthTable[value];
			}
			return 8 + BitLengthTable[b];
		}
	}
}
