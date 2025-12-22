using System;
using System.Collections.Generic;
using System.Text;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkLevelerConsoleText : MyRvLinkEvent<MyRvLinkLevelerConsoleText>
	{
		private const ushort NullCh = 32;

		public static readonly ushort[] UnicodeCharset = new ushort[256]
		{
			0, 8855, 8648, 8649, 9425, 9432, 9444, 9431, 32, 9,
			9226, 32, 32, 9229, 8597, 8596, 8593, 8599, 8594, 8600,
			8595, 8601, 8592, 8598, 32, 32, 32, 32, 32, 32,
			32, 32, 32, 33, 34, 35, 36, 37, 38, 39,
			40, 41, 42, 43, 44, 45, 46, 47, 48, 49,
			50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
			60, 61, 62, 63, 64, 65, 66, 67, 68, 69,
			70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
			80, 81, 82, 83, 84, 85, 86, 87, 88, 89,
			90, 91, 92, 93, 94, 95, 96, 97, 98, 99,
			100, 101, 102, 103, 104, 105, 106, 107, 108, 109,
			110, 111, 112, 113, 114, 115, 116, 117, 118, 119,
			120, 121, 122, 123, 124, 125, 126, 32, 32, 32,
			32, 402, 32, 32, 32, 32, 32, 32, 352, 32,
			338, 32, 381, 32, 32, 8216, 8217, 8220, 8221, 8226,
			8722, 32, 32, 8482, 353, 32, 339, 32, 382, 376,
			32, 161, 162, 163, 164, 165, 32, 32, 32, 169,
			32, 32, 32, 32, 174, 32, 176, 177, 178, 179,
			32, 181, 32, 183, 184, 185, 32, 32, 188, 189,
			190, 191, 192, 193, 194, 195, 196, 197, 198, 199,
			200, 201, 202, 203, 204, 205, 206, 207, 208, 209,
			210, 211, 212, 213, 214, 215, 216, 217, 218, 219,
			220, 221, 222, 223, 224, 225, 226, 227, 228, 229,
			230, 231, 232, 233, 234, 235, 236, 237, 238, 239,
			240, 241, 242, 243, 244, 245, 246, 247, 248, 249,
			250, 251, 252, 253, 254, 255
		};

		private const int DeviceTableIdIndex = 1;

		private const int DeviceIdIndex = 2;

		private const int ConsoleMessageStartIndex = 3;

		private const byte DelimiterByte = 0;

		private const int MaxLineLength = 32;

		private byte[] Utf16LineByteArray = new byte[64];

		public override MyRvLinkEventType EventType => MyRvLinkEventType.LevelerConsoleText;

		protected override int MinPayloadLength => 2;

		public byte DeviceId => _rawData[2];

		public byte DeviceTableId => _rawData[1];

		protected override byte[] _rawData { get; }

		protected MyRvLinkLevelerConsoleText(IReadOnlyList<byte> rawData)
		{
			_rawData = new byte[rawData.Count];
			for (int i = 0; i < rawData.Count; i++)
			{
				_rawData[i] = rawData[i];
			}
		}

		public static MyRvLinkLevelerConsoleText Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkLevelerConsoleText(rawData);
		}

		public List<string> GetConsoleMessages()
		{
			List<string> list = new List<string>();
			int num = 3;
			try
			{
				for (int i = 3; i < _rawData.Length; i++)
				{
					if (_rawData[i] == 0)
					{
						ArraySegment<byte> utf8ByteArray = new ArraySegment<byte>(_rawData, num, i - num);
						list.Add(GetUtf16(utf8ByteArray).Trim());
						num = i + 1;
					}
				}
				return list;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("RvLinkConsoleMessages", "Leveler text console decoding error - " + ex);
				return list;
			}
		}

		private string GetUtf16(ArraySegment<byte> utf8ByteArray)
		{
			lock (this)
			{
				int num = 0;
				Utf16LineByteArray.Clear();
				foreach (byte item in utf8ByteArray)
				{
					ushort num2 = UnicodeCharset[item];
					Utf16LineByteArray[2 * num] = (byte)num2;
					Utf16LineByteArray[2 * num + 1] = (byte)(num2 >> 8);
					num++;
					if (num >= 32)
					{
						break;
					}
				}
				return Encoding.Unicode.GetString(Utf16LineByteArray);
			}
		}
	}
}
