namespace OneControl.Devices
{
	public class LogicalDeviceRelayHBridgeCommandFactoryMomentaryType2 : ILogicalDeviceRelayHBridgeCommandFactory
	{
		public ILogicalDeviceRelayHBridgeCommand MakeRelayCommand(LogicalDeviceRelayHBridgeDirection relayDirection)
		{
			return new LogicalDeviceRelayHBridgeMomentaryCommandType2(relayDirection);
		}

		public ILogicalDeviceRelayHBridgeCommand MakeRelayStopCommand()
		{
			return new LogicalDeviceRelayHBridgeMomentaryCommandType2(clearUserClearRequiredLatch: false);
		}

		public ILogicalDeviceRelayHBridgeCommand MakeClearFaultCommand()
		{
			return new LogicalDeviceRelayHBridgeMomentaryCommandType2(clearUserClearRequiredLatch: true);
		}

		public ILogicalDeviceRelayHBridgeCommand MakeHomeResetCommand()
		{
			return LogicalDeviceRelayHBridgeMomentaryCommandType2.MakeHomeResetCommand();
		}

		public ILogicalDeviceRelayHBridgeCommand MakeAutoForwardCommand()
		{
			return LogicalDeviceRelayHBridgeMomentaryCommandType2.MakeAutoCommand(HBridgeCommand.AutoForward);
		}

		public ILogicalDeviceRelayHBridgeCommand MakeAutoReverseCommand()
		{
			return LogicalDeviceRelayHBridgeMomentaryCommandType2.MakeAutoCommand(HBridgeCommand.AutoReverse);
		}
	}
}
