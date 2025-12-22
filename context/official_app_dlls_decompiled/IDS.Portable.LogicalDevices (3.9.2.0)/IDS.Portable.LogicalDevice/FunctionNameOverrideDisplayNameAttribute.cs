using System;

namespace IDS.Portable.LogicalDevice
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	internal class FunctionNameOverrideDisplayNameAttribute : Attribute
	{
		private readonly OverrideDisplayNameOperation _displayNameOperation;

		private readonly string[] _parameters;

		public FunctionNameOverrideDisplayNameAttribute(OverrideDisplayNameOperation displayNameOperation, params string[] parameters)
		{
			_displayNameOperation = displayNameOperation;
			_parameters = parameters;
		}

		public string Transform(string? inString)
		{
			return FunctionNameOverrideDisplayName.Transform(_displayNameOperation, inString, _parameters);
		}
	}
}
