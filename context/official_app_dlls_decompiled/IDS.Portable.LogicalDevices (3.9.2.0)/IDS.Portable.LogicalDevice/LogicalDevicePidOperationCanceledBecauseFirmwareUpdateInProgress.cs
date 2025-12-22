using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidOperationCanceledBecauseFirmwareUpdateInProgress : OperationCanceledException
	{
		public LogicalDevicePidOperationCanceledBecauseFirmwareUpdateInProgress()
			: base("Pid Operation Canceled Because Firmware Update In Progress")
		{
		}
	}
}
