using System;
using ids.portable.ble.BleManager;

namespace ids.portable.ble.Exceptions
{
	public class NotBondedException : BleManagerException, IConnectionFailureBleException
	{
		public NotBondedException()
			: this("Not bonded")
		{
		}

		public NotBondedException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public virtual BleManagerConnectionFailure ConvertToConnectionFailure()
		{
			return BleManagerConnectionFailure.BleDeviceNotBonded;
		}
	}
}
