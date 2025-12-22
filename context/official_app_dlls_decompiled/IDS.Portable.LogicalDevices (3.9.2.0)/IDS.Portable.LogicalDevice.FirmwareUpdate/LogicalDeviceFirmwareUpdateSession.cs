using System;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice.FirmwareUpdate
{
	public class LogicalDeviceFirmwareUpdateSession : CommonDisposable, ILogicalDeviceFirmwareUpdateSession, ICommonDisposable, IDisposable
	{
		public const string LogTag = "LogicalDeviceFirmwareUpdateSession";

		public ILogicalDeviceFirmwareUpdateDevice LogicalDevice { get; }

		internal LogicalDeviceFirmwareUpdateSession(ILogicalDeviceFirmwareUpdateDevice logicalDevice)
		{
			LogicalDevice = logicalDevice;
		}

		public override void Dispose(bool disposing)
		{
			TaggedLog.Information("LogicalDeviceFirmwareUpdateSession", $"Firmware Update Session Stopped (Disposed) for {LogicalDevice}");
		}
	}
}
