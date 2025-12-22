using System;
using System.Text;

namespace IDS.Portable.Common.Extensions
{
	public static class StringExtension
	{
		private const string StrDblQuote = "\"";

		private static readonly char[] CharDblQuote = new char[1] { '"' };

		public static string Base64Encode(this string instance)
		{
			if (instance == null)
			{
				return "";
			}
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(instance));
		}

		public static string? Base64Decode(this string instance)
		{
			if (instance == null)
			{
				return null;
			}
			byte[] array = Convert.FromBase64String(instance);
			return Encoding.UTF8.GetString(array, 0, array.Length);
		}

		public static string? WrapWithDoubleQuotes(this string ssid)
		{
			if (ssid != null)
			{
				return "\"" + ssid.UnwrapDoubleQuotes() + "\"";
			}
			return null;
		}

		public static string? UnwrapDoubleQuotes(this string ssid)
		{
			return ssid?.Trim(CharDblQuote);
		}

		public static string Truncate(this string value, int maxLength, string truncationAppend = "")
		{
			if (maxLength < 0)
			{
				return value;
			}
			if (string.IsNullOrEmpty(value))
			{
				return value;
			}
			if (value.Length <= maxLength)
			{
				return value;
			}
			truncationAppend = truncationAppend ?? "";
			if (maxLength - truncationAppend.Length <= 0)
			{
				truncationAppend = "";
			}
			int length = maxLength - truncationAppend.Length;
			return value.Substring(0, length) + truncationAppend;
		}

		public static string TrimStartString(this string inputText, string value, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase)
		{
			if (!string.IsNullOrEmpty(value))
			{
				while (!string.IsNullOrEmpty(inputText) && inputText.StartsWith(value, comparisonType))
				{
					inputText = inputText.Substring(value.Length);
				}
			}
			return inputText;
		}

		public static string TrimEndString(this string inputText, string value, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase)
		{
			if (!string.IsNullOrEmpty(value))
			{
				while (!string.IsNullOrEmpty(inputText) && inputText.EndsWith(value, comparisonType))
				{
					inputText = inputText.Substring(0, inputText.Length - value.Length);
				}
			}
			return inputText;
		}

		public static string TrimString(this string inputText, string value, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase)
		{
			return inputText.TrimEndString(value, comparisonType).TrimStartString(value, comparisonType);
		}

		public static bool TryParseDecFlex(this string intAsString, out int result)
		{
			if (intAsString.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
			{
				try
				{
					int num = (result = Convert.ToInt32(intAsString.Remove(0, 2), 16));
					return true;
				}
				catch (Exception ex)
				{
					TaggedLog.Debug("TryParseDecFlex", "Invalid hex value '{0}': {1}", intAsString, ex.Message);
					result = 0;
					return false;
				}
			}
			if (intAsString.StartsWith("0b", StringComparison.CurrentCultureIgnoreCase))
			{
				try
				{
					int num2 = (result = Convert.ToInt32(intAsString.Remove(0, 2), 2));
					return true;
				}
				catch (Exception ex2)
				{
					TaggedLog.Debug("TryParseDecFlex", "Invalid binary value '{0}': {1}", intAsString, ex2.Message);
					result = 0;
					return false;
				}
			}
			return int.TryParse(intAsString, out result);
		}

		public static string? First(this string value, int maxCharacters = 1)
		{
			if (value == null)
			{
				return null;
			}
			if (string.IsNullOrEmpty(value) || value.Length <= 0 || maxCharacters <= 0)
			{
				return string.Empty;
			}
			if (maxCharacters >= value.Length)
			{
				return value;
			}
			return value.Substring(0, maxCharacters);
		}

		public static string? Last(this string value, int maxCharacters = 1)
		{
			if (value == null)
			{
				return null;
			}
			if (string.IsNullOrEmpty(value) || value.Length <= 0 || maxCharacters <= 0)
			{
				return string.Empty;
			}
			if (maxCharacters >= value.Length)
			{
				return value;
			}
			return value.Substring(value.Length - maxCharacters, maxCharacters);
		}
	}
}
