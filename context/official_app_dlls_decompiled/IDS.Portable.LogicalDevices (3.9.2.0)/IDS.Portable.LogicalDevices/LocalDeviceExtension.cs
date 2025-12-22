using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using Serilog;

namespace IDS.Portable.LogicalDevices
{
	public static class LocalDeviceExtension
	{
		private const string LogTag = "LocalDeviceExtension";

		private const int DisableInMotionLockoutThrottleTimeMs = 5000;

		private static readonly object _locker = new object();

		private static DateTime? _lastTimeSentDisableInMotionLockoutCommand = null;

		public static void SendDisableInMotionLockoutCommand(this ILocalDevice localDevice, bool forceSend = false)
		{
			if (localDevice.Adapter.LocalHost.Address == ADDRESS.INVALID)
			{
				return;
			}
			lock (_locker)
			{
				if (_lastTimeSentDisableInMotionLockoutCommand.HasValue)
				{
					TimeSpan timeSpan = DateTime.Now - _lastTimeSentDisableInMotionLockoutCommand.Value;
					if (!forceSend && timeSpan.TotalMilliseconds < 5000.0)
					{
						TaggedLog.Debug("LocalDeviceExtension", $"SendDisableInMotionLockoutCommand attempt to clear In Motion Lockout ignored because only {timeSpan.TotalMilliseconds}ms has passed since last time it was sent");
						return;
					}
				}
				TaggedLog.Debug("LocalDeviceExtension", "SendDisableInMotionLockoutCommand sending clear In Motion Lockout request!");
				_lastTimeSentDisableInMotionLockoutCommand = DateTime.Now;
			}
			CAN.PAYLOAD payload = new CAN.PAYLOAD(0);
			payload.Append((byte)85);
			localDevice.Transmit29((byte)128, 2, ADDRESS.BROADCAST, payload);
			CAN.PAYLOAD payload2 = new CAN.PAYLOAD(0);
			payload2.Append((byte)170);
			localDevice.Transmit29((byte)128, 2, ADDRESS.BROADCAST, payload2);
		}

		public static Task<CommandResult> SendSoftwareUpdateAuthorizationAsync(this ILocalDevice localDevice, ADDRESS destinationCanAddress, CancellationToken cancelToken)
		{
			ILocalDevice localDevice2 = localDevice;
			ADDRESS destinationCanAddress2 = destinationCanAddress;
			if (cancelToken.IsCancellationRequested)
			{
				return Task.FromResult(CommandResult.Canceled);
			}
			if (localDevice2 == null)
			{
				return Task.FromResult(CommandResult.ErrorOther);
			}
			ADDRESS aDDRESS = localDevice2.Adapter?.LocalHost?.Address;
			if (aDDRESS == null || aDDRESS == ADDRESS.INVALID)
			{
				return Task.FromResult(CommandResult.ErrorDeviceOffline);
			}
			if (destinationCanAddress2 == ADDRESS.INVALID)
			{
				return Task.FromResult(CommandResult.ErrorDeviceOffline);
			}
			Log.Debug($"SendSoftwareUpdateAuthorizationAsync from {localDevice2} to {destinationCanAddress2} from {aDDRESS}");
			Task.Run(async delegate
			{
				for (int index = 0; index < 3; index++)
				{
					if (cancelToken.IsCancellationRequested)
					{
						break;
					}
					CAN.PAYLOAD payload = new CAN.PAYLOAD(0);
					payload.Append((byte)1);
					localDevice2.Transmit29((byte)128, 3, destinationCanAddress2, payload);
					await TaskExtension.TryDelay(1000, cancelToken);
				}
			}, cancelToken);
			return Task.FromResult(CommandResult.Completed);
		}
	}
}
