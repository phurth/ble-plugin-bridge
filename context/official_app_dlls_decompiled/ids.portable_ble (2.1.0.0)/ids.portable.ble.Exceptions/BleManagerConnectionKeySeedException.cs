using System;
using ids.portable.ble.BleManager;

namespace ids.portable.ble.Exceptions
{
	public class BleManagerConnectionKeySeedException : BleManagerConnectionException, IConnectionFailureBleException
	{
		public BleManagerConnectionKeySeedException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public override BleManagerConnectionFailure ConvertToConnectionFailure()
		{
			return BleManagerConnectionFailure.DeviceUnlockFailure;
		}
	}
}
