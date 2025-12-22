using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using IDS.Core.Tasks;

namespace IDS.Core.IDS_CAN
{
	public interface IPIDManager : IEnumerable<IDevicePID>, IEnumerable
	{
		IRemoteDevice Device { get; }

		int Count { get; }

		bool DeviceQueryComplete { get; }

		void QueryDevice();

		bool Contains(PID id);

		IDevicePID GetPID(PID id);

		Task<PIDValue> ReadAsync(PID id, AsyncOperation operation);

		Task<PIDValue> ReadAsync(PID id, ushort address, AsyncOperation operation);
	}
}
