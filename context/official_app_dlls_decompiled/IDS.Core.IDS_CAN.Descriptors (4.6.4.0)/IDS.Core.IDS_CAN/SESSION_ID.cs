using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public sealed class SESSION_ID
	{
		private static readonly Dictionary<ushort, SESSION_ID> Lookup;

		private static readonly List<SESSION_ID> List;

		public static readonly SESSION_ID UNKNOWN;

		public const ushort MANUFACTURING = 1;

		public const ushort DIAGNOSTIC = 2;

		public const ushort REPROGRAMMING = 3;

		public const ushort REMOTE_CONTROL = 4;

		public const ushort DAQ = 5;

		public readonly ushort Value;

		public readonly uint Cypher;

		public readonly string Name;

		public readonly string Description;

		public bool IsValid => this?.Value > 0;

		public static IEnumerable<SESSION_ID> GetEnumerator()
		{
			return List;
		}

		static SESSION_ID()
		{
			Lookup = new Dictionary<ushort, SESSION_ID>();
			List = new List<SESSION_ID>();
			UNKNOWN = new SESSION_ID(0, 0u, "UNKNOWN", "Reserved, do not use");
			new SESSION_ID(1, 2976620821u, "MANUFACTURING", "Used to enable manufacturing features");
			new SESSION_ID(2, 3133065982u, "DIAGNOSTIC", "Used to enable diagnostic tool features");
			new SESSION_ID(3, 3735928559u, "REPROGRAMMING", "Used when reprogramming a device");
			new SESSION_ID(4, 2976579765u, "REMOTE_CONTROL", "Used when enabling remote control of the device");
			new SESSION_ID(5, 184594741u, "DAQ", "Used to enable DAQ features");
		}

		private SESSION_ID(ushort value, uint cypher, string name, string description)
		{
			Value = value;
			Cypher = cypher;
			Name = name.Trim();
			Description = description.Trim();
			if (value > 0)
			{
				List.Add(this);
				Lookup.Add(value, this);
			}
		}

		public static implicit operator ushort(SESSION_ID msg)
		{
			return msg?.Value ?? 0;
		}

		public static implicit operator SESSION_ID(ushort value)
		{
			if (Lookup.TryGetValue(value, out var value2))
			{
				return value2;
			}
			return UNKNOWN;
		}

		public override string ToString()
		{
			return Name;
		}

		public uint Encrypt(uint seed)
		{
			uint num = Cypher;
			int num2 = 32;
			uint num3 = 2654435769u;
			while (true)
			{
				seed += ((num << 4) + 1131376761) ^ (num + num3) ^ ((num >> 5) + 1919510376);
				if (--num2 <= 0)
				{
					break;
				}
				num += ((seed << 4) + 1948272964) ^ (seed + num3) ^ ((seed >> 5) + 1400073827);
				num3 += 2654435769u;
			}
			return seed;
		}
	}
}
