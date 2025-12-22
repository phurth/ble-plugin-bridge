using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.OneControlTouchPanel
{
	public class LogicalDeviceOneControlTouchPanelFactory : DefaultLogicalDeviceFactory
	{
		public override ILogicalDevice? MakeLogicalDevice(ILogicalDeviceService service, ILogicalDeviceId logicalDeviceId, byte? rawCapability)
		{
			if (!logicalDeviceId.ProductId.IsValid || !logicalDeviceId.DeviceType.IsValid)
			{
				return null;
			}
			if ((byte)logicalDeviceId.DeviceType != 21)
			{
				return null;
			}
			return new LogicalDeviceOneControlTouchPanel(logicalDeviceId, new LogicalDeviceOneControlTouchPanelCapability(rawCapability), service);
		}
	}
}
