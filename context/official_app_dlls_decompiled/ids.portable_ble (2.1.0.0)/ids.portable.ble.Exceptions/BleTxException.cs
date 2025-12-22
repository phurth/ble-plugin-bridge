using System;

namespace ids.portable.ble.Exceptions
{
	public class BleTxException : Exception
	{
		public BleTxException()
		{
		}

		public BleTxException(string message)
			: base(message)
		{
		}

		public BleTxException(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
}
