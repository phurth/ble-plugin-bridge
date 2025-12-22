using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public static class RelayHBridgeDirectionExtension
	{
		public static RelayHBridgeEnergized ConvertToRelayEnergized(this RelayHBridgeDirection direction, ILogicalDeviceId logicalId)
		{
			return direction switch
			{
				RelayHBridgeDirection.Forward => RelayHBridgeEnergizedExtension.ConvertToRelayEnergized(isForward: true, isReverse: false, logicalId), 
				RelayHBridgeDirection.Reverse => RelayHBridgeEnergizedExtension.ConvertToRelayEnergized(isForward: false, isReverse: true, logicalId), 
				_ => RelayHBridgeEnergizedExtension.ConvertToRelayEnergized(isForward: false, isReverse: false, logicalId), 
			};
		}

		public static RelayHBridgeDirection ConvertToHBridgeDirection(bool relay1, bool relay2, ILogicalDeviceId logicalId)
		{
			if (relay1 == relay2)
			{
				return RelayHBridgeDirection.Stop;
			}
			RelayHBridgeEnergized relayHBridgeEnergized = (relay1 ? RelayHBridgeEnergized.Relay1 : RelayHBridgeEnergized.Relay2);
			if (relayHBridgeEnergized == RelayHBridgeDirection.Forward.ConvertToRelayEnergized(logicalId))
			{
				return RelayHBridgeDirection.Forward;
			}
			if (relayHBridgeEnergized == RelayHBridgeDirection.Reverse.ConvertToRelayEnergized(logicalId))
			{
				return RelayHBridgeDirection.Reverse;
			}
			return RelayHBridgeDirection.Stop;
		}

		public static (bool realy1, bool realy2) ConvertToRelay1Relay2(this RelayHBridgeDirection direction, ILogicalDeviceId logicalId)
		{
			return direction.ConvertToRelayEnergized(logicalId) switch
			{
				RelayHBridgeEnergized.Relay1 => (true, false), 
				RelayHBridgeEnergized.Relay2 => (false, true), 
				_ => (false, false), 
			};
		}
	}
}
