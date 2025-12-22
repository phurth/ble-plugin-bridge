using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidBindableAsyncBool : LogicalDevicePidBindableAsyncGeneric<ILogicalDevicePidBool, bool>
	{
		protected override Task<bool> ReadValueAsync(ILogicalDevicePidBool logicalDevicePid, CancellationToken cancellationToken)
		{
			return logicalDevicePid.ReadBoolAsync(cancellationToken);
		}

		protected override Task WriteValueAsync(ILogicalDevicePidBool logicalDevicePid, bool value, CancellationToken cancellationToken)
		{
			return logicalDevicePid.WriteBoolAsync(value, cancellationToken);
		}

		public LogicalDevicePidBindableAsyncBool(ILogicalDevicePidBool logicalDevicePid, bool autoLoadPid, bool autoSavePid = false, bool autoRefreshPid = false, PropertyChangedEventHandler? propertyChanged = null)
			: base(logicalDevicePid, autoLoadPid, autoSavePid, autoRefreshPid, propertyChanged)
		{
		}
	}
}
