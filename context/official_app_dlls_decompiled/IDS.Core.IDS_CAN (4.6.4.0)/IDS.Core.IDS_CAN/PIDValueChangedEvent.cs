using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class PIDValueChangedEvent : Event
	{
		public readonly IDevicePID PID;

		public ulong Value { get; private set; }

		public PIDValueChangedEvent(IDevicePID pid)
			: base(pid)
		{
			PID = pid;
		}

		public void Publish(ulong value)
		{
			Value = value;
			Publish();
		}
	}
}
