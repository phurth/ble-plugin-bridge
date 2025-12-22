using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceExTemperature : ILogicalDeviceEx
	{
		Task<ITemperatureMeasurement> ReadTemperatureMeasurementAsync(CancellationToken cancellationToken);
	}
}
