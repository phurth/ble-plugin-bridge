using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public interface IReadVoltageMeasurement
	{
		Task<float> ReadVoltageMeasurementAsync(CancellationToken cancellationToken);
	}
}
