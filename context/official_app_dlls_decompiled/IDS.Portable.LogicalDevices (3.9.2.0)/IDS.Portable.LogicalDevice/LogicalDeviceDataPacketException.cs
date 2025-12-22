using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceDataPacketException : Exception
	{
		public LogicalDeviceDataPacketException(string message)
			: base(message)
		{
		}
	}
}
