using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Android.App;
using Android.Bluetooth.LE;
using Android.Content;
using Android.Runtime;
using Java.Lang;
using Plugin.BLE;
using Plugin.BLE.Android;

namespace ids.portable.ble.BleAdapter.PlatformAdapter.Android
{
	[BroadcastReceiver(Enabled = true, Exported = true)]
	[IntentFilter(new string[] { "ids.portable.ble.BleAdapter.PlatformAdapter.Android.PendingIntentScanReceiver.ReceiveScanResult" })]
	public class PendingIntentScanReceiver : BroadcastReceiver
	{
		public const string Action = "ids.portable.ble.BleAdapter.PlatformAdapter.Android.PendingIntentScanReceiver.ReceiveScanResult";

		private readonly Adapter? _adapter;

		public PendingIntentScanReceiver(IntPtr javaRef, JniHandleOwnership transfer)
			: base(javaRef, transfer)
		{
			_adapter = CrossBluetoothLE.Current.Adapter as Adapter;
		}

		public PendingIntentScanReceiver()
		{
			_adapter = CrossBluetoothLE.Current.Adapter as Adapter;
		}

		public override void OnReceive(Context? context, Intent? intent)
		{
			if (context == null || intent == null || _adapter == null)
			{
				return;
			}
			intent!.GetParcelableArrayListExtra("android.bluetooth.le.extra.LIST_SCAN_RESULT", Class.FromType(typeof(ScanResult)));
			IList parcelableArrayListExtra = intent!.GetParcelableArrayListExtra("android.bluetooth.le.extra.LIST_SCAN_RESULT");
			List<ScanResult> list = ((parcelableArrayListExtra != null) ? Enumerable.ToList(Enumerable.OfType<ScanResult>(parcelableArrayListExtra)) : null);
			int intExtra = intent!.GetIntExtra("android.bluetooth.le.extra.ERROR_CODE", 0);
			if (intExtra != 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 1);
				defaultInterpolatedStringHandler.AppendLiteral("PendingIntentScanReceiver OnReceive failed with error code ");
				defaultInterpolatedStringHandler.AppendFormatted(intExtra);
				Console.WriteLine(defaultInterpolatedStringHandler.ToStringAndClear());
				return;
			}
			if (list == null)
			{
				Console.WriteLine("PendingIntentScanReceiver OnReceive failed, scan results are null");
				return;
			}
			foreach (ScanResult item in list)
			{
				try
				{
					Device device = new Device(_adapter, item.Device, null, item.Rssi, item.ScanRecord?.GetBytes());
					_adapter!.HandleDiscoveredDevice(device);
				}
				catch (System.Exception ex)
				{
					Console.WriteLine("[PendingIntentScanReceiver] Failed to invoke HandleBackgroundScanResult: " + ex.Message);
				}
			}
		}
	}
}
