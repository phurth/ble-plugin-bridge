namespace IDS.Portable.LogicalDevice.LogicalDeviceEx.Reactive
{
	public abstract class LogicalDeviceExReactive : LogicalDeviceExReactiveBase
	{
		protected override string LogTag => "LogicalDeviceExReactive";

		public override void LogicalDeviceDetached(ILogicalDevice logicalDevice)
		{
			base.LogicalDeviceDetached(logicalDevice);
			TryAllOnNext(logicalDevice);
		}

		public override void LogicalDeviceOnlineChanged(ILogicalDevice logicalDevice)
		{
			base.LogicalDeviceOnlineChanged(logicalDevice);
			TryAllOnNext(logicalDevice);
		}
	}
}
