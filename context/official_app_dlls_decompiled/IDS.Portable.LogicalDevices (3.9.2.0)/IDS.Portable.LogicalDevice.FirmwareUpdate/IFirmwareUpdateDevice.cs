using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public interface IFirmwareUpdateDevice
	{
		Task<FirmwareUpdateSupport> TryGetFirmwareUpdateSupportAsync(ILogicalDevice logicalDevice, CancellationToken cancelToken);

		Task UpdateFirmwareAsync(ILogicalDeviceFirmwareUpdateSession firmwareUpdateSession, IReadOnlyList<byte> data, Func<ILogicalDeviceTransferProgress, bool> progressAck, CancellationToken cancellationToken, IReadOnlyDictionary<FirmwareUpdateOption, object>? options = null);
	}
}
