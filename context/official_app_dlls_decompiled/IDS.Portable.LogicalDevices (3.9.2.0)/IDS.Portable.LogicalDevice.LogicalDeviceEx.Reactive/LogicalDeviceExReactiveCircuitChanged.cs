namespace IDS.Portable.LogicalDevice.LogicalDeviceEx.Reactive
{
	public class LogicalDeviceExReactiveCircuitChanged : LogicalDeviceExReactive, ILogicalDeviceExCircuit, ILogicalDeviceExOnline, ILogicalDeviceEx
	{
		protected override string LogTag => "LogicalDeviceExReactiveCircuitChanged";

		public static LogicalDeviceExReactiveCircuitChanged? SharedExtension => LogicalDeviceExBase<ILogicalDevice>.GetSharedExtension<LogicalDeviceExReactiveCircuitChanged>(autoCreate: true);

		public static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExBase<ILogicalDevice>.LogicalDeviceExFactory<LogicalDeviceExReactiveCircuitChanged>(logicalDevice, GetLogicalDeviceScope);
		}

		protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExScope.Shared;
		}

		public void LogicalDeviceCircuitIdChanged(ILogicalDevice logicalDevice)
		{
			TryAllOnNext(logicalDevice);
		}
	}
}
