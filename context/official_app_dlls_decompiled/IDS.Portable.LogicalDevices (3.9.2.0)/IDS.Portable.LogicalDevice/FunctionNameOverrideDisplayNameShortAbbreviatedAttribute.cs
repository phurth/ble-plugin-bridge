using System;
using System.Collections.Generic;
using System.Text;

namespace IDS.Portable.LogicalDevice
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	internal class FunctionNameOverrideDisplayNameShortAbbreviatedAttribute : Attribute
	{
		private readonly OverrideDisplayNameOperation _displayNameOperation;

		private readonly string[] _parameters;

		private static readonly IReadOnlyDictionary<string, string> Abbreviations = new Dictionary<string, string>
		{
			{ "Bedroom", "BED" },
			{ "Room", "RM" },
			{ "Rear", "R" },
			{ "Front", "F" },
			{ "Refrigerator", "FRIDGE" },
			{ "Accessory", "ACCY" },
			{ "Water", "WTR" },
			{ "Heater", "HTR" },
			{ "Overhead", "OVRHD" },
			{ "Bathroom", "BATH" },
			{ "Kitchen", "KIT" },
			{ "Cabinet", "CAB" },
			{ "Living", "LVG" },
			{ "Stabilizer", "STBZR" },
			{ "Main", "MN" },
			{ "Entertainment", "ENT" },
			{ "Electric", "ELEC" },
			{ "Underbody", "UNDRBDY" },
			{ "Passenger", "PASS" },
			{ "equip", "EQ" },
			{ "Auxiliary", "AUX" }
		};

		public FunctionNameOverrideDisplayNameShortAbbreviatedAttribute(OverrideDisplayNameOperation displayNameOperation, params string[] parameters)
		{
			_displayNameOperation = displayNameOperation;
			_parameters = parameters;
		}

		public string Transform(string? inString)
		{
			return FunctionNameOverrideDisplayName.Transform(_displayNameOperation, DefaultRulesTransformer, inString, _parameters);
		}

		public static string DefaultRulesTransformer(string? inString)
		{
			if (inString == null)
			{
				inString = string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder(inString);
			foreach (KeyValuePair<string, string> abbreviation in Abbreviations)
			{
				stringBuilder.Replace(abbreviation.Key, abbreviation.Value);
			}
			return stringBuilder.ToString();
		}
	}
}
