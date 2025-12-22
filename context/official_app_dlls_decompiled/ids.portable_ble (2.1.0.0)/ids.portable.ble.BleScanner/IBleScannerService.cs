using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ids.portable.ble.Platforms.Shared.BleScanner;
using ids.portable.ble.ScanResults;

namespace ids.portable.ble.BleScanner
{
	public interface IBleScannerService
	{
		IBleScanResultFactoryRegistry FactoryRegistry { get; }

		IEnumerable<IBleScanResult> CachedScanResults { get; }

		event Action<IBleScanResult>? DidReceiveScanResult;

		void Start(bool filterUsingExplicitServiceUuids = false);

		void Stop();

		void ForceDeviceRefresh(Guid deviceId);

		Task<TScanResult?> GetDeviceAsync<TScanResult>(Guid deviceId, CancellationToken cancelToken, string deviceName = "") where TScanResult : class, IBleScanResult;

		Task<TScanResult?> GetDeviceAsync<TScanResult>(string deviceName, CancellationToken cancelToken) where TScanResult : class, IBleScanResult;

		Task<TScanResult?> TryGetDeviceAsync<TScanResult>(Guid deviceId, CancellationToken cancelToken, string deviceName = "") where TScanResult : class, IBleScanResult;

		Task<TScanResult?> TryGetDeviceAsync<TScanResult>(string deviceName, CancellationToken cancelToken) where TScanResult : class, IBleScanResult;

		Task GetDevicesAsync<TScanResult>(Action<BleScanResultOperation, TScanResult> deviceScanned, Func<TScanResult, BleScannerCommandControl> filter, CancellationToken cancelToken) where TScanResult : IBleScanResult;

		Task TryGetDevicesAsync<TScanResult>(Action<BleScanResultOperation, TScanResult> deviceScanned, Func<TScanResult, BleScannerCommandControl> filter, CancellationToken cancelToken) where TScanResult : IBleScanResult;
	}
}
