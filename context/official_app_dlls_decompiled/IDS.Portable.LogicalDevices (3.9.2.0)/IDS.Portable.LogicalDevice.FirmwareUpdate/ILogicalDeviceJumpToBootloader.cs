using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public interface ILogicalDeviceJumpToBootloader : ILogicalDeviceFirmwareUpdateDevice, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		bool IsJumpToBootPidSupported { get; }

		bool IsJumpToBootRequiredForFirmwareUpdate { get; }

		ILogicalDevicePidJumpToBoot JumpToBootPid { get; }
	}
}
