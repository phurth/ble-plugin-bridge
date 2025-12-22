using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidBindableAsyncOfType<TValue> : LogicalDevicePidBindableAsyncGeneric<ILogicalDevicePid<TValue>, TValue> where TValue : IEquatable<TValue>
	{
		public LogicalDevicePidBindableAsyncOfType(ILogicalDevicePid<TValue> logicalDevicePid, bool autoLoadPid, bool autoSavePid = false, bool autoRefreshPid = false, PropertyChangedEventHandler? propertyChanged = null)
			: base(logicalDevicePid, autoLoadPid, autoSavePid, autoRefreshPid, propertyChanged)
		{
		}

		protected override Task<TValue> ReadValueAsync(ILogicalDevicePid<TValue> logicalDevicePid, CancellationToken cancellationToken)
		{
			return logicalDevicePid.ReadAsync(cancellationToken);
		}

		protected override Task WriteValueAsync(ILogicalDevicePid<TValue> logicalDevicePid, TValue value, CancellationToken cancellationToken)
		{
			return logicalDevicePid.WriteAsync(value, cancellationToken);
		}
	}
}
