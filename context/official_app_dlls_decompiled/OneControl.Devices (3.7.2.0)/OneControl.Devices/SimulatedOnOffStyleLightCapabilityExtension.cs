using System.Collections.Concurrent;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public static class SimulatedOnOffStyleLightCapabilityExtension
	{
		private static readonly ConcurrentDictionary<SimulatedOnOffStyleLightCapability, LogicalDeviceCapabilitySerializable> _capabilitySerializableCache = new ConcurrentDictionary<SimulatedOnOffStyleLightCapability, LogicalDeviceCapabilitySerializable>();

		public static LogicalDeviceCapabilitySerializable ToCapabilitySerializable(this SimulatedOnOffStyleLightCapability instance)
		{
			if (_capabilitySerializableCache.TryGetValue(instance, out var result))
			{
				return result;
			}
			switch (instance)
			{
			case SimulatedOnOffStyleLightCapability.NotSupported:
			case SimulatedOnOffStyleLightCapability.Undefined:
			case SimulatedOnOffStyleLightCapability.Disabled:
			case SimulatedOnOffStyleLightCapability.Enabled:
				result = new LogicalDeviceCapabilitySerializable(string.Format("{0}-{1}", "SimulatedOnOffStyleLightCapability", instance));
				break;
			default:
				result = new LogicalDeviceCapabilitySerializable(string.Format("{0}-{1}", "SimulatedOnOffStyleLightCapability", PhysicalSwitchTypeCapability.Unknown));
				break;
			}
			_capabilitySerializableCache[instance] = result;
			return result;
		}
	}
}
