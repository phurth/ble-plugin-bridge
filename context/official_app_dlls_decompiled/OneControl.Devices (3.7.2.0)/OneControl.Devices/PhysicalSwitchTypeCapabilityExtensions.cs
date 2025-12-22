using System.Collections.Concurrent;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public static class PhysicalSwitchTypeCapabilityExtensions
	{
		private static readonly ConcurrentDictionary<PhysicalSwitchTypeCapability, LogicalDeviceCapabilitySerializable> _capabilitySerializableCache = new ConcurrentDictionary<PhysicalSwitchTypeCapability, LogicalDeviceCapabilitySerializable>();

		public static bool IsKnownAndSupported(this PhysicalSwitchTypeCapability instance)
		{
			if (instance != 0)
			{
				return instance != PhysicalSwitchTypeCapability.Unknown;
			}
			return false;
		}

		public static LogicalDeviceCapabilitySerializable ToCapabilitySerializable(this PhysicalSwitchTypeCapability instance)
		{
			if (_capabilitySerializableCache.TryGetValue(instance, out var result))
			{
				return result;
			}
			switch (instance)
			{
			case PhysicalSwitchTypeCapability.None:
			case PhysicalSwitchTypeCapability.Dimmable:
			case PhysicalSwitchTypeCapability.Toggle:
			case PhysicalSwitchTypeCapability.Momentary:
				result = new LogicalDeviceCapabilitySerializable(string.Format("{0}-{1}", "PhysicalSwitchTypeCapability", instance));
				break;
			default:
				result = new LogicalDeviceCapabilitySerializable(string.Format("{0}-{1}", "PhysicalSwitchTypeCapability", PhysicalSwitchTypeCapability.Unknown));
				break;
			}
			_capabilitySerializableCache[instance] = result;
			return result;
		}
	}
}
