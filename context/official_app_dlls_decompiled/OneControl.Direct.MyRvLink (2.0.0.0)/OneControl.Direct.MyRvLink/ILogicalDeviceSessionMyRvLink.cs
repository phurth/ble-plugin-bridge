using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public interface ILogicalDeviceSessionMyRvLink : ILogicalDeviceSession
	{
		bool IsActivated { get; }
	}
}
