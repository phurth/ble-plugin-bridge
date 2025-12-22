using System;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace ids.portable.ble.BleManager
{
	public class BondErrorEventArgs : DeviceEventArgs
	{
		private Guid _id;

		public Guid Id => Device?.Id ?? _id;

		public BondErrorEventArgs(IDevice device)
		{
			Device = device;
		}

		public BondErrorEventArgs(Guid id)
		{
			_id = id;
			Device = null;
		}
	}
}
