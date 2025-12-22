using System;

namespace ids.portable.ble.BleManager
{
	[Flags]
	public enum BleStreamOption : ushort
	{
		None = 0,
		WriteWithoutResponse = 1,
		DisableDisconnectDeviceOnClose = 2
	}
}
