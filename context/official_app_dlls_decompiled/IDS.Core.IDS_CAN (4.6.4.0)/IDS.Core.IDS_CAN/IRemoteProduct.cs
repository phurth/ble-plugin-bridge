using System.Collections;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public interface IRemoteProduct : IProduct, IBusEndpoint, IUniqueProductInfo, IEnumerable<IDevice>, IEnumerable, IEnumerable<IRemoteDevice>
	{
		IDTCManager DTCs { get; }
	}
}
