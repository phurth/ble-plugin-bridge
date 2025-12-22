using System;
using ids.portable.ble.BleManager;

namespace ids.portable.ble.Exceptions
{
	public class NotBondedPairingNotEnabledException : NotBondedException, IConnectionFailureBleException
	{
		public NotBondedPairingNotEnabledException()
			: this("Pairing isn't enabled on the device")
		{
		}

		public NotBondedPairingNotEnabledException(string message, Exception? innerException = null)
			: base(message, innerException)
		{
		}

		public override BleManagerConnectionFailure ConvertToConnectionFailure()
		{
			return BleManagerConnectionFailure.BleBondedPairingNotEnabled;
		}
	}
}
