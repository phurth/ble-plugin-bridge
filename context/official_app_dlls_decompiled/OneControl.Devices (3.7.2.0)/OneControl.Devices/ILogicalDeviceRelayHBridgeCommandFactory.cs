namespace OneControl.Devices
{
	public interface ILogicalDeviceRelayHBridgeCommandFactory
	{
		ILogicalDeviceRelayHBridgeCommand MakeRelayCommand(LogicalDeviceRelayHBridgeDirection relayDirection);

		ILogicalDeviceRelayHBridgeCommand MakeRelayStopCommand();

		ILogicalDeviceRelayHBridgeCommand MakeClearFaultCommand();

		ILogicalDeviceRelayHBridgeCommand MakeHomeResetCommand();

		ILogicalDeviceRelayHBridgeCommand MakeAutoForwardCommand();

		ILogicalDeviceRelayHBridgeCommand MakeAutoReverseCommand();
	}
}
