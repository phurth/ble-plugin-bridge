namespace IDS.Portable.LogicalDevice.LogicalDeviceEx.Reactive
{
	public class LogicalDeviceExReactiveStatusChanged : LogicalDeviceExReactive, ILogicalDeviceExStatus, ILogicalDeviceExOnline, ILogicalDeviceEx
	{
		protected override string LogTag => "LogicalDeviceExReactiveStatusChanged";

		public static LogicalDeviceExReactiveStatusChanged? SharedExtension => LogicalDeviceExBase<ILogicalDevice>.GetSharedExtension<LogicalDeviceExReactiveStatusChanged>(autoCreate: true);

		public static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExBase<ILogicalDevice>.LogicalDeviceExFactory<LogicalDeviceExReactiveStatusChanged>(logicalDevice, GetLogicalDeviceScope);
		}

		protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExScope.Shared;
		}

		public void LogicalDeviceStatusChanged(ILogicalDevice logicalDevice)
		{
			TryAllOnNext(logicalDevice);
		}
	}
}
