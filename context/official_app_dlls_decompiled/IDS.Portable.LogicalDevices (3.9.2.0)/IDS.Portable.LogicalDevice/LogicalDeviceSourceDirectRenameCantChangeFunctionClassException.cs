using System;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceSourceDirectRenameCantChangeFunctionClassException : LogicalDeviceSourceDirectException
	{
		public LogicalDeviceSourceDirectRenameCantChangeFunctionClassException(FUNCTION_NAME toName, FUNCTION_CLASS fromClass, FUNCTION_CLASS toClass, Exception? innerException = null)
			: base($"Changing name to {toName} would cause FUNCTION_CLASS to change from {fromClass} to {toClass}", innerException)
		{
		}
	}
}
