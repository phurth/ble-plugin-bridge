using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class CircuitListChangedEvent : Event
	{
		public readonly ICircuitManager Circuits;

		public CircuitListChangedEvent(ICircuitManager circuits)
			: base(circuits.Adapter)
		{
			Circuits = circuits;
		}
	}
}
