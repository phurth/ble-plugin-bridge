using System;

namespace IDS.Portable.LogicalDevice
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public class LogicalDeviceFeatureIdCloudAttribute : Attribute
	{
		public string CloudToken { get; }

		public LogicalDeviceFeatureIdCloudAttribute(string cloudToken)
		{
			CloudToken = cloudToken;
		}
	}
}
