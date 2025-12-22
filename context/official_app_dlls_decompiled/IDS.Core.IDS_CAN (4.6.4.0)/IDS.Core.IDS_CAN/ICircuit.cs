using System.Collections;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public interface ICircuit : IEnumerable<IRemoteDevice>, IEnumerable
	{
		CIRCUIT_ID CircuitID { get; }

		int DeviceCount { get; }

		bool IsEmpty { get; }
	}
}
