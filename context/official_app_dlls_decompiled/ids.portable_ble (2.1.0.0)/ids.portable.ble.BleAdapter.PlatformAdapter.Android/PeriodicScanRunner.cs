using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Runtime;
using AndroidX.Work;
using ids.portable.ble.BleScanner;
using Plugin.BLE;
using Plugin.BLE.Android;

namespace ids.portable.ble.BleAdapter.PlatformAdapter.Android
{
	[BroadcastReceiver(Enabled = true, Exported = true)]
	[IntentFilter(new string[] { "com.ids.ble.BLE_PERIODIC_SCAN" })]
	public class PeriodicScanRunner : Worker
	{
		private readonly Adapter _bleAdapter;

		public const string Action = "com.ids.ble.BLE_PERIODIC_SCAN";

		public PeriodicScanRunner(IntPtr javaReference, JniHandleOwnership transfer)
			: base(javaReference, transfer)
		{
			_bleAdapter = CrossBluetoothLE.Current.Adapter as Adapter;
		}

		public PeriodicScanRunner(Context context, WorkerParameters workerParams)
			: base(context, workerParams)
		{
			_bleAdapter = CrossBluetoothLE.Current.Adapter as Adapter;
		}

		public override Result DoWork()
		{
			IBleScannerService bleScannerServiceSingleton = ScanServiceSingleton.BleScannerServiceSingleton;
			if (bleScannerServiceSingleton == null)
			{
				Console.WriteLine("PeriodicScanRunner: ScanServiceSingleton.BleScannerServiceSingleton is null.");
				return Result.InvokeFailure();
			}
			if (_bleAdapter.IsScanning)
			{
				return Result.InvokeFailure();
			}
			try
			{
				bleScannerServiceSingleton.Stop();
				Task.Delay(TimeSpan.FromSeconds(6.0), CancellationToken.None).GetAwaiter().GetResult();
				bleScannerServiceSingleton.Start(filterUsingExplicitServiceUuids: true);
				return Result.InvokeSuccess();
			}
			catch (Exception ex)
			{
				Console.WriteLine("An error occurred in the PeriodicScanRunner. Exception: " + ex.Message + ". Stacktrace: " + ex.StackTrace);
				return Result.InvokeFailure();
			}
		}
	}
}
