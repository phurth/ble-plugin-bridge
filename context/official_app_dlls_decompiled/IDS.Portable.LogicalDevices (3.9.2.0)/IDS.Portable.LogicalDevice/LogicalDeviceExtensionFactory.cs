using System;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceExtensionFactory : LogicalDeviceExtensionFactory<ILogicalDevice>, ILogicalDeviceExtensionFactory
	{
		public LogicalDeviceExtensionFactory(Func<ILogicalDevice, ILogicalDeviceEx> factory)
			: base(factory)
		{
		}
	}
	public class LogicalDeviceExtensionFactory<TLogicalDevice> : ILogicalDeviceExtensionFactory<TLogicalDevice> where TLogicalDevice : class, ILogicalDevice
	{
		private readonly Func<TLogicalDevice, ILogicalDeviceEx> _factory;

		public LogicalDeviceExtensionFactory(Func<TLogicalDevice, ILogicalDeviceEx> factory)
		{
			_factory = factory ?? throw new ArgumentNullException("factory");
		}

		public ILogicalDeviceEx MakeLogicalDeviceEx(TLogicalDevice logicalDevice)
		{
			return _factory(logicalDevice);
		}
	}
}
