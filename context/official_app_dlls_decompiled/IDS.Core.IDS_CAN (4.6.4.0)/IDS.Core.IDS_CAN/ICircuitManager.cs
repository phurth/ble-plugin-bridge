using System.Collections;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public interface ICircuitManager : IEnumerable<ICircuit>, IEnumerable
	{
		IAdapter Adapter { get; }

		int Count { get; }

		CIRCUIT_ID GetRandomUnusedCircuitID();
	}
}
