using System;
using IDS.Portable.Common;

namespace ids.portable.ble.Platforms.Shared.Reachability
{
	public interface IBleDeviceReachabilityManager : ICommonDisposable, IDisposable
	{
		TimeSpan MinReachabilityTimeout { get; }

		TimeSpan MaxReachabilityTimeout { get; }

		BleDeviceReachability Reachability { get; }

		event ReachabilityChangedHandler? ReachabilityChanged;

		void DeviceReachableUntil(TimeSpan timeoutTimeSpan);
	}
}
