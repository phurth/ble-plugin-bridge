using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevicePidBindableAsyncFloat : LogicalDevicePidBindableAsyncGeneric<ILogicalDevicePid, float>
	{
		public readonly FixedPointType FixedPointType;

		protected override async Task<float> ReadValueAsync(ILogicalDevicePid logicalDevicePid, CancellationToken cancellationToken)
		{
			ulong fixedPointNumber = await logicalDevicePid.ReadValueAsync(cancellationToken);
			return FixedPointType.FixedPointToFloat(fixedPointNumber);
		}

		protected override Task WriteValueAsync(ILogicalDevicePid logicalDevicePid, float value, CancellationToken cancellationToken)
		{
			ulong value2 = FixedPointType.ToFixedPointAsULong(value);
			return logicalDevicePid.WriteValueAsync(value2, cancellationToken);
		}

		public LogicalDevicePidBindableAsyncFloat(FixedPointType fixedPointType, ILogicalDevicePid logicalDevicePid, bool autoLoadPid, bool autoSavePid = false, bool autoRefreshPid = false, PropertyChangedEventHandler? propertyChanged = null)
			: base(logicalDevicePid, autoLoadPid, autoSavePid, autoRefreshPid, propertyChanged)
		{
			FixedPointType = fixedPointType;
		}
	}
}
