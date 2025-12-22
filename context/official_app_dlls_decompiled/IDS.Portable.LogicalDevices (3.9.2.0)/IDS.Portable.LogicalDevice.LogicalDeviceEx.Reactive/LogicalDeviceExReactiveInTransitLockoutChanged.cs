namespace IDS.Portable.LogicalDevice.LogicalDeviceEx.Reactive
{
	public class LogicalDeviceExReactiveInTransitLockoutChanged : LogicalDeviceExReactive, ILogicalDeviceExInTransitLockout, ILogicalDeviceExOnline, ILogicalDeviceEx
	{
		protected override string LogTag => "LogicalDeviceExReactiveInTransitLockoutChanged";

		public static LogicalDeviceExReactiveInTransitLockoutChanged? SharedExtension => LogicalDeviceExBase<ILogicalDevice>.GetSharedExtension<LogicalDeviceExReactiveInTransitLockoutChanged>(autoCreate: true);

		public static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExBase<ILogicalDevice>.LogicalDeviceExFactory<LogicalDeviceExReactiveInTransitLockoutChanged>(logicalDevice, GetLogicalDeviceScope);
		}

		protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExScope.Shared;
		}

		public void LogicalDeviceInTransitLockoutChanged(ILogicalDevice logicalDevice)
		{
			TryAllOnNext(logicalDevice);
		}
	}
}
