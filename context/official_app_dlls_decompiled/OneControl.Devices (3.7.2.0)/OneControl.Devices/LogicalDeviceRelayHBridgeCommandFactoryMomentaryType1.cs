namespace OneControl.Devices
{
	public class LogicalDeviceRelayHBridgeCommandFactoryMomentaryType1 : ILogicalDeviceRelayHBridgeCommandFactory
	{
		public ILogicalDeviceRelayHBridgeCommand MakeRelayCommand(LogicalDeviceRelayHBridgeDirection relayDirection)
		{
			return new LogicalDeviceRelayHBridgeMomentaryCommandType1(relayDirection, clearFault: false);
		}

		public ILogicalDeviceRelayHBridgeCommand MakeRelayStopCommand()
		{
			return new LogicalDeviceRelayHBridgeMomentaryCommandType1(clearFault: false);
		}

		public ILogicalDeviceRelayHBridgeCommand MakeClearFaultCommand()
		{
			return new LogicalDeviceRelayHBridgeMomentaryCommandType1(clearFault: true);
		}

		public ILogicalDeviceRelayHBridgeCommand MakeHomeResetCommand()
		{
			return null;
		}

		public ILogicalDeviceRelayHBridgeCommand MakeAutoForwardCommand()
		{
			return null;
		}

		public ILogicalDeviceRelayHBridgeCommand MakeAutoReverseCommand()
		{
			return null;
		}
	}
}
