using System;
using System.Collections;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public interface IDeviceDiscoverer : IEnumerable<IRemoteDevice>, IEnumerable
	{
		IAdapter Adapter { get; }

		int NumDevicesDetectedOnNetwork { get; }

		IRemoteDevice GetDeviceByAddress(ADDRESS address);

		IRemoteDevice GetDeviceByUniqueID(ulong unique_id);

		IEnumerable<IRemoteDevice> GetAllDevicesMatchingFilter(Func<IDevice, bool> filter);
	}
}
