using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDevicePowerMonitorSetecCommand : LogicalDeviceCommandPacket
	{
		private const int CommandPacketSize = 0;

		public LogicalDevicePowerMonitorSetecCommand(LogicalDevicePowerMonitorCommandType commandType)
			: base(commandType.CommandByte(), new byte[0])
		{
		}
	}
}
