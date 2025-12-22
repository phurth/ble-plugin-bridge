namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceExCircuit : ILogicalDeviceExOnline, ILogicalDeviceEx
	{
		void LogicalDeviceCircuitIdChanged(ILogicalDevice logicalDevice);
	}
}
