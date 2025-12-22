using System;

namespace OneControl.Devices
{
	public class LevelerExtendedStatusHasNoDataException : LevelerException
	{
		public LevelerExtendedStatusHasNoDataException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public LevelerExtendedStatusHasNoDataException(Exception? innerException = null)
			: this("Extended Status Has No Data", innerException)
		{
		}
	}
}
