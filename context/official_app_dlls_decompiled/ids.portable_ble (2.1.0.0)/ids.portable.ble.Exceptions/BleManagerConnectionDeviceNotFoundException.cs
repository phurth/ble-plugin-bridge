using System;
using ids.portable.ble.BleManager;

namespace ids.portable.ble.Exceptions
{
	public class BleManagerConnectionDeviceNotFoundException : BleManagerConnectionException, IConnectionFailureBleException
	{
		public BleManagerConnectionDeviceNotFoundException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public override BleManagerConnectionFailure ConvertToConnectionFailure()
		{
			return BleManagerConnectionFailure.DeviceNotFound;
		}
	}
}
