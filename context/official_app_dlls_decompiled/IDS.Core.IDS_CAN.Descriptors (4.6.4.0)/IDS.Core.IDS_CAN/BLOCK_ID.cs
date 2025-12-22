using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public sealed class BLOCK_ID
	{
		public enum BLOCK_PROPERTIES_OPTIONS : byte
		{
			BLOCK_PROPERTIES_FLAGS,
			BLOCK_PROPERTIES_READ_SESSION_ID,
			BLOCK_PROPERTIES_WRITE_SESSION_ID,
			BLOCK_PROPERTIES_CAPACITY,
			BLOCK_PROPERTIES_CURRENT_SIZE,
			BLOCK_PROPERTIES_CRC,
			BLOCK_PROPERTIES_VERIFY_CRC,
			BLOCK_PROPERTIES_START_ADDRESS
		}

		private static readonly Dictionary<ushort, BLOCK_ID> Lookup = new Dictionary<ushort, BLOCK_ID>();

		private static readonly List<BLOCK_ID> List = new List<BLOCK_ID>();

		public static readonly BLOCK_ID UNKNOWN = new BLOCK_ID(0, "UNKNOWN");

		public static readonly BLOCK_ID BLOCK_ID_GENERIC_1 = new BLOCK_ID(1, "GENERIC_1");

		public static readonly BLOCK_ID BLOCK_ID_MONITOR_PANEL = new BLOCK_ID(2, "MONITOR_PANEL");

		public static readonly BLOCK_ID BLOCK_ID_REFLASH = new BLOCK_ID(3, "REFLASH");

		public static readonly BLOCK_ID BLOCK_ID_LOCAP_GROUP_DATA = new BLOCK_ID(4, "BLOCK_ID_LOCAP_GROUP_DATA");

		public readonly ushort Value;

		public readonly string Name;

		public bool IsValid => this?.Value > 0;

		public static IEnumerable<BLOCK_ID> GetEnumerator()
		{
			return List;
		}

		private BLOCK_ID(ushort value, string name)
		{
			Value = value;
			Name = name.Trim();
			if (value > 0)
			{
				List.Add(this);
				Lookup.Add(value, this);
			}
		}

		public static implicit operator ushort(BLOCK_ID msg)
		{
			return msg?.Value ?? 0;
		}

		public static implicit operator BLOCK_ID(ushort value)
		{
			if (Lookup.TryGetValue(value, out var value2))
			{
				return value2;
			}
			if (value == 0)
			{
				return UNKNOWN;
			}
			return new BLOCK_ID(value, "UNKNOWN_" + value.ToString("X4") + "h");
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
