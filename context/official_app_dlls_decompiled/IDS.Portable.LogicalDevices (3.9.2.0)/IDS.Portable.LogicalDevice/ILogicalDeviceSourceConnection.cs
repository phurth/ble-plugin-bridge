using System;
using System.Collections.Generic;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceConnection : ILogicalDeviceSource
	{
		IReadOnlyList<ILogicalDeviceTag> ConnectionTagList { get; }

		bool IsConnected { get; }

		event Action<ILogicalDeviceSourceDirectConnection>? DidConnectEvent;

		event Action<ILogicalDeviceSourceDirectConnection>? DidDisconnectEvent;

		event UpdateDeviceSourceReachabilityEventHandler? UpdateDeviceSourceReachabilityEvent;

		void Start();

		void Stop();

		LogicalDeviceReachability DeviceSourceReachability(ILogicalDevice logicalDevice);
	}
}
