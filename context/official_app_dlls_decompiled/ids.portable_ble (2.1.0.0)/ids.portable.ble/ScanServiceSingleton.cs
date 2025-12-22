using System;
using ids.portable.ble.BleScanner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;

namespace ids.portable.ble
{
	internal class ScanServiceSingleton : IMauiInitializeService
	{
		public static IBleScannerService? BleScannerServiceSingleton { get; private set; }

		public void Initialize(IServiceProvider services)
		{
			BleScannerServiceSingleton = ServiceProviderServiceExtensions.GetRequiredService<IBleScannerService>(services);
		}
	}
}
