using System.Collections;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public interface IDTCManager : IEnumerable<IProductDTC>, IEnumerable
	{
		IRemoteProduct Product { get; }

		bool AreSupported { get; }

		bool HasActiveDTCs { get; }

		bool HasStoredDTCs { get; }

		int Count { get; }

		IEnumerator<IProductDTC> ActiveDTCs { get; }

		int ActiveCount { get; }

		IEnumerator<IProductDTC> StoredDTCs { get; }

		int StoredCount { get; }

		void QueryProduct();

		bool Contains(DTC_ID id);
	}
}
