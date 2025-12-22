namespace IDS.Portable.LogicalDevice
{
	public class PhysicalDeviceNotFoundException : LogicalDeviceException
	{
		public PhysicalDeviceNotFoundException(string tag, ILogicalDevice logicalDevice, string message = "")
			: base(tag + " - Physical device for " + (logicalDevice?.DeviceName ?? "Unknown") + " isn't available." + message)
		{
		}

		public PhysicalDeviceNotFoundException(string tag, string message)
			: base(tag + " - Physical device isn't available, " + message)
		{
		}
	}
}
