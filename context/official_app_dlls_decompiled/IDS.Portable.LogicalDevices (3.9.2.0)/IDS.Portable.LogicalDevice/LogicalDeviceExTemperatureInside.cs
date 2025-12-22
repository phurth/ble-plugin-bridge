using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceExTemperatureInside : LogicalDeviceExTemperature<ILogicalDeviceTemperatureMeasurementInside>
	{
		protected override string LogTag => "LogicalDeviceExTemperatureInside";

		public static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExBase<ILogicalDeviceTemperatureMeasurementInside>.LogicalDeviceExFactory<LogicalDeviceExTemperatureInside>(logicalDevice, GetLogicalDeviceScope);
		}

		protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDeviceTemperatureMeasurementInside logicalDevice)
		{
			return logicalDevice.TemperatureMeasurementInsideScope;
		}

		protected static ITemperatureMeasurement GetLogicalDeviceTemperatureMeasurement(ILogicalDeviceTemperatureMeasurementInside logicalDevice)
		{
			return logicalDevice.TemperatureMeasurementInside;
		}

		public override Task<ITemperatureMeasurement> ReadTemperatureMeasurementAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(GetTemperatureMeasurement(GetLogicalDeviceScope, GetLogicalDeviceTemperatureMeasurement));
		}

		protected override bool CanBePreferredDevice(ILogicalDeviceTemperatureMeasurementInside logicalDevice)
		{
			return logicalDevice.TemperatureMeasurementInside.IsTemperatureValid;
		}
	}
}
