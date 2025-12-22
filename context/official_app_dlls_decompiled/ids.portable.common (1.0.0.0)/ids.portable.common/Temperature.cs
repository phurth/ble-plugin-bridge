using System;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;

namespace IDS.Portable.Common
{
	public struct Temperature
	{
		public enum DisplayType
		{
			Condensed,
			Default
		}

		public decimal Fahrenheit { get; private set; }

		public decimal Celsius => ConvertFahrenheitToCelsius(Fahrenheit);

		public decimal Kelvin => ConvertCelsiusToKelvin(Celsius);

		public static decimal ConvertCelsiusToFahrenheit(decimal celsius)
		{
			return celsius * 9m / 5m + 32m;
		}

		public static decimal ConvertCelsiusDeltaToFahrenheitDelta(decimal celsiusDelta)
		{
			return celsiusDelta * 9m / 5m;
		}

		public static decimal ConvertFahrenheitToCelsius(decimal fahrenheit)
		{
			return (fahrenheit - 32m) * 5m / 9m;
		}

		public static decimal ConvertFahrenheitDeltaToCelsiusDelta(decimal fahrenheitDelta)
		{
			return fahrenheitDelta * 5m / 9m;
		}

		public static decimal ConvertCelsiusToKelvin(decimal celsius)
		{
			return celsius + 273.15m;
		}

		public static decimal ConvertCelsiusDeltaToKelvinDelta(decimal celsiusDelta)
		{
			return celsiusDelta;
		}

		public static double ConvertCelsiusToFahrenheit(double celsius)
		{
			return (double)ConvertCelsiusToFahrenheit((decimal)celsius);
		}

		public static double ConvertCelsiusDeltaToFahrenheitDelta(double celsiusDelta)
		{
			return (double)ConvertCelsiusDeltaToFahrenheitDelta((decimal)celsiusDelta);
		}

		public static double ConvertFahrenheitToCelsius(double fahrenheit)
		{
			return (double)ConvertFahrenheitToCelsius((decimal)fahrenheit);
		}

		public static double ConvertCelsiusToKelvin(double celsius)
		{
			return (double)ConvertCelsiusToKelvin((decimal)celsius);
		}

		public static float ConvertCelsiusToFahrenheit(float celsius)
		{
			return (float)ConvertCelsiusToFahrenheit((decimal)celsius);
		}

		public static float ConvertFahrenheitToCelsius(float fahrenheit)
		{
			return (float)ConvertFahrenheitToCelsius((decimal)fahrenheit);
		}

		public static float ConvertCelsiusToKelvin(float celsius)
		{
			return (float)ConvertCelsiusToKelvin((decimal)celsius);
		}

		public static decimal ConvertKelvinToFahrenheit(double kelvin)
		{
			return Convert.ToDecimal((kelvin - 273.1499938964844) * 9.0 / 5.0 + 32.0);
		}

		public Temperature(float fahrenheit)
		{
			Fahrenheit = (decimal)fahrenheit;
		}

		public Temperature(decimal temperature, TemperatureScale scale)
		{
			this = default(Temperature);
			SetTemperature(temperature, scale);
		}

		public void SetFahrenheit(decimal fahrenheit)
		{
			SetTemperature(fahrenheit, TemperatureScale.Fahrenheit);
		}

		public void SetCelsius(decimal celsius)
		{
			SetTemperature(celsius, TemperatureScale.Celsius);
		}

		public void SetTemperature(decimal temperature, TemperatureScale scale)
		{
			switch (scale)
			{
			case TemperatureScale.Fahrenheit:
				Fahrenheit = temperature;
				break;
			case TemperatureScale.Celsius:
				Fahrenheit = ConvertCelsiusToFahrenheit(temperature);
				break;
			case TemperatureScale.Kelvin:
				Fahrenheit = ConvertKelvinToFahrenheit(Convert.ToDouble(temperature));
				break;
			default:
				throw new ArgumentOutOfRangeException("scale");
			}
		}

		public decimal ConvertToScale(TemperatureScale scale)
		{
			return scale switch
			{
				TemperatureScale.Fahrenheit => Fahrenheit, 
				TemperatureScale.Celsius => Celsius, 
				TemperatureScale.Kelvin => Kelvin, 
				_ => throw new ArgumentOutOfRangeException("scale"), 
			};
		}

		public static decimal ConvertToScaleFromFahrenheit(decimal fahrenheit, TemperatureScale scale)
		{
			return scale switch
			{
				TemperatureScale.Fahrenheit => fahrenheit, 
				TemperatureScale.Celsius => ConvertFahrenheitToCelsius(fahrenheit), 
				TemperatureScale.Kelvin => ConvertCelsiusToKelvin(ConvertFahrenheitToCelsius(fahrenheit)), 
				_ => throw new ArgumentOutOfRangeException("scale"), 
			};
		}

		public static decimal ConvertDeltaToScaleFromFahrenheitDelta(decimal fahrenheitDelta, TemperatureScale scale)
		{
			return scale switch
			{
				TemperatureScale.Fahrenheit => fahrenheitDelta, 
				TemperatureScale.Celsius => ConvertFahrenheitDeltaToCelsiusDelta(fahrenheitDelta), 
				TemperatureScale.Kelvin => ConvertCelsiusDeltaToKelvinDelta(ConvertFahrenheitDeltaToCelsiusDelta(fahrenheitDelta)), 
				_ => throw new ArgumentOutOfRangeException("scale"), 
			};
		}

		public static decimal ConvertToScale(decimal temperature, TemperatureScale fromScale, TemperatureScale toScale, double? roundThreshold = null)
		{
			if (fromScale == toScale)
			{
				return temperature;
			}
			decimal num = fromScale switch
			{
				TemperatureScale.Fahrenheit => temperature, 
				TemperatureScale.Celsius => ConvertCelsiusToFahrenheit(temperature), 
				_ => throw new ArgumentOutOfRangeException("fromScale"), 
			};
			if (!roundThreshold.HasValue)
			{
				return ConvertToScaleFromFahrenheit(num, toScale);
			}
			return ConvertToScaleFromFahrenheit(((double)num).RoundUsingThreshold(roundThreshold.Value), toScale);
		}

		public static decimal ConvertDeltaToScale(decimal temperatureDelta, TemperatureScale fromScale, TemperatureScale toScale, double? roundThreshold = null)
		{
			if (fromScale == toScale)
			{
				return temperatureDelta;
			}
			decimal num = fromScale switch
			{
				TemperatureScale.Fahrenheit => temperatureDelta, 
				TemperatureScale.Celsius => ConvertCelsiusDeltaToFahrenheitDelta(temperatureDelta), 
				_ => throw new ArgumentOutOfRangeException("fromScale"), 
			};
			if (!roundThreshold.HasValue)
			{
				return ConvertDeltaToScaleFromFahrenheitDelta(num, toScale);
			}
			return ConvertDeltaToScaleFromFahrenheitDelta(((double)num).RoundUsingThreshold(roundThreshold.Value), toScale);
		}

		public static string StringFormat(double temperature, TemperatureScale scale, DisplayType displayType = DisplayType.Default)
		{
			return StringFormat((decimal)temperature, scale, displayType);
		}

		public static string StringFormat(float temperature, TemperatureScale scale, DisplayType displayType = DisplayType.Default)
		{
			return StringFormat((decimal)temperature, scale, displayType);
		}

		public static string StringFormat(byte temperature, TemperatureScale scale, DisplayType displayType = DisplayType.Default)
		{
			return StringFormat((decimal)temperature, scale, displayType);
		}

		public static string StringFormat(decimal temperature, TemperatureScale scale, DisplayType displayType = DisplayType.Default)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			if ((uint)scale <= 2u)
			{
				if (displayType != DisplayType.Default)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 2);
					defaultInterpolatedStringHandler.AppendFormatted(decimal.ToInt32(temperature));
					defaultInterpolatedStringHandler.AppendFormatted(scale.Description());
					return defaultInterpolatedStringHandler.ToStringAndClear();
				}
				return temperature.ToString("N1") + " " + scale.Description();
			}
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 1);
			defaultInterpolatedStringHandler.AppendLiteral("The specified scale of '");
			defaultInterpolatedStringHandler.AppendFormatted(scale);
			defaultInterpolatedStringHandler.AppendLiteral("' is not supported.");
			throw new FormatException(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public override string ToString()
		{
			return ToString("F");
		}

		public string ToString(string format)
		{
			if (string.IsNullOrEmpty(format))
			{
				format = "C";
			}
			format = format.Trim().ToUpperInvariant();
			switch (format)
			{
			case "F":
				return StringFormat(Fahrenheit, TemperatureScale.Fahrenheit);
			case "K":
				return StringFormat(Kelvin, TemperatureScale.Kelvin);
			case "G":
			case "C":
				return StringFormat(Celsius, TemperatureScale.Celsius);
			default:
				throw new FormatException("The '" + format + "' format string is not supported.");
			}
		}
	}
}
