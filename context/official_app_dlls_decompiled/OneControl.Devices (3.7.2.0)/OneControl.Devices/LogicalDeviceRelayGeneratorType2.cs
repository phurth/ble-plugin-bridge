using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayGeneratorType2 : LogicalDeviceRelayGenerator<LogicalDeviceRelayHBridgeStatusType2, LogicalDeviceRelayHBridgeCommandFactoryMomentaryType2, LogicalDeviceRelayCapabilityType2>
	{
		public LogicalDeviceRelayGeneratorType2(ILogicalDeviceId logicalDeviceId, LogicalDeviceRelayCapabilityType2 capability, ILogicalDeviceService service = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, capability, service, isFunctionClassChangeable)
		{
		}
	}
}
