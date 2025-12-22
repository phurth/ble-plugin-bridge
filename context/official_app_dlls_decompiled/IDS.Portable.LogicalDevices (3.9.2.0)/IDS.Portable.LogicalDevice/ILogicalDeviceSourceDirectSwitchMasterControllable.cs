using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirectSwitchMasterControllable : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<bool> TrySwitchAllMasterControllable(IEnumerable<ILogicalDevice> logicalDeviceList, bool allOn, CancellationToken cancellationToken);
	}
}
