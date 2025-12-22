using System;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public interface ILogicalDeviceFirmwareUpdateSession : ICommonDisposable, IDisposable
	{
		ILogicalDeviceFirmwareUpdateDevice LogicalDevice { get; }
	}
}
