using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	internal class LogicalDeviceCircuitIdWriteTracker
	{
		public readonly CIRCUIT_ID CircuitId;

		public readonly CancellationToken CancellationToken;

		public readonly TaskCompletionSource<LogicalDeviceCircuitIdWriteResult> Result;

		public LogicalDeviceCircuitIdWriteTracker(CIRCUIT_ID circuitId, CancellationToken cancellationToken, TaskCompletionSource<LogicalDeviceCircuitIdWriteResult> result)
		{
			CircuitId = circuitId;
			CancellationToken = cancellationToken;
			Result = result;
		}
	}
}
