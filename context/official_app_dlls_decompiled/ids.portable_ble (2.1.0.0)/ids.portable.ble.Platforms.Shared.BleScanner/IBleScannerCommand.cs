using System;
using System.Collections.Generic;
using ids.portable.ble.ScanResults;
using IDS.Portable.Common;

namespace ids.portable.ble.Platforms.Shared.BleScanner
{
	internal interface IBleScannerCommand : ICommonDisposable, IDisposable
	{
		bool IsCompleted { get; }

		void UpdateDevices(IEnumerable<IBleScanResult> devices);

		void UpdateDevice(IBleScanResult device);

		void ScanCompleted();

		void ScanFailed(Exception ex);

		void ScanCanceled();
	}
}
