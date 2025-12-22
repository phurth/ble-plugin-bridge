using System;
using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public interface IAdapter : CAN.IAdapter<CAN.MessageBuffer>, Comm.IAdapter<CAN.MessageBuffer>, Comm.IAdapter, IEventSender, IDisposableManager, IDisposable, System.IDisposable, CAN.IAdapter
	{
		ADAPTER_OPTIONS Options { get; set; }

		IDeviceDiscoverer Devices { get; }

		IProductManager Products { get; }

		ILocalProductManager LocalProducts { get; }

		ICircuitManager Circuits { get; }

		ILocalDevice LocalHost { get; }

		INetworkTime Clock { get; }

		IN_MOTION_LOCKOUT_LEVEL NetworkLevelInMotionLockoutLevel { get; }

		ADDRESS GetUnusedDeviceAddress();

		void EnableLocalHost();

		void DisableLocalHost();
	}
}
