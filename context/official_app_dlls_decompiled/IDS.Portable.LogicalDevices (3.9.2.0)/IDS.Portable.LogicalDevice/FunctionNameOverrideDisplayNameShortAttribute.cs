using System;

namespace IDS.Portable.LogicalDevice
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	internal class FunctionNameOverrideDisplayNameShortAttribute : Attribute
	{
		private readonly OverrideDisplayNameOperation _displayNameOperation;

		private readonly string[] _parameters;

		public FunctionNameOverrideDisplayNameShortAttribute(OverrideDisplayNameOperation displayNameOperation, params string[] parameters)
		{
			_displayNameOperation = displayNameOperation;
			_parameters = parameters;
		}

		public string Transform(FUNCTION_CLASS functionClass, string? inString)
		{
			if (_displayNameOperation == OverrideDisplayNameOperation.ApplyDefaultRules)
			{
				return DefaultRulesTransformer(functionClass, inString);
			}
			return FunctionNameOverrideDisplayName.Transform(_displayNameOperation, inString, _parameters);
		}

		public static string DefaultRulesTransformer(FUNCTION_CLASS functionClass, string? inString)
		{
			if (inString == null)
			{
				inString = string.Empty;
			}
			inString = inString!.Replace(functionClass.GetName(), string.Empty);
			return inString;
		}
	}
}
