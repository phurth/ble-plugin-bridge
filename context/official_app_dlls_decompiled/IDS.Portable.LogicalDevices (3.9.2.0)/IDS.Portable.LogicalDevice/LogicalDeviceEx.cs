using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public abstract class LogicalDeviceEx<TLogicalDevice> : ILogicalDeviceEx where TLogicalDevice : class, ILogicalDevice
	{
		private readonly object _locker = new object();

		protected abstract string LogTag { get; }

		public TLogicalDevice? LogicalDevice { get; private set; }

		protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExScope.Device;
		}

		protected static ILogicalDeviceEx? LogicalDeviceExFactory<TExtension>(ILogicalDevice logicalDevice) where TExtension : LogicalDeviceEx<TLogicalDevice>, new()
		{
			if (!(logicalDevice is TLogicalDevice))
			{
				return null;
			}
			return new TExtension();
		}

		protected virtual void LogicalDeviceChanged(TLogicalDevice? fromLogicalDevice, TLogicalDevice? toLogicalDevice)
		{
		}

		public void LogicalDeviceAttached(ILogicalDevice logicalDevice)
		{
			lock (_locker)
			{
				if (!(logicalDevice is TLogicalDevice logicalDevice2))
				{
					TaggedLog.Error(LogTag, $"LogicalDeviceAttached attempting to attach to a Logical Device that doesn't implement {typeof(TLogicalDevice)}: {logicalDevice}");
				}
				else if (logicalDevice != LogicalDevice)
				{
					if (LogicalDevice != null)
					{
						TaggedLog.Error(LogTag, $"LogicalDeviceAttached attempting to attach to a Logical Device {logicalDevice} but the extension is already attached to {LogicalDevice}");
						return;
					}
					TLogicalDevice logicalDevice3 = LogicalDevice;
					LogicalDevice = logicalDevice2;
					LogicalDeviceChanged(logicalDevice3, LogicalDevice);
				}
			}
		}

		public void LogicalDeviceDetached(ILogicalDevice logicalDevice)
		{
			lock (_locker)
			{
				if (logicalDevice != null)
				{
					if (logicalDevice != LogicalDevice)
					{
						TaggedLog.Error(LogTag, "LogicalDeviceDetached attempting to detach from a Logical Device which isn't associated with this extension");
						return;
					}
					TLogicalDevice logicalDevice2 = LogicalDevice;
					LogicalDevice = null;
					LogicalDeviceChanged(logicalDevice2, LogicalDevice);
				}
			}
		}
	}
}
