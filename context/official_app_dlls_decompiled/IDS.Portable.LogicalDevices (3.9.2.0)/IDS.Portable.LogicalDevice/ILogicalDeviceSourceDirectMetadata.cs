using System;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirectMetadata : ILogicalDeviceSourceDirect, ILogicalDeviceSource
	{
		Task<string> GetSoftwarePartNumberAsync(ILogicalDevice logicalDevice, CancellationToken cancelToken);

		Version? GetDeviceProtocolVersion(ILogicalDevice logicalDevice);
	}
}
