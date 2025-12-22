using System;
using ids.portable.ble.BleManager;
using ids.portable.ble.Exceptions;
using IDS.Portable.LogicalDevice.LogicalDeviceSource.ConnectionFailure;

namespace OneControl.Direct.MyRvLinkBle
{
	public class DirectConnectionMyRvLinkBleConnectionFailure : ConnectionFailureManager<BleManagerConnectionFailure>
	{
		public override BleManagerConnectionFailure GetConnectionFailure(Exception? lastFailureException)
		{
			if (lastFailureException != null)
			{
				if (!(lastFailureException is TimeoutException))
				{
					if (!(lastFailureException is OperationCanceledException))
					{
						if (lastFailureException is IConnectionFailureBleException ex)
						{
							return ex.ConvertToConnectionFailure();
						}
						return BleManagerConnectionFailure.Other;
					}
					return BleManagerConnectionFailure.OperationCanceled;
				}
				return BleManagerConnectionFailure.Timeout;
			}
			return BleManagerConnectionFailure.None;
		}
	}
}
