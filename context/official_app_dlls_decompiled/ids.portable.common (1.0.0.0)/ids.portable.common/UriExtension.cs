using System;

namespace IDS.Portable.Common
{
	public static class UriExtension
	{
		public static Uri? TryMakeUri(string uriString)
		{
			try
			{
				return string.IsNullOrWhiteSpace(uriString) ? null : new Uri(uriString);
			}
			catch
			{
				return null;
			}
		}
	}
}
