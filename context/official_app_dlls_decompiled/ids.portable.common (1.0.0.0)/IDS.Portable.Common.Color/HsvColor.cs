using System;
using System.Runtime.CompilerServices;

namespace IDS.Portable.Common.Color
{
	public struct HsvColor
	{
		private float _hue;

		private float _saturation;

		private float _value;

		public float Hue
		{
			get
			{
				return _hue;
			}
			private set
			{
				if (value < 0f)
				{
					_hue = Math.Abs(value);
					_hue = (360f + value) % 360f;
				}
				else if (value >= 360f)
				{
					_hue = value % 360f;
				}
				else
				{
					_hue = value;
				}
			}
		}

		public float Saturation
		{
			get
			{
				return _saturation;
			}
			private set
			{
				if (value < 0f)
				{
					_saturation = 0f;
				}
				else if (value > 1f)
				{
					_saturation = 1f;
				}
				else
				{
					_saturation = value;
				}
			}
		}

		public float Value
		{
			get
			{
				return _value;
			}
			private set
			{
				if (value < 0f)
				{
					_value = 0f;
				}
				else if (value > 1f)
				{
					_value = 1f;
				}
				else
				{
					_value = value;
				}
			}
		}

		public float Brightness => Value;

		public HsvColor(float[] hsv)
			: this(hsv[0], hsv[1], hsv[2])
		{
		}

		public HsvColor(float hue, float saturation, float value)
		{
			_hue = (_saturation = (_value = 0f));
			Hue = hue;
			Saturation = saturation;
			Value = value;
		}

		public RgbColor ToRgb()
		{
			byte red = 0;
			byte green = 0;
			byte blue = 0;
			float num = _value * _saturation;
			float num2 = num * (1f - Math.Abs(_hue / 60f % 2f - 1f));
			float num3 = _value - num;
			if (0f <= _hue && _hue < 60f)
			{
				red = (byte)((num + num3) * 255f);
				green = (byte)((num2 + num3) * 255f);
				blue = (byte)(num3 * 255f);
			}
			else if (60f <= _hue && _hue < 120f)
			{
				red = (byte)((num2 + num3) * 255f);
				green = (byte)((num + num3) * 255f);
				blue = (byte)(num3 * 255f);
			}
			else if (120f <= _hue && _hue < 180f)
			{
				red = (byte)(num3 * 255f);
				green = (byte)((num + num3) * 255f);
				blue = (byte)((num2 + num3) * 255f);
			}
			else if (180f <= _hue && _hue < 240f)
			{
				red = (byte)(num3 * 255f);
				green = (byte)((num2 + num3) * 255f);
				blue = (byte)((num + num3) * 255f);
			}
			else if (240f <= _hue && _hue < 300f)
			{
				red = (byte)((num2 + num3) * 255f);
				green = (byte)(num3 * 255f);
				blue = (byte)((num + num3) * 255f);
			}
			else if (300f <= _hue && _hue < 360f)
			{
				red = (byte)((num + num3) * 255f);
				green = (byte)(num3 * 255f);
				blue = (byte)((num2 + num3) * 255f);
			}
			return new RgbColor(red, green, blue);
		}

		public static implicit operator RgbColor(HsvColor hsvColor)
		{
			return hsvColor.ToRgb();
		}

		public float[] ToFloatArray()
		{
			return new float[3] { Hue, Saturation, Value };
		}

		public static implicit operator float[](HsvColor hsvColor)
		{
			return hsvColor.ToFloatArray();
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 3);
			defaultInterpolatedStringHandler.AppendFormatted(Hue, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted(Saturation, "F2");
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted(Value, "F2");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		public static bool operator ==(HsvColor a, HsvColor b)
		{
			if ((double)Math.Abs(a.Hue - b.Hue) < 0.0001 && (double)Math.Abs(a.Saturation - b.Saturation) < 0.0001)
			{
				return (double)Math.Abs(a.Value - b.Value) < 0.0001;
			}
			return false;
		}

		public static bool operator !=(HsvColor a, HsvColor b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (obj != null)
			{
				return this == (HsvColor)obj;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ToFloatArray().GetHashCode();
		}
	}
}
