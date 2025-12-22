namespace IDS.Portable.LogicalDevice.LogicalDeviceEx.Reactive
{
	public class LogicalDeviceExDeviceDetached : LogicalDeviceExReactiveBase
	{
		protected override string LogTag => "LogicalDeviceExDeviceDetached";

		public static LogicalDeviceExDeviceDetached? SharedExtension => LogicalDeviceExBase<ILogicalDevice>.GetSharedExtension<LogicalDeviceExDeviceDetached>(autoCreate: true);

		protected override bool NotifyOnInitialSubscribeOrDeviceAttached => false;

		public static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExBase<ILogicalDevice>.LogicalDeviceExFactory<LogicalDeviceExDeviceDetached>(logicalDevice, GetLogicalDeviceScope);
		}

		protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExScope.Shared;
		}

		public override void LogicalDeviceDetached(ILogicalDevice logicalDevice)
		{
			base.LogicalDeviceDetached(logicalDevice);
			TryAllOnNext(logicalDevice);
		}
	}
}
