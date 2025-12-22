using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePid
	{
		int PidReadTimeoutSec { get; }

		int PidWriteTimeoutSec { get; }

		PID PropertyId { get; }

		bool IsReadOnly { get; }

		IPidDetail PidDetail { get; }

		Task<ulong> ReadValueAsync(CancellationToken cancellationToken);

		Task WriteValueAsync(ulong value, CancellationToken cancellationToken);
	}
	public interface ILogicalDevicePid<TValue> : ILogicalDevicePid
	{
		Task<TValue> ReadAsync(CancellationToken cancellationToken);

		Task WriteAsync(TValue value, CancellationToken cancellationToken);
	}
}
