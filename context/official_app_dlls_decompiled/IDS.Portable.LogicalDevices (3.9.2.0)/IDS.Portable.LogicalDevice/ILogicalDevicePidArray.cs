using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidArray<TValue>
	{
		PID PropertyId { get; }

		LogicalDeviceSessionType WriteAccess { get; }

		bool IsReadOnly { get; }

		ushort MinIndex { get; }

		ushort MaxIndex { get; }

		Task<TValue> ReadValueAsync(ushort index, CancellationToken cancellationToken);

		Task WriteValueAsync(ushort index, TValue value, CancellationToken cancellationToken);
	}
}
