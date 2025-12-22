using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLeveler5TouchpadStatus : LogicalDeviceStatusPacketMutable
	{
		public LogicalDeviceLeveler5TouchpadStatus()
			: base(0u)
		{
		}

		public LogicalDeviceLeveler5TouchpadStatus(LogicalDeviceLeveler5TouchpadStatus originalSensorStatus)
			: this()
		{
			byte[] data = originalSensorStatus.Data;
			Update(data, data.Length);
		}
	}
}
