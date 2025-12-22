using System;
using IDS.Core.Types;

namespace IDS.Portable.LogicalDevice
{
	public readonly struct LogicalDeviceTransferProgress : ILogicalDeviceTransferProgress
	{
		public UInt48 BytesSent { get; }

		public UInt48 TotalRetryCount { get; }

		public UInt48 CurrentCommandRetryCount { get; }

		public TimeSpan TotalTime { get; }

		public float TransferRateBytesPerSecond
		{
			get
			{
				float num = (float)TotalTime.TotalSeconds;
				if (num <= 0f)
				{
					return 0f;
				}
				float num2 = (float)BytesSent / num;
				if (num2 < 1f)
				{
					return 0f;
				}
				return num2;
			}
		}

		public LogicalDeviceTransferProgress(UInt48 bytesSent, UInt48 totalRetryCount, UInt48 currentCommandRetryCount, TimeSpan totalTime)
		{
			BytesSent = bytesSent;
			TotalRetryCount = totalRetryCount;
			CurrentCommandRetryCount = currentCommandRetryCount;
			TotalTime = totalTime;
		}
	}
}
