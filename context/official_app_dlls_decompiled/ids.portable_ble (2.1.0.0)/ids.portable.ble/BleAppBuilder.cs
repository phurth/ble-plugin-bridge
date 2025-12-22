using System;
using System.Threading.Tasks;
using ids.portable.ble.Ble;
using ids.portable.ble.BleAdapter;
using ids.portable.ble.BleManager;
using ids.portable.ble.BleScanner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

namespace ids.portable.ble
{
	public sealed class BleAppBuilder
	{
		private Func<IServiceProvider, Task<bool>>? _isBluetoothPermissionGrantedAsync;

		internal BleAppBuilder(MauiAppBuilder builder)
		{
			ServiceCollectionDescriptorExtensions.TryAddEnumerable(builder.Services, ServiceDescriptor.Singleton<IMauiInitializeService, ScanServiceSingleton>());
			ServiceCollectionServiceExtensions.AddSingleton(builder.Services, (IServiceProvider _) => CrossBluetoothLE.Current);
			ServiceCollectionServiceExtensions.AddSingleton<IBleManager, ids.portable.ble.BleManager.BleManager>(builder.Services);
			ServiceCollectionServiceExtensions.AddSingleton<IBleScannerService, BleScannerService>(builder.Services);
			ServiceCollectionServiceExtensions.AddSingleton<IBleService, BleService>(builder.Services);
			ServiceCollectionServiceExtensions.AddSingleton(builder.Services, (Func<IServiceProvider, IBleAdapterService>)((IServiceProvider serviceProvider) => new BleAdapterServices(serviceProvider, ServiceProviderServiceExtensions.GetRequiredService<IBluetoothLE>(serviceProvider), ServiceProviderServiceExtensions.GetRequiredService<ILogger<BleAdapterServices>>(serviceProvider), IsBluetoothPermissionGrantedAsync)));
		}

		public BleAppBuilder IsBluetoothPermissionGrantedAsync(Func<IServiceProvider, Task<bool>> isBluetoothPermissionGrantedAsync)
		{
			_isBluetoothPermissionGrantedAsync = isBluetoothPermissionGrantedAsync;
			return this;
		}

		private Task<bool> IsBluetoothPermissionGrantedAsync(IServiceProvider serviceProvider)
		{
			if (_isBluetoothPermissionGrantedAsync != null)
			{
				return _isBluetoothPermissionGrantedAsync!(serviceProvider);
			}
			ILogger<BleAdapterServices> service = ServiceProviderServiceExtensions.GetService<ILogger<BleAdapterServices>>(serviceProvider);
			if (service != null)
			{
				LoggerExtensions.LogWarning(service, "No implementation supplied to check Bluetooth Permission - default of \"true\" will be used");
			}
			return Task.FromResult(true);
		}
	}
}
