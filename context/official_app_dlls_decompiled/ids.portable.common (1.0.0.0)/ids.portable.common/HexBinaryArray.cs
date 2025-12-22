using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace IDS.Portable.Common
{
	public sealed class HexBinaryArray
	{
		public byte[] HexBytes { get; }

		public string HexString { get; }

		public HexBinaryArray(byte[] value)
		{
			HexBytes = value;
			StringBuilder stringBuilder = new StringBuilder
			{
				Length = 0
			};
			foreach (byte b in value)
			{
				stringBuilder.Append(b.ToString("X2"));
			}
			HexString = stringBuilder.ToString();
		}

		public HexBinaryArray(string value)
		{
			HexBytes = FromBinHexString(value);
			HexString = value;
		}

		public static byte[] FromBinHexString(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return new byte[0];
			}
			char[] array = value.ToCharArray();
			byte[] array2 = new byte[array.Length / 2 + array.Length % 2];
			int num = array.Length;
			if (num % 2 != 0)
			{
				throw new ArgumentException("FromBinHexString invalid value " + value);
			}
			int num2 = 0;
			for (int i = 0; i < num - 1; i += 2)
			{
				array2[num2] = FromHex(array[i], value);
				array2[num2] <<= 4;
				array2[num2] += FromHex(array[i + 1], value);
				num2++;
			}
			return array2;
		}

		private static byte FromHex(char hexDigit, string value)
		{
			try
			{
				return byte.Parse(hexDigit.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			}
			catch (FormatException)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 2);
				defaultInterpolatedStringHandler.AppendLiteral("FromHex invalid ");
				defaultInterpolatedStringHandler.AppendFormatted(hexDigit);
				defaultInterpolatedStringHandler.AppendLiteral(" for ");
				defaultInterpolatedStringHandler.AppendFormatted(value);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		public override string ToString()
		{
			return HexString;
		}

		public static implicit operator string(HexBinaryArray v)
		{
			return v.HexString;
		}

		public static implicit operator byte[](HexBinaryArray v)
		{
			return v.HexBytes;
		}
	}
}
