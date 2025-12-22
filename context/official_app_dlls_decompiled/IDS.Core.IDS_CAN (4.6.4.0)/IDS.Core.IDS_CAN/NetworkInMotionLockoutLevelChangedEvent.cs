using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class NetworkInMotionLockoutLevelChangedEvent : Event
	{
		public readonly IAdapter Adapter;

		public IN_MOTION_LOCKOUT_LEVEL CurrentLockoutLevel { get; private set; } = (byte)0;


		public IN_MOTION_LOCKOUT_LEVEL PreviousLockoutLevel { get; private set; } = (byte)0;


		public NetworkInMotionLockoutLevelChangedEvent(IAdapter a)
			: base(a)
		{
			Adapter = a;
		}

		public void Publish(IN_MOTION_LOCKOUT_LEVEL current, IN_MOTION_LOCKOUT_LEVEL prev)
		{
			CurrentLockoutLevel = current;
			PreviousLockoutLevel = prev;
			Publish();
		}
	}
}
