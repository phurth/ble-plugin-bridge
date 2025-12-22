namespace OneControl.Devices
{
	public interface ILogicalDeviceRelayBasicCommandFactory
	{
		ILogicalDeviceRelayBasicCommand MakeRelayOnCommand();

		ILogicalDeviceRelayBasicCommand MakeRelayOffCommand();

		ILogicalDeviceRelayBasicCommand MakeClearFaultCommand();
	}
}
