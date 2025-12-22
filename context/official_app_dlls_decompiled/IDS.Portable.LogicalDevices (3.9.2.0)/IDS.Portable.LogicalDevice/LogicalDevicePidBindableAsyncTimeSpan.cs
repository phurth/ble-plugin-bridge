using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidBindableAsyncTimeSpan : LogicalDevicePidBindableAsyncGeneric<ILogicalDevicePidTimeSpan, TimeSpan>
	{
		protected override Task<TimeSpan> ReadValueAsync(ILogicalDevicePidTimeSpan logicalDevicePid, CancellationToken cancellationToken)
		{
			return logicalDevicePid.ReadTimeSpanAsync(cancellationToken);
		}

		protected override Task WriteValueAsync(ILogicalDevicePidTimeSpan logicalDevicePid, TimeSpan value, CancellationToken cancellationToken)
		{
			return logicalDevicePid.WriteTimeSpanAsync(value, cancellationToken);
		}

		public LogicalDevicePidBindableAsyncTimeSpan(ILogicalDevicePidTimeSpan logicalDevicePid, bool autoLoadPid, bool autoSavePid = false, bool autoRefreshPid = false, PropertyChangedEventHandler? propertyChanged = null)
			: base(logicalDevicePid, autoLoadPid, autoSavePid, autoRefreshPid, propertyChanged)
		{
		}
	}
}
