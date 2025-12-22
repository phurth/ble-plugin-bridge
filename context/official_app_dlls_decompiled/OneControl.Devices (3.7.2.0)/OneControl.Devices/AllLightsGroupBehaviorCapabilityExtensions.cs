using System.Collections.Concurrent;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public static class AllLightsGroupBehaviorCapabilityExtensions
	{
		private static readonly ConcurrentDictionary<AllLightsGroupBehaviorCapability, LogicalDeviceCapabilitySerializable> _capabilitySerializableCache = new ConcurrentDictionary<AllLightsGroupBehaviorCapability, LogicalDeviceCapabilitySerializable>();

		public static bool IsKnownAndSupported(this AllLightsGroupBehaviorCapability instance)
		{
			if (instance != 0)
			{
				return instance != AllLightsGroupBehaviorCapability.Undefined;
			}
			return false;
		}

		public static LogicalDeviceCapabilitySerializable ToCapabilitySerializable(this AllLightsGroupBehaviorCapability instance)
		{
			if (_capabilitySerializableCache.TryGetValue(instance, out var result))
			{
				return result;
			}
			result = new LogicalDeviceCapabilitySerializable(string.Format("{0}-{1}", "AllLightsGroupBehaviorCapability", instance));
			_capabilitySerializableCache[instance] = result;
			return result;
		}
	}
}
