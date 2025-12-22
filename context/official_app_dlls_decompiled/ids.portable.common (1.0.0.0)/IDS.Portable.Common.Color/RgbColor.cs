using System;

namespace IDS.Portable.Common.Color
{
	public struct RgbColor
	{
		public static RgbColor ColorBlack = new RgbColor(0, 0, 0);

		public static RgbColor ColorRed = new RgbColor(byte.MaxValue, 0, 0);

		public static RgbColor ColorGreen = new RgbColor(0, byte.MaxValue, 0);

		public static RgbColor ColorBlue = new RgbColor(0, 0, byte.MaxValue);

		public static RgbColor ColorCyan = new RgbColor(0, byte.MaxValue, byte.MaxValue);

		public static RgbColor ColorMagenta = new RgbColor(byte.MaxValue, 0, byte.MaxValue);

		public static RgbColor ColorYellow = new RgbColor(byte.MaxValue, byte.MaxValue, 0);

		public static RgbColor ColorWhite = new RgbColor(byte.MaxValue, byte.MaxValue, byte.MaxValue);

		public byte Red { get; }

		public byte Green { get; }

		public byte Blue { get; }

		public RgbColor(int rgb)
		{
			Red = (byte)(rgb >> 16);
			Green = (byte)(rgb >> 8);
			Blue = (byte)rgb;
		}

		public RgbColor(byte red, byte green, byte blue)
		{
			Red = red;
			Green = green;
			Blue = blue;
		}

		public HsvColor ToHsv()
		{
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = (float)(int)Red / 255f;
			float num5 = (float)(int)Green / 255f;
			float num6 = (float)(int)Blue / 255f;
			float num7 = Math.Max(Math.Max(num4, num5), num6);
			float num8 = Math.Min(Math.Min(num4, num5), num6);
			float num9 = num7 - num8;
			num = (((double)Math.Abs(num4 - num5) < 0.0001 && (double)Math.Abs(num5 - num6) < 0.0001) ? 0f : (((double)Math.Abs(num7 - num4) < 0.0001) ? (60f * ((num5 - num6) / num9 % 6f)) : (((double)Math.Abs(num7 - num5) < 0.0001) ? (60f * ((num6 - num4) / num9 + 2f)) : ((!((double)Math.Abs(num7 - num6) < 0.0001)) ? 0f : (60f * ((num4 - num5) / num9 + 4f))))));
			num2 = ((!((double)Math.Abs(num7) < 0.0001)) ? (num9 / num7) : 0f);
			num3 = num7;
			return new HsvColor(num, num2, num3);
		}

		public static implicit operator HsvColor(RgbColor rgbColor)
		{
			return rgbColor.ToHsv();
		}

		public int ToInt()
		{
			return -16777216 | (Red << 16) | (Green << 8) | Blue;
		}

		public static implicit operator int(RgbColor rgbColor)
		{
			return rgbColor.ToInt();
		}

		public static implicit operator RgbColor(int value)
		{
			return new RgbColor((byte)((value & 0xFF0000) >> 16), (byte)((value & 0xFF00) >> 8), (byte)((uint)value & 0xFFu));
		}

		public override string ToString()
		{
			return ToInt().ToString("x8");
		}

		public bool IsCloseToColor(RgbColor matchColor, int colorDelta = 3)
		{
			if (Math.Abs(Red - matchColor.Red) > colorDelta)
			{
				return false;
			}
			if (Math.Abs(Green - matchColor.Green) > colorDelta)
			{
				return false;
			}
			return Math.Abs(Blue - matchColor.Blue) <= colorDelta;
		}

		public static bool operator ==(RgbColor a, RgbColor b)
		{
			return a.ToInt() == b.ToInt();
		}

		public static bool operator !=(RgbColor a, RgbColor b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (obj != null)
			{
				return this == (RgbColor)obj;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ToInt();
		}

		public bool IsBlack()
		{
			if (Red == 0 && Green == 0)
			{
				return Blue == 0;
			}
			return false;
		}

		public bool IsWhite()
		{
			if (Red == byte.MaxValue && Green == byte.MaxValue)
			{
				return Blue == byte.MaxValue;
			}
			return false;
		}
	}
}
