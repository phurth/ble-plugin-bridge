using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Android.Bluetooth;
using Android.Content;
using IDS.Portable.Common;

namespace ids.portable.ble.Platforms.Android
{
	internal class BleBondingRequest : BroadcastReceiverAsync<bool>
	{
		private readonly BluetoothDevice? _device;

		private bool _bonding;

		private const string Tag = "BleBondingRequest";

		public BleBondingRequest(BluetoothDevice device, Context context, CancellationToken cancellationToken)
			: base(context, cancellationToken, new IntentFilter("android.bluetooth.device.action.BOND_STATE_CHANGED"))
		{
			_device = device;
			if (_device == null)
			{
				_tcs.TrySetResult(false);
			}
			else if (_device!.BondState == Bond.Bonded)
			{
				_tcs.TrySetResult(true);
			}
			else if (!_device!.CreateBond())
			{
				_tcs.TrySetResult(false);
			}
		}

		protected override bool TryOnReceive(Context? context, Intent? intent, out bool result)
		{
			try
			{
				BluetoothDevice bluetoothDevice = intent?.Extras?.Get("android.bluetooth.device.extra.DEVICE") as BluetoothDevice;
				BluetoothDevice? device = _device;
				result = device != null && device!.BondState == Bond.Bonded;
				if (!string.Equals(_device?.Address, bluetoothDevice?.Address, StringComparison.Ordinal))
				{
					return result;
				}
				Bond bond = (Bond)(intent?.GetIntExtra("android.bluetooth.device.extra.BOND_STATE", 10) ?? 10);
				if (bond == Bond.Bonding)
				{
					_bonding = true;
				}
				if (_bonding && bond == Bond.None)
				{
					return true;
				}
				return result;
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(61, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Exception occurred while receiving bluetooth bonding intent: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex);
				TaggedLog.Error("BleBondingRequest", defaultInterpolatedStringHandler.ToStringAndClear());
				result = false;
			}
			return result;
		}
	}
}
