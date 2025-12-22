using System;
using System.Linq;

namespace IDS.Portable.LogicalDevice
{
	internal static class FunctionNameOverrideDisplayName
	{
		public static string Transform(OverrideDisplayNameOperation displayNameOperation, string? inString, params string[] parameters)
		{
			return Transform(displayNameOperation, null, inString, parameters);
		}

		public static string Transform(OverrideDisplayNameOperation displayNameOperation, Func<string, string?>? defaultRulesTransform, string? inString, params string[] parameters)
		{
			if (inString == null)
			{
				inString = string.Empty;
			}
			string text = Enumerable.FirstOrDefault(parameters);
			string text2 = displayNameOperation switch
			{
				OverrideDisplayNameOperation.Unchanged => inString, 
				OverrideDisplayNameOperation.ApplyDefaultRules => defaultRulesTransform?.Invoke(inString) ?? inString, 
				OverrideDisplayNameOperation.Override => text ?? inString, 
				OverrideDisplayNameOperation.RemoveOccurrencesOf => (text == null) ? inString : inString!.Replace(text, string.Empty), 
				_ => inString, 
			};
			if (text2.Length == 0)
			{
				text2 = inString;
			}
			return text2;
		}
	}
}
