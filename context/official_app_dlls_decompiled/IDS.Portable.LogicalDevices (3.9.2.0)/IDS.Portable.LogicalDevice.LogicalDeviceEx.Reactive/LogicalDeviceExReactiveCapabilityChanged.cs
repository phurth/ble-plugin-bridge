namespace IDS.Portable.LogicalDevice.LogicalDeviceEx.Reactive
{
	public class LogicalDeviceExReactiveCapabilityChanged : LogicalDeviceExReactive, ILogicalDeviceExCapability, ILogicalDeviceExOnline, ILogicalDeviceEx
	{
		protected override string LogTag => "LogicalDeviceExReactiveCapabilityChanged";

		public static LogicalDeviceExReactiveCapabilityChanged? SharedExtension => LogicalDeviceExBase<ILogicalDevice>.GetSharedExtension<LogicalDeviceExReactiveCapabilityChanged>(autoCreate: true);

		public static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExBase<ILogicalDevice>.LogicalDeviceExFactory<LogicalDeviceExReactiveCapabilityChanged>(logicalDevice, GetLogicalDeviceScope);
		}

		protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExScope.Shared;
		}

		public void LogicalDeviceCapabilityChanged(ILogicalDevice logicalDevice)
		{
			TryAllOnNext(logicalDevice);
		}
	}
}
