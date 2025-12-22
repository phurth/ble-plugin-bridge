using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidBindableAsyncValue : LogicalDevicePidBindableAsyncGeneric<ILogicalDevicePid, ulong>
	{
		protected override Task<ulong> ReadValueAsync(ILogicalDevicePid logicalDevicePid, CancellationToken cancellationToken)
		{
			return logicalDevicePid.ReadValueAsync(cancellationToken);
		}

		protected override Task WriteValueAsync(ILogicalDevicePid logicalDevicePid, ulong value, CancellationToken cancellationToken)
		{
			return logicalDevicePid.WriteValueAsync(value, cancellationToken);
		}

		public LogicalDevicePidBindableAsyncValue(ILogicalDevicePid logicalDevicePid, bool autoLoadPid, bool autoSavePid = false, bool autoRefreshPid = false, PropertyChangedEventHandler? propertyChanged = null)
			: base(logicalDevicePid, autoLoadPid, autoSavePid, autoRefreshPid, propertyChanged)
		{
		}
	}
}
