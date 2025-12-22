using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidBindableAsyncMap<TValue> : LogicalDevicePidBindableAsyncGeneric<ILogicalDevicePidMap<TValue>, TValue> where TValue : IEquatable<TValue>
	{
		protected override Task<TValue> ReadValueAsync(ILogicalDevicePidMap<TValue> logicalDevicePid, CancellationToken cancellationToken)
		{
			return logicalDevicePid.ReadMapAsync(cancellationToken);
		}

		protected override Task WriteValueAsync(ILogicalDevicePidMap<TValue> logicalDevicePid, TValue value, CancellationToken cancellationToken)
		{
			return logicalDevicePid.WriteMapAsync(value, cancellationToken);
		}

		public LogicalDevicePidBindableAsyncMap(ILogicalDevicePidMap<TValue> logicalDevicePid, bool autoLoadPid, bool autoSavePid = false, bool autoRefreshPid = false, PropertyChangedEventHandler? propertyChanged = null)
			: base(logicalDevicePid, autoLoadPid, autoSavePid, autoRefreshPid, propertyChanged)
		{
		}
	}
}
