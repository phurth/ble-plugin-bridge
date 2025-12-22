using System;
using System.Runtime.CompilerServices;
using ids.portable.ble.BleManager;
using ids.portable.ble.ScanResults;

namespace ids.portable.ble.Exceptions
{
	public class BleManagerConnectionDeviceTooManyConnectionsException : BleManagerConnectionException, IConnectionFailureBleException
	{
		private IBleScanResult ScanResult { get; }

		public BleManagerConnectionDeviceTooManyConnectionsException(IBleScanResult scanResult, Exception? innerException = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(50, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Unable to connect to ");
			defaultInterpolatedStringHandler.AppendFormatted(scanResult);
			defaultInterpolatedStringHandler.AppendLiteral(" because too many connections");
			base._002Ector(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
			ScanResult = scanResult;
		}

		public override BleManagerConnectionFailure ConvertToConnectionFailure()
		{
			return BleManagerConnectionFailure.BleTooManyConnections;
		}
	}
}
