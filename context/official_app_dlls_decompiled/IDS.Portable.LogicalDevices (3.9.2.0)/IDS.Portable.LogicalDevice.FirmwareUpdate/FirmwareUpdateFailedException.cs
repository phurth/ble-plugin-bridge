using System;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public class FirmwareUpdateFailedException : FirmwareUpdateException
	{
		public ILogicalDeviceTransferProgress Progress { get; }

		public FirmwareUpdateFailedException(ILogicalDevice logicalDevice, ILogicalDeviceTransferProgress progress, Exception? innerException = null)
			: base($"Firmware Update failed {progress} for {logicalDevice}", innerException)
		{
			Progress = progress;
		}
	}
}
