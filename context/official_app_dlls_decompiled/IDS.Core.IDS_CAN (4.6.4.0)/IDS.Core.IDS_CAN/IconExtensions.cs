using System;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public static class IconExtensions
	{
		private static readonly Dictionary<ushort, ICON> Lookup;

		static IconExtensions()
		{
			Lookup = new Dictionary<ushort, ICON>();
			foreach (ICON value in Enum.GetValues(typeof(ICON)))
			{
				Lookup.Add((ushort)value, value);
			}
		}

		public static ICON ToICON(this ushort value)
		{
			if (Lookup.TryGetValue(value, out var value2))
			{
				return value2;
			}
			return ICON.UNKNOWN;
		}

		public static ushort Value(this ICON icon)
		{
			return (ushort)icon;
		}
	}
}
