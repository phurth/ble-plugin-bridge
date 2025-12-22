using System.Collections;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public interface IProductManager : IEnumerable<IRemoteProduct>, IEnumerable
	{
		IAdapter Adapter { get; }

		int Count { get; }

		IRemoteProduct GetProduct(ADDRESS address);

		IRemoteProduct GetProduct(ulong unique_id);
	}
}
