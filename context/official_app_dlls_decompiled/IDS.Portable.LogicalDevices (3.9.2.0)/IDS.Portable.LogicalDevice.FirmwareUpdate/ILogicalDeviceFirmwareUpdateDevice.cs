using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public interface ILogicalDeviceFirmwareUpdateDevice : ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		IReadOnlyDictionary<FirmwareUpdateOption, string> DefaultFirmwareUpdateOptions { get; }

		Task<FirmwareUpdateSupport> TryGetFirmwareUpdateSupportAsync(CancellationToken cancelToken);

		Task UpdateFirmwareAsync(IReadOnlyList<byte> data, Func<ILogicalDeviceTransferProgress, bool> progressAck, CancellationToken cancellationToken, Dictionary<FirmwareUpdateOption, object>? options = null);
	}
}
