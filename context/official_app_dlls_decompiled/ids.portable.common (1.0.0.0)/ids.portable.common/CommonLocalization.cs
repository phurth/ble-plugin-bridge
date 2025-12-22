using System;

namespace ids.portable.common
{
	public static class CommonLocalization
	{
		public static Func<string?, string?>? LocalizationMethod { get; set; }

		public static string? Localize(string? originalStr)
		{
			return LocalizationMethod?.Invoke(originalStr) ?? originalStr;
		}
	}
}
