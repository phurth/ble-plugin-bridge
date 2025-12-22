using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayGeneratorType1 : LogicalDeviceRelayGenerator<LogicalDeviceRelayHBridgeStatusType1, LogicalDeviceRelayHBridgeCommandFactoryMomentaryType1, LogicalDeviceRelayCapabilityType1>
	{
		public LogicalDeviceRelayGeneratorType1(ILogicalDeviceId logicalDeviceId, LogicalDeviceRelayCapabilityType1 capability, ILogicalDeviceService service = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, capability, service, isFunctionClassChangeable)
		{
		}
	}
}
