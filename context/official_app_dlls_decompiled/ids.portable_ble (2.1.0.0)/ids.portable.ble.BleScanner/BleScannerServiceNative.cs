namespace ids.portable.ble.BleScanner
{
	internal abstract class BleScannerServiceNative : IBleScannerServiceNative
	{
		public virtual bool IsExplicitServiceUuidScanningSupported { get; }

		public virtual int ScanTimeoutMs { get; }

		public virtual int ScannerStopDelayMs { get; }
	}
}
