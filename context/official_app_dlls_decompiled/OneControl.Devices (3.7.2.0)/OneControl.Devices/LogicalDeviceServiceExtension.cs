using System.Reflection;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.AccessoryGateway;
using OneControl.Devices.AwningSensor;
using OneControl.Devices.BatteryMonitor;
using OneControl.Devices.BluetoothGateway;
using OneControl.Devices.BootLoader;
using OneControl.Devices.BrakingSystem;
using OneControl.Devices.DoorLock;
using OneControl.Devices.EchoBrakeControl;
using OneControl.Devices.Fan;
using OneControl.Devices.FlicButton;
using OneControl.Devices.OneControlTouchPanel;
using OneControl.Devices.TemperatureSensor;
using OneControl.Devices.TextDevice;

namespace OneControl.Devices
{
	public static class LogicalDeviceServiceExtension
	{
		public static void RegisterAllLogicalDeviceFactories(this ILogicalDeviceService deviceService)
		{
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceUnknownRemoteFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceTextFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceOneControlTouchPanelFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceBrakingSystemFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceFanFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceCloudGatewayFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceBluetoothGatewayFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceTemperatureSensorFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceAwningSensorFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceAccessoryGatewayFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceChassisInfoFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceMonitorPanelFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceHourMeterFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceGeneratorGenieFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceLevelerFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceClimateZoneFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceLightDimmableFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceLightRgbFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceTankSensorFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceRelayHBridgeFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceRelayBasicFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceLightFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceRelayGeneratorFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceBatteryMonitorFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceDoorLockFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceEchoBrakeControlFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceReflashBootLoaderFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceLeveler5TouchpadFactory());
			deviceService.RegisterLogicalDeviceFactory(new LogicalDeviceFlicButtonFactory());
			deviceService.RegisterLogicalDeviceExFactory(LogicalDeviceLightMasterSwitchControllableIdsCanEx.LogicalDeviceExFactory);
			deviceService.RegisterLogicalDeviceExFactory(LogicalDeviceLightMasterSwitchControllableMyRvLinkEx.LogicalDeviceExFactory);
			deviceService.RegisterLogicalDeviceExFactory(LogicalDeviceRvKindEx.LogicalDeviceExFactory);
			deviceService.RegisterLogicalDeviceExFactory(LogicalDeviceExManifest.LogicalDeviceExFactory);
			JsonSerializer.AutoRegisterJsonSerializersFromAssembly(Assembly.GetExecutingAssembly());
		}
	}
}
