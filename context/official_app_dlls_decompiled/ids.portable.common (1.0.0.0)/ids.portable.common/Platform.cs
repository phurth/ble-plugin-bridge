using System;

namespace IDS.Portable.Common
{
	public static class Platform
	{
		public static Func<string, bool>? IsPackageInstalledHandler { get; set; }

		public static bool IsPackageInstalled(string packageName)
		{
			return IsPackageInstalledHandler?.Invoke(packageName) ?? false;
		}
	}
}
