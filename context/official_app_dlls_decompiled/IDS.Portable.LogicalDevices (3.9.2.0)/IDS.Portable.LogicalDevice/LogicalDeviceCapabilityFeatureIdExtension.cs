using IDS.Portable.Common.Extensions;

namespace IDS.Portable.LogicalDevice
{
	public static class LogicalDeviceCapabilityFeatureIdExtension
	{
		public static string? GetCloudToken(this LogicalDeviceCapabilityFeatureId capabilityFeatureId)
		{
			return capabilityFeatureId.GetAttribute<LogicalDeviceFeatureIdCloudAttribute>(inherit: false)?.CloudToken;
		}
	}
}
