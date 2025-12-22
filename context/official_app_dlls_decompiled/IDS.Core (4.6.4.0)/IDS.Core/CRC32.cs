using System.Collections.Generic;

namespace IDS.Core
{
	public class CRC32
	{
		private const uint RESET_VALUE = uint.MaxValue;

		private const uint CRC_POLYNOMIAL = 1947962583u;

		public uint Value { get; private set; }

		public static implicit operator uint(CRC32 crc)
		{
			return crc.Value;
		}

		public CRC32()
		{
			Reset();
		}

		public void Reset()
		{
			Value = uint.MaxValue;
		}

		public void Update(byte b)
		{
			Value = Update(Value, b);
		}

		public void Update(IReadOnlyList<byte> buffer)
		{
			Update(buffer, buffer.Count, 0);
		}

		public void Update(IReadOnlyList<byte> buffer, int count)
		{
			Update(buffer, count, 0);
		}

		public void Update(IReadOnlyList<byte> buffer, int count, int offset)
		{
			Value = Calculate(Value, buffer, count, offset);
		}

		public static uint Calculate(IReadOnlyCollection<byte> bytes)
		{
			uint num = uint.MaxValue;
			foreach (byte @byte in bytes)
			{
				num = Update(num, @byte);
			}
			return num;
		}

		public static uint Calculate(IReadOnlyList<byte> buffer, int count)
		{
			return Calculate(uint.MaxValue, buffer, count, 0);
		}

		public static uint Calculate(IReadOnlyList<byte> buffer, int count, int offset)
		{
			return Calculate(uint.MaxValue, buffer, count, offset);
		}

		private static uint Calculate(uint crc, IReadOnlyList<byte> buffer, int count, int offset)
		{
			while (count-- > 0)
			{
				crc = Update(crc, buffer[offset++]);
			}
			return crc;
		}

		private static uint Update(uint crc, byte data)
		{
			crc ^= (uint)(data << 24);
			for (int i = 0; i < 8; i++)
			{
				crc = (((crc & 0x80000000u) != 2147483648u) ? (crc << 1) : ((crc << 1) ^ 0x741B8CD7u));
			}
			return crc;
		}
	}
}
