using System;
using IDS.Core.Types;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceTransferProgress
	{
		UInt48 BytesSent { get; }

		UInt48 TotalRetryCount { get; }

		UInt48 CurrentCommandRetryCount { get; }

		TimeSpan TotalTime { get; }

		float TransferRateBytesPerSecond { get; }
	}
}
