namespace OneControl.Devices
{
	public class LogicalDeviceRelayBasicCommandFactoryLatchingType1 : ILogicalDeviceRelayBasicCommandFactory
	{
		public ILogicalDeviceRelayBasicCommand MakeClearFaultCommand()
		{
			return LogicalDeviceRelayBasicCommandType1.MakeClearFaultCommand();
		}

		public ILogicalDeviceRelayBasicCommand MakeRelayOffCommand()
		{
			return LogicalDeviceRelayBasicCommandType1.MakeLatchTurnOffRelayCommand();
		}

		public ILogicalDeviceRelayBasicCommand MakeRelayOnCommand()
		{
			return LogicalDeviceRelayBasicCommandType1.MakeLatchTurnOnRelayCommand();
		}
	}
}
