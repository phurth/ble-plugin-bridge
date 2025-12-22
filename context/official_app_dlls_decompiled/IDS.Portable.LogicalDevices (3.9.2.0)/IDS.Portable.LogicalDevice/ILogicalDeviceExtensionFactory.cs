namespace IDS.Portable.LogicalDevice
{
	internal interface ILogicalDeviceExtensionFactory
	{
		ILogicalDeviceEx MakeLogicalDeviceEx(ILogicalDevice logicalDevice);
	}
	internal interface ILogicalDeviceExtensionFactory<in TLogicalDevice> where TLogicalDevice : class, ILogicalDevice
	{
		ILogicalDeviceEx MakeLogicalDeviceEx(TLogicalDevice logicalDevice);
	}
}
