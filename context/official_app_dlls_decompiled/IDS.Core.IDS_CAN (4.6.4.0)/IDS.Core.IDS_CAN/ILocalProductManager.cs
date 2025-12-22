using System.Collections;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public interface ILocalProductManager : IEnumerable<LocalProduct>, IEnumerable
	{
		LocalProduct GetProductAtAddress(ADDRESS address);
	}
}
