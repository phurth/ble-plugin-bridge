using System.Collections;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public interface IProduct : IBusEndpoint, IUniqueProductInfo, IEnumerable<IDevice>, IEnumerable
	{
		string Name { get; }

		string UniqueName { get; }

		IDS_CAN_VERSION_NUMBER ProtocolVersion { get; }

		byte ProductInstance { get; }

		int AssemblyPartNumber { get; }

		int DeviceCount { get; }

		SOFTWARE_UPDATE_STATE SoftwareUpdateState { get; }
	}
}
