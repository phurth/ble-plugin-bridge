using System;
using System.Threading;
using System.Threading.Tasks;

namespace ids.portable.ble.BleAdapter
{
	internal interface IBleAdapterService
	{
		Task BleServicesEnabledCheckAsync(TimeSpan timeout, CancellationToken cancellationToken, bool validateLocationServices = true);
	}
}
