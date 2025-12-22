using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public static class LogicalDeviceRelayHBridgeExtension
	{
		public static bool Allowed(this ILogicalDeviceRelayHBridgeCommand command, ILogicalDeviceWithStatus<ILogicalDeviceRelayHBridgeStatus> logicalDevice)
		{
			if (command.IsStopCommand)
			{
				return RelayHBridgeDirection.Stop.Allowed(logicalDevice);
			}
			if (command.IsForwardCommand)
			{
				return RelayHBridgeDirection.Forward.Allowed(logicalDevice);
			}
			if (command.IsReverseCommand)
			{
				return RelayHBridgeDirection.Reverse.Allowed(logicalDevice);
			}
			if (command.IsHomeResetCommand)
			{
				return RelayHBridgeDirection.Stop.Allowed(logicalDevice);
			}
			if (command.IsAutoForwardCommand && logicalDevice.DeviceCapabilityBasic is ILogicalDeviceRelayCapability logicalDeviceRelayCapability && logicalDeviceRelayCapability.AreAutoCommandsSupported)
			{
				return RelayHBridgeDirection.Forward.Allowed(logicalDevice);
			}
			if (command.IsAutoReverseCommand && logicalDevice.DeviceCapabilityBasic is ILogicalDeviceRelayCapability logicalDeviceRelayCapability2 && logicalDeviceRelayCapability2.AreAutoCommandsSupported)
			{
				return RelayHBridgeDirection.Reverse.Allowed(logicalDevice);
			}
			if (command.ClearingFault)
			{
				return true;
			}
			return false;
		}

		public static bool Allowed(this RelayHBridgeDirection forState, ILogicalDeviceWithStatus<ILogicalDeviceRelayHBridgeStatus> logicalDevice)
		{
			if (forState == RelayHBridgeDirection.Stop)
			{
				return true;
			}
			switch (logicalDevice.InTransitLockout)
			{
			case InTransitLockoutStatus.Unknown:
			case InTransitLockoutStatus.Off:
			case InTransitLockoutStatus.OnIgnored:
				return true;
			case InTransitLockoutStatus.OnSomeOperationsAllowed:
				return forState switch
				{
					RelayHBridgeDirection.Stop => true, 
					RelayHBridgeDirection.Forward => logicalDevice.DeviceStatus.CommandForwardNotHazardous, 
					RelayHBridgeDirection.Reverse => logicalDevice.DeviceStatus.CommandReverseNotHazardous, 
					_ => false, 
				};
			default:
				return false;
			}
		}
	}
}
