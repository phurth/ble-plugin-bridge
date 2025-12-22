namespace IDS.Portable.LogicalDevice.LogicalDeviceEx.Reactive
{
	public class LogicalDeviceExReactiveAlertChanged : LogicalDeviceExReactive, ILogicalDeviceExAlertChanged, ILogicalDeviceExOnline, ILogicalDeviceEx
	{
		protected override string LogTag => "LogicalDeviceExReactiveAlertChanged";

		public static LogicalDeviceExReactiveAlertChanged? SharedExtension => LogicalDeviceExBase<ILogicalDevice>.GetSharedExtension<LogicalDeviceExReactiveAlertChanged>(autoCreate: true);

		public static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExBase<ILogicalDevice>.LogicalDeviceExFactory<LogicalDeviceExReactiveAlertChanged>(logicalDevice, GetLogicalDeviceScope);
		}

		protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExScope.Shared;
		}

		public void LogicalDeviceAlertChanged(ILogicalDevice logicalDevice, ILogicalDeviceAlert fromAlert, ILogicalDeviceAlert toAlert)
		{
			TryAllOnNext(logicalDevice);
		}
	}
}
