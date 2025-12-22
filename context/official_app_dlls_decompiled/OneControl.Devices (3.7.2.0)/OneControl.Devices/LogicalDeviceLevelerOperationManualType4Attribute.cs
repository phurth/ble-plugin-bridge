using System;

namespace OneControl.Devices
{
	[AttributeUsage(AttributeTargets.Field)]
	internal class LogicalDeviceLevelerOperationManualType4Attribute : Attribute
	{
		public bool HasConsole { get; }

		public bool HasFault { get; }

		public LogicalDeviceLevelerOperationManualType4Attribute(bool hasConsole = false, bool hasFault = false)
		{
			HasConsole = hasConsole;
			HasFault = hasFault;
		}
	}
}
