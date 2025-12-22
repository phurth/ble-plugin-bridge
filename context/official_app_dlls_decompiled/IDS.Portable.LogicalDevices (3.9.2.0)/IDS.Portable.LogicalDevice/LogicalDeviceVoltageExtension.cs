using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public static class LogicalDeviceVoltageExtension
	{
		public static async Task<float> ReadVoltageMeasurementAsync(ILogicalDevicePidFixedPoint fixedPointPid, CancellationToken cancellationToken)
		{
			try
			{
				return await fixedPointPid.ReadFloatAsync(cancellationToken);
			}
			catch (LogicalDevicePidValueReadNotSupportedException nested)
			{
				throw new VoltageMeasurementNotSupportedException(nested);
			}
			catch (OperationCanceledException nested2)
			{
				throw new VoltageMeasurementOperationCanceledException(nested2);
			}
			catch (TimeoutException nested3)
			{
				throw new VoltageMeasurementTimeoutException(nested3);
			}
			catch (Exception nested4)
			{
				throw new VoltageMeasurementException("Unable to read voltage", nested4);
			}
		}
	}
}
