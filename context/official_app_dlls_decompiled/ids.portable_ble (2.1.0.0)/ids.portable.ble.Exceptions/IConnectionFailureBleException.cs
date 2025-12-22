using ids.portable.ble.BleManager;

namespace ids.portable.ble.Exceptions
{
	public interface IConnectionFailureBleException
	{
		BleManagerConnectionFailure ConvertToConnectionFailure();
	}
}
