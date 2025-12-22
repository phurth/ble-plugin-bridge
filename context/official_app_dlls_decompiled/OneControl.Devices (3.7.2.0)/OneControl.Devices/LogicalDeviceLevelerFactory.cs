using IDS.Portable.LogicalDevice;
using OneControl.Devices.Leveler.Type5;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			switch ((byte)logicalDeviceId.DeviceType)
			{
			case 7:
				return new LogicalDeviceLevelerType1(logicalDeviceId, service);
			case 17:
				return new LogicalDeviceLevelerType3(logicalDeviceId, service);
			case 40:
			{
				LogicalDeviceLevelerCapabilityType4 levelerCapability2 = new LogicalDeviceLevelerCapabilityType4(rawCapability);
				return new LogicalDeviceLevelerType4(logicalDeviceId, levelerCapability2, service);
			}
			case 56:
			{
				LogicalDeviceLevelerCapabilityType5 levelerCapability = new LogicalDeviceLevelerCapabilityType5(rawCapability);
				return new LogicalDeviceLevelerType5(logicalDeviceId, levelerCapability, service);
			}
			default:
				return null;
			}
		}
	}
}
