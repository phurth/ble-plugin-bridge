using System;
using System.Reflection;
using ids.portable.ble.Ble;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.TankSensor.Mopeka;
using OneControl.Direct.IdsCanAccessoryBle.Connections;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public static class LogicalDeviceServiceExtension
	{
		public static void AccessoryRegistration(this ILogicalDeviceService deviceService, IBleService bleService, Func<ILPSettingsRepository> lpSettingsRepositoryFactory)
		{
			JsonSerializer.AutoRegisterJsonSerializersFromAssembly(Assembly.GetExecutingAssembly());
			bleService.Scanner.FactoryRegistry.Register(new IdsCanAccessoryBleScanResultPrimaryServiceFactory(bleService));
			Singleton<AccessoryRegistrationManager>.Instance.RegisterFactory(new SensorConnectionAwningSensorFactory(bleService, deviceService));
			Singleton<AccessoryRegistrationManager>.Instance.RegisterFactory(new SensorConnectionBatteryMonitorFactory(bleService, deviceService));
			Singleton<AccessoryRegistrationManager>.Instance.RegisterFactory(new SensorConnectionDoorLockFactory(bleService, deviceService));
			Singleton<AccessoryRegistrationManager>.Instance.RegisterFactory(new SensorConnectionEchoBrakeControlFactory(bleService, deviceService));
			Singleton<AccessoryRegistrationManager>.Instance.RegisterFactory(new SensorConnectionTirePressureMonitorFactory(bleService, deviceService));
			Singleton<AccessoryRegistrationManager>.Instance.RegisterFactory(new SensorConnectionMopekaFactory(bleService, deviceService, lpSettingsRepositoryFactory));
			Singleton<AccessoryRegistrationManager>.Instance.RegisterFactory(new SensorConnectionTemperatureFactory(bleService, deviceService));
			Singleton<AccessoryRegistrationManager>.Instance.RegisterFactory(new SensorConnectionTankSensorFactory(bleService, deviceService));
			Singleton<AccessoryRegistrationManager>.Instance.RegisterFactory(new SensorConnectionTpmsFactory(bleService, deviceService));
		}
	}
}
