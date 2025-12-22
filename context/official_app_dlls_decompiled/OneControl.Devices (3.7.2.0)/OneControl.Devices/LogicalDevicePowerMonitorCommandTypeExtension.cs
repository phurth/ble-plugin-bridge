namespace OneControl.Devices
{
	internal static class LogicalDevicePowerMonitorCommandTypeExtension
	{
		public static byte CommandByte(this LogicalDevicePowerMonitorCommandType commandType)
		{
			return commandType switch
			{
				LogicalDevicePowerMonitorCommandType.Off => 0, 
				LogicalDevicePowerMonitorCommandType.On => 1, 
				_ => 0, 
			};
		}
	}
}
