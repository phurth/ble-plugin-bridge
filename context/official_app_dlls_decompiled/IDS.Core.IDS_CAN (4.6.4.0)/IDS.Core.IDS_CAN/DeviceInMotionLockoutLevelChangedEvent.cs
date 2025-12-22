using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class DeviceInMotionLockoutLevelChangedEvent : Event
	{
		public readonly IAdapter Adapter;

		public readonly IDevice Device;

		public IN_MOTION_LOCKOUT_LEVEL CurrentLockoutLevel { get; private set; } = (byte)0;


		public IN_MOTION_LOCKOUT_LEVEL PreviousLockoutLevel { get; private set; } = (byte)0;


		public DeviceInMotionLockoutLevelChangedEvent(IDevice device)
			: base(device.Adapter)
		{
			Device = device;
			Adapter = device.Adapter;
		}

		public void Publish(IN_MOTION_LOCKOUT_LEVEL current, IN_MOTION_LOCKOUT_LEVEL prev)
		{
			CurrentLockoutLevel = current;
			PreviousLockoutLevel = prev;
			Publish();
		}
	}
}
