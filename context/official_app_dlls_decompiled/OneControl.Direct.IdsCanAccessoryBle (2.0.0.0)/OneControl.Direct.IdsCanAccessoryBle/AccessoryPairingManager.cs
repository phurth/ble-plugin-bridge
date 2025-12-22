using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using ids.portable.ble.Platforms.Shared.BleScanner;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDeviceEx.Reactive;
using OneControl.Devices.AccessoryGateway;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public class AccessoryPairingManager : IAccessoryPairingManager
	{
		private readonly IBleService _bleService;

		public const string LogTag = "AccessoryPairingManager";

		private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

		private static readonly TimeSpan ScanDelayInterval = TimeSpan.FromSeconds(4.0);

		private const int MaximumScanAttempts = 6;

		private const int PairDevicesWatchdogTimeout = 10000;

		private readonly Watchdog _pairDevicesWatchdog;

		private readonly IDisposable? _subscription;

		public TimeSpan PairWithRvTimeout => TimeSpan.FromSeconds(90.0);

		public TimeSpan UnpairWithRvTimeout => TimeSpan.FromSeconds(90.0);

		public ILogicalDeviceManager LogicalDeviceManager { get; }

		public IAccessoryRegistrationManager AccessoryRegistrationManager { get; }

		public AccessoryPairingManager(IBleService bleService, ILogicalDeviceManager logicalDeviceManager, IAccessoryRegistrationManager accessoryRegistrationManager)
		{
			_bleService = bleService;
			LogicalDeviceManager = logicalDeviceManager;
			AccessoryRegistrationManager = accessoryRegistrationManager;
			_pairDevicesWatchdog = new Watchdog(10000, delegate
			{
				TryPairLinkedDevicesWithPhone(CancellationToken.None);
			}, autoStartOnFirstPet: true);
			LogicalDeviceExReactiveOnlineChanged? sharedExtension = LogicalDeviceExReactiveOnlineChanged.SharedExtension;
			_subscription = ((sharedExtension != null) ? ObservableExtensions.Subscribe(Observable.Where(Observable.OfType<ILogicalDeviceAccessory>(sharedExtension), (ILogicalDeviceAccessory device) => device.ActiveConnection != 0 && device.IsAccessoryGatewaySupported), delegate
			{
				_pairDevicesWatchdog.TryPet(autoReset: true);
			}) : null);
		}

		public ILogicalDeviceAccessoryGateway? GetAccessoryGatewayAssociatedWithRv()
		{
			List<ILogicalDeviceAccessoryGateway> list = Enumerable.ToList(Enumerable.OrderBy(LogicalDeviceManager.FindLogicalDevices((ILogicalDeviceAccessoryGateway logicalDevice) => logicalDevice.ActiveConnection != LogicalDeviceActiveConnection.Offline), (ILogicalDeviceAccessoryGateway it) => it.LogicalId.ProductMacAddress));
			HashSet<ILogicalDeviceAccessoryGateway> hashSet = new HashSet<ILogicalDeviceAccessoryGateway>();
			foreach (ILogicalDeviceSourceDirect deviceSource in LogicalDeviceManager.DeviceService.DeviceSourceManager.DeviceSources)
			{
				foreach (ILogicalDeviceAccessoryGateway item in list)
				{
					if (item.IsAssociatedWithDeviceSource(deviceSource))
					{
						hashSet.Add(item);
					}
				}
			}
			if (hashSet.Count > 1)
			{
				TaggedLog.Warning("AccessoryPairingManager", "Found more than 1 accessory gateway on the current RV connection. We do not support multiple accessory gateways");
			}
			return Enumerable.FirstOrDefault(hashSet);
		}

		public async Task<bool> IsPairedWithRv(ILogicalDeviceAccessory? device, ILogicalDeviceAccessoryGateway? accessoryGateway, CancellationToken token)
		{
			if ((object)device?.LogicalId.ProductMacAddress == null)
			{
				return false;
			}
			if (!device!.IsAccessoryGatewaySupported)
			{
				return false;
			}
			if (accessoryGateway == null)
			{
				return false;
			}
			if (accessoryGateway!.ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				return false;
			}
			if (!(await accessoryGateway!.IsDeviceLinkedAsync(device!.Product?.MacAddress, token)))
			{
				return false;
			}
			return true;
		}

		public Task<bool> IsPairedOverBle(ILogicalDeviceAccessory? device, CancellationToken token)
		{
			if (device == null || !device!.IsAccessoryGatewaySupported)
			{
				return Task.FromResult(false);
			}
			foreach (ILogicalDeviceSourceDirect deviceSource2 in LogicalDeviceManager.DeviceService.DeviceSourceManager.DeviceSources)
			{
				if (deviceSource2 is ILogicalDeviceSourceDirectIdsAccessory deviceSource && device!.IsAssociatedWithDeviceSource(deviceSource))
				{
					return Task.FromResult(true);
				}
			}
			return Task.FromResult(false);
		}

		public async Task<bool> PairWithRv(ILogicalDeviceAccessory? device, ILogicalDeviceAccessoryGateway? accessoryGateway, CancellationToken token)
		{
			if (device?.LogicalId.ProductMacAddress == null)
			{
				return false;
			}
			if (accessoryGateway == null)
			{
				return false;
			}
			if (!device!.IsAccessoryGatewaySupported)
			{
				return false;
			}
			try
			{
				if (await IsPairedWithRv(device, accessoryGateway, token))
				{
					return true;
				}
				if (await accessoryGateway!.LinkDeviceAsync(device!.Product?.MacAddress, token) != 0)
				{
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("AccessoryPairingManager", "PairWithRv failed because " + ex.Message);
				return false;
			}
		}

		public async Task<bool> UnpairWithRv(ILogicalDeviceAccessory? device, ILogicalDeviceAccessoryGateway? accessoryGateway, CancellationToken token)
		{
			_ = 1;
			try
			{
				if (device?.LogicalId.ProductMacAddress == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Accessory must have a MAC address ");
					defaultInterpolatedStringHandler.AppendFormatted(device);
					throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				if (accessoryGateway == null)
				{
					return false;
				}
				if (!device!.IsAccessoryGatewaySupported)
				{
					return false;
				}
				if (await accessoryGateway!.UnlinkDeviceAsync(device!.Product?.MacAddress, token) != 0)
				{
					return false;
				}
				List<ILogicalDeviceSourceDirectRemoveOfflineDevices> list = device!.DeviceService.DeviceSourceManager.FindDeviceSources<ILogicalDeviceSourceDirectRemoveOfflineDevices>(device!.IsAssociatedWithDeviceSource);
				foreach (ILogicalDeviceSourceDirect deviceSource in LogicalDeviceManager.DeviceService.DeviceSourceManager.DeviceSources)
				{
					if (device!.IsAssociatedWithDeviceSource(deviceSource) && accessoryGateway!.IsAssociatedWithDeviceSource(deviceSource))
					{
						device!.RemoveDeviceSource(deviceSource);
						break;
					}
				}
				List<Task> list2 = new List<Task>();
				foreach (ILogicalDeviceSourceDirectRemoveOfflineDevices item in list)
				{
					if (token.IsCancellationRequested)
					{
						TaggedLog.Warning("AccessoryPairingManager", "UnpairWithRv operation was cancelled by the cancellation token");
						return false;
					}
					try
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 2);
						defaultInterpolatedStringHandler.AppendLiteral("RemoveOfflineDevices from ");
						defaultInterpolatedStringHandler.AppendFormatted(item.GetType());
						defaultInterpolatedStringHandler.AppendLiteral(" ");
						defaultInterpolatedStringHandler.AppendFormatted(item);
						TaggedLog.Debug("AccessoryPairingManager", defaultInterpolatedStringHandler.ToStringAndClear());
						list2.Add(item.RemoveOfflineDevicesAsync(token));
					}
					catch (Exception ex)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 3);
						defaultInterpolatedStringHandler.AppendLiteral("Unable to Remove Offline Devices From ");
						defaultInterpolatedStringHandler.AppendFormatted(item.GetType());
						defaultInterpolatedStringHandler.AppendLiteral(" ");
						defaultInterpolatedStringHandler.AppendFormatted(item);
						defaultInterpolatedStringHandler.AppendLiteral(": ");
						defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
						TaggedLog.Debug("AccessoryPairingManager", defaultInterpolatedStringHandler.ToStringAndClear());
					}
				}
				await Task.WhenAll(list2);
				return true;
			}
			catch (Exception ex2)
			{
				TaggedLog.Warning("AccessoryPairingManager", "UnpairWithRv failed because " + ex2.Message);
				return false;
			}
		}

		public async Task<bool> ResyncAccessoryGatewayDevices(CancellationToken token)
		{
			_ = 1;
			try
			{
				await _lock.WaitAsync(token);
				ILogicalDeviceAccessoryGateway accessoryGatewayAssociatedWithRv = GetAccessoryGatewayAssociatedWithRv();
				if (accessoryGatewayAssociatedWithRv == null)
				{
					return false;
				}
				return await accessoryGatewayAssociatedWithRv.ResyncDevicesAsync(token);
			}
			finally
			{
				_lock.Release();
			}
		}

		public async Task TryPairLinkedDevicesWithPhone(CancellationToken cancellationToken)
		{
			_ = 3;
			try
			{
				await _lock.WaitAsync(cancellationToken);
				TaggedLog.Information("AccessoryPairingManager", "Attempting to auto pair any accessories with the phone.");
				List<ILogicalDeviceAccessory> list = Enumerable.ToList(Enumerable.OrderBy(LogicalDeviceManager.FindLogicalDevices((ILogicalDeviceAccessory it) => it.ActiveConnection != 0 && it.IsAccessoryGatewaySupported), (ILogicalDeviceAccessory it) => it.LogicalId.ProductMacAddress));
				List<MAC> unpairedMacs = new List<MAC>();
				foreach (ILogicalDeviceAccessory device in list)
				{
					if (!(await IsPairedOverBle(device, cancellationToken)) && (object)device.Product?.MacAddress != null)
					{
						unpairedMacs.Add(device.Product!.MacAddress);
					}
				}
				if (unpairedMacs.Count == 0)
				{
					TaggedLog.Information("AccessoryPairingManager", "Found no linked devices that aren't already paired with the phone.");
					return;
				}
				HashSet<IdsCanAccessoryScanResult> scanResults = new HashSet<IdsCanAccessoryScanResult>();
				for (int scanAttempt = 0; scanAttempt < 6; scanAttempt++)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						break;
					}
					await _bleService.Scanner.TryGetDevicesAsync(delegate(BleScanResultOperation _, IdsCanAccessoryScanResult scanResult)
					{
						scanResults.Add(scanResult);
					}, delegate(IdsCanAccessoryScanResult scanResult)
					{
						MAC mAC = scanResult?.AccessoryMacAddress;
						if ((object)mAC == null)
						{
							return BleScannerCommandControl.Skip;
						}
						if (!unpairedMacs.Contains(mAC))
						{
							return BleScannerCommandControl.Skip;
						}
						return (scanResult?.GetAccessoryStatus(mAC)).HasValue ? BleScannerCommandControl.Include : BleScannerCommandControl.Skip;
					}, cancellationToken);
					if (scanResults.Count == unpairedMacs.Count)
					{
						break;
					}
					await TaskExtension.TryDelay(ScanDelayInterval, cancellationToken);
				}
				foreach (IdsCanAccessoryScanResult item in scanResults)
				{
					if ((object)item.AccessoryMacAddress == null)
					{
						TaggedLog.Information("AccessoryPairingManager", "Scan result mac address is null.");
						continue;
					}
					IdsCanAccessoryStatus? accessoryStatus = item.GetAccessoryStatus(item.AccessoryMacAddress);
					if (!accessoryStatus.HasValue)
					{
						TaggedLog.Information("AccessoryPairingManager", "Accessory status is null.");
						continue;
					}
					bool flag = AccessoryRegistrationManager.TryAddSensorConnection(item, requestSave: true);
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Attempted auto link of ");
					defaultInterpolatedStringHandler.AppendFormatted(accessoryStatus.Value.DeviceType);
					defaultInterpolatedStringHandler.AppendLiteral(", success: ");
					defaultInterpolatedStringHandler.AppendFormatted(flag);
					TaggedLog.Information("AccessoryPairingManager", defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(58, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Problem pairing accessory gateway devices with the phone: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex);
				TaggedLog.Error("AccessoryPairingManager", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			finally
			{
				_lock.Release();
			}
		}
	}
}
