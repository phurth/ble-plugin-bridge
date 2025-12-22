using System;

namespace ids.portable.ble.Exceptions
{
	public class BleScannerScanResultParseException : BleServiceException
	{
		public BleScannerScanResultParseException(Exception innerException)
			: base("Unable to parse scan result", innerException)
		{
		}
	}
}
