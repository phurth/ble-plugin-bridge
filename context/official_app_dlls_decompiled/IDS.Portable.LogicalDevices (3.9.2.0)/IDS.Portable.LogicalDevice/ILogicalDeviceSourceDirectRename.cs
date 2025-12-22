using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirectRename : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		bool IsLogicalDeviceRenameSupported(ILogicalDevice? logicalDevice);

		Task RenameLogicalDevice(ILogicalDevice? logicalDevice, FUNCTION_NAME toName, byte toFunctionInstance, CancellationToken cancellationToken);
	}
}
