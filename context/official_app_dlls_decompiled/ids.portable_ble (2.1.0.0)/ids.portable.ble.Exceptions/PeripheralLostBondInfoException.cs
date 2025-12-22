using System;
using ids.portable.ble.BleManager;

namespace ids.portable.ble.Exceptions
{
	public class PeripheralLostBondInfoException : BleManagerException, IConnectionFailureBleException
	{
		public PeripheralLostBondInfoException()
			: this("Peripheral Lost Bond Information")
		{
		}

		public PeripheralLostBondInfoException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public BleManagerConnectionFailure ConvertToConnectionFailure()
		{
			return BleManagerConnectionFailure.BlePeripheralLostBondInfo;
		}
	}
}
