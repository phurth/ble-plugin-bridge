namespace OneControl.Devices
{
	public class LogicalDeviceRelayBasicCommandFactoryLatchingType2 : ILogicalDeviceRelayBasicCommandFactory
	{
		public ILogicalDeviceRelayBasicCommand MakeClearFaultCommand()
		{
			return LogicalDeviceRelayBasicLatchingCommandType2.MakeClearFaultCommand();
		}

		public ILogicalDeviceRelayBasicCommand MakeRelayOffCommand()
		{
			return LogicalDeviceRelayBasicLatchingCommandType2.MakeLatchTurnOffRelayCommand();
		}

		public ILogicalDeviceRelayBasicCommand MakeRelayOnCommand()
		{
			return LogicalDeviceRelayBasicLatchingCommandType2.MakeLatchTurnOnRelayCommand();
		}
	}
}
