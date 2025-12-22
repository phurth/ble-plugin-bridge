using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceExTemperatureOutside : LogicalDeviceExTemperature<ILogicalDeviceTemperatureMeasurementOutside>
	{
		protected override string LogTag => "LogicalDeviceExTemperatureOutside";

		public static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExBase<ILogicalDeviceTemperatureMeasurementOutside>.LogicalDeviceExFactory<LogicalDeviceExTemperatureOutside>(logicalDevice, GetLogicalDeviceScope);
		}

		protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDeviceTemperatureMeasurementOutside logicalDevice)
		{
			return logicalDevice.TemperatureMeasurementOutsideScope;
		}

		protected static ITemperatureMeasurement GetLogicalDeviceTemperatureMeasurement(ILogicalDeviceTemperatureMeasurementOutside logicalDevice)
		{
			return logicalDevice.TemperatureMeasurementOutside;
		}

		public override Task<ITemperatureMeasurement> ReadTemperatureMeasurementAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(GetTemperatureMeasurement(GetLogicalDeviceScope, GetLogicalDeviceTemperatureMeasurement));
		}

		protected override bool CanBePreferredDevice(ILogicalDeviceTemperatureMeasurementOutside logicalDevice)
		{
			return logicalDevice.TemperatureMeasurementOutside.IsTemperatureValid;
		}
	}
}
