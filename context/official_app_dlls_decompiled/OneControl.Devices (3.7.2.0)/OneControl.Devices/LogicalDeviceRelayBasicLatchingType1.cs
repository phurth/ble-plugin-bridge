using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayBasicLatchingType1 : LogicalDeviceRelayBasicLatching<LogicalDeviceRelayBasicStatusType1, LogicalDeviceRelayBasicCommandFactoryLatchingType1, LogicalDeviceRelayCapabilityType1>
	{
		public LogicalDeviceRelayBasicLatchingType1(ILogicalDeviceId logicalDeviceId, LogicalDeviceRelayCapabilityType1 capability, ILogicalDeviceService service = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, capability, service, isFunctionClassChangeable)
		{
		}
	}
}
