using System;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public class FirmwareUpdateSessionDisposedException : FirmwareUpdateException
	{
		public FirmwareUpdateSessionDisposedException(Exception? innerException = null)
			: base("Firmware Update Session Disposed", innerException)
		{
		}
	}
}
