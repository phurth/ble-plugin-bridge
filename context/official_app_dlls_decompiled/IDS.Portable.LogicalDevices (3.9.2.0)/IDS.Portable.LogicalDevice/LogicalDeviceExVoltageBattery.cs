using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceExVoltageBattery : LogicalDeviceExVoltage<ILogicalDeviceVoltageMeasurementBattery>
	{
		private bool _pidBatteryNotSupported;

		protected override string LogTag => "LogicalDeviceExVoltageBattery";

		public static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExBase<ILogicalDeviceVoltageMeasurementBattery>.LogicalDeviceExFactory<LogicalDeviceExVoltageBattery>(logicalDevice, GetLogicalDeviceScope);
		}

		protected override bool VoltageMeasurementSupported(ILogicalDeviceVoltageMeasurementBattery logicalDevice)
		{
			if (_pidBatteryNotSupported)
			{
				return false;
			}
			if (logicalDevice is ILogicalDeviceVoltageMeasurementBatteryPid logicalDeviceVoltageMeasurementBatteryPid)
			{
				return logicalDeviceVoltageMeasurementBatteryPid.IsVoltagePidReadSupported;
			}
			return false;
		}

		protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDeviceVoltageMeasurementBattery logicalDevice)
		{
			if (logicalDevice is ILogicalDeviceVoltageMeasurementBatteryPid logicalDeviceVoltageMeasurementBatteryPid)
			{
				return logicalDeviceVoltageMeasurementBatteryPid.VoltageMeasurementBatteryPidScope;
			}
			throw new VoltageMeasurementNotSupportedException();
		}

		protected static async Task<float> GetVoltageMeasurementAsync(ILogicalDeviceVoltageMeasurementBattery logicalDevice, CancellationToken cancellationToken)
		{
			if (logicalDevice is ILogicalDeviceVoltageMeasurementBatteryPid logicalDeviceVoltageMeasurementBatteryPid)
			{
				return await LogicalDeviceVoltageExtension.ReadVoltageMeasurementAsync(logicalDeviceVoltageMeasurementBatteryPid.VoltageMeasurementBatteryPid, cancellationToken);
			}
			throw new VoltageMeasurementNotSupportedException();
		}

		public override async Task<float> ReadVoltageMeasurementAsync(CancellationToken cancellationToken)
		{
			try
			{
				return await GetVoltageMeasurementAsync(cancellationToken, GetLogicalDeviceScope, GetVoltageMeasurementAsync);
			}
			catch (VoltageMeasurementUnavailableException)
			{
				throw;
			}
			catch (LogicalDevicePidValueReadNotSupportedException nested)
			{
				_pidBatteryNotSupported = true;
				throw new VoltageMeasurementNotSupportedException(nested);
			}
			catch (VoltageMeasurementNotSupportedException)
			{
				_pidBatteryNotSupported = true;
				throw;
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
