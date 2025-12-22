using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class CircuitIDChangedEvent : Event
	{
		public readonly IRemoteDevice Device;

		public CIRCUIT_ID Prev { get; private set; }

		public CircuitIDChangedEvent(IRemoteDevice device)
			: base(device.Adapter)
		{
			Device = device;
		}

		public void Publish(CIRCUIT_ID prev)
		{
			Prev = prev;
			Publish();
		}
	}
}
