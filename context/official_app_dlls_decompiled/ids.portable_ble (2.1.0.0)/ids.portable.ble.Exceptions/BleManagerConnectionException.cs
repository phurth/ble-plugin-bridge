using System;
using ids.portable.ble.BleManager;

namespace ids.portable.ble.Exceptions
{
	public class BleManagerConnectionException : BleManagerException, IConnectionFailureBleException
	{
		public BleManagerConnectionException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public virtual BleManagerConnectionFailure ConvertToConnectionFailure()
		{
			return BleManagerConnectionFailure.Other;
		}
	}
}
