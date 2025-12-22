using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public struct LogicalDeviceRelayHBridgeDirection
	{
		public RelayHBridgeDirection RelayDirection { get; private set; }

		public RelayHBridgeEnergized RelayEnergized { get; private set; }

		public RelayHBridgeDirectionVerbose VerboseDirection { get; private set; }

		public ILogicalDeviceId LogicalId { get; }

		public LogicalDeviceRelayHBridgeDirection(RelayHBridgeEnergized relayEnergized, ILogicalDeviceId logicalId)
		{
			LogicalId = logicalId;
			RelayDirection = relayEnergized.ConvertToDirection(logicalId);
			RelayEnergized = relayEnergized;
			VerboseDirection = relayEnergized.ConvertToVerboseDirection(logicalId);
		}

		public LogicalDeviceRelayHBridgeDirection(RelayHBridgeDirection relayDirection, ILogicalDeviceId logicalId)
		{
			LogicalId = logicalId;
			RelayDirection = relayDirection;
			RelayEnergized = relayDirection.ConvertToRelayEnergized(logicalId);
			VerboseDirection = RelayEnergized.ConvertToVerboseDirection(logicalId);
		}

		public override string ToString()
		{
			return $"{RelayEnergized}";
		}
	}
}
