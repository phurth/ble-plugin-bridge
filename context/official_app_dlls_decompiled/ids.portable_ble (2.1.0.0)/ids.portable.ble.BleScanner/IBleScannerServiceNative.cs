namespace ids.portable.ble.BleScanner
{
	internal interface IBleScannerServiceNative
	{
		bool IsExplicitServiceUuidScanningSupported { get; }

		int ScanTimeoutMs { get; }

		int ScannerStopDelayMs { get; }
	}
}
