using System;

namespace IDS.Portable.LogicalDevice.LogicalDevice
{
	public interface ILogicalDeviceMessageLogging
	{
		TimeSpan MessageInterval { get; }

		bool IsMessageLoggingActive { get; set; }

		DateTimeOffset LogDeviceMessage(object message, DateTimeOffset lastLogAt);
	}
}
