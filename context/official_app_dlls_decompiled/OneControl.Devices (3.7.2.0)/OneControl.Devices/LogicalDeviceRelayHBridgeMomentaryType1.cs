using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayHBridgeMomentaryType1 : LogicalDeviceRelayHBridgeMomentary<LogicalDeviceRelayHBridgeStatusType1, LogicalDeviceRelayHBridgeCommandFactoryMomentaryType1, LogicalDeviceRelayCapabilityType1>
	{
		private const string LogTag = "LogicalDeviceRelayHBridgeMomentaryType1";

		public LogicalDeviceRelayHBridgeMomentaryType1(ILogicalDeviceId logicalDeviceId, LogicalDeviceRelayCapabilityType1 capability, ILogicalDeviceService service = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, capability, service, isFunctionClassChangeable)
		{
		}
	}
}
