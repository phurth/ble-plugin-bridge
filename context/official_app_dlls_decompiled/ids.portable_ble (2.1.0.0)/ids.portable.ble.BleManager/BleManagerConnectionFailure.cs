namespace ids.portable.ble.BleManager
{
	public enum BleManagerConnectionFailure : byte
	{
		None = 0,
		Timeout = 1,
		OperationCanceled = 2,
		DeviceNotFound = 3,
		DeviceUnlockFailure = 4,
		DeviceUnlockCanceled = 5,
		Other = 127,
		BleDeviceNotBonded = 128,
		BleBondedPairingNotEnabled = 129,
		BlePeripheralLostBondInfo = 130,
		BleTooManyConnections = 131
	}
}
