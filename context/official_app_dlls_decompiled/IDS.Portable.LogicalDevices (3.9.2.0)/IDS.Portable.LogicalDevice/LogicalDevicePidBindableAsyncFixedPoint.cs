using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidBindableAsyncFixedPoint : LogicalDevicePidBindableAsyncGeneric<ILogicalDevicePidFixedPoint, float>
	{
		protected override Task<float> ReadValueAsync(ILogicalDevicePidFixedPoint logicalDevicePid, CancellationToken cancellationToken)
		{
			return logicalDevicePid.ReadFloatAsync(cancellationToken);
		}

		protected override Task WriteValueAsync(ILogicalDevicePidFixedPoint logicalDevicePid, float value, CancellationToken cancellationToken)
		{
			return logicalDevicePid.WriteFloatAsync(value, cancellationToken);
		}

		public LogicalDevicePidBindableAsyncFixedPoint(ILogicalDevicePidFixedPoint logicalDevicePid, bool autoLoadPid, bool autoSavePid = false, bool autoRefreshPid = false, PropertyChangedEventHandler? propertyChanged = null)
			: base(logicalDevicePid, autoLoadPid, autoSavePid, autoRefreshPid, propertyChanged)
		{
		}
	}
}
