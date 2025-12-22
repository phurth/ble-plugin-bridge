using System;
using ids.portable.ble.BleManager;

namespace ids.portable.ble.Exceptions
{
	public class BleManagerConnectionKeySeedCancelledException : BleManagerConnectionException, IConnectionFailureBleException
	{
		public BleManagerConnectionKeySeedCancelledException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public override BleManagerConnectionFailure ConvertToConnectionFailure()
		{
			return BleManagerConnectionFailure.DeviceUnlockCanceled;
		}
	}
}
