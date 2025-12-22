using System;
using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public interface ILocalDevice : IDevice, IBusEndpoint, IUniqueDeviceInfo, IUniqueProductInfo, ILocalDeviceAsyncMessaging, IPidClient, IBlockClient, IEventSender, IDisposableManager, IDisposable, System.IDisposable
	{
		bool IsMuted { get; }

		bool IsEnabled { get; }

		LOCAL_DEVICE_OPTIONS Options { get; }

		new LocalTextConsole TextConsole { get; }

		bool IsInMotionLockoutInContention { get; }

		bool Transmit11(MESSAGE_TYPE type, CAN.PAYLOAD payload = default(CAN.PAYLOAD));

		bool Transmit29(MESSAGE_TYPE type, byte ext_data, IBusEndpoint target, CAN.PAYLOAD payload = default(CAN.PAYLOAD));

		bool Transmit29(MESSAGE_TYPE type, byte ext_data, ADDRESS target, CAN.PAYLOAD payload = default(CAN.PAYLOAD));
	}
}
