using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class TextConsoleTextChangedEvent : Event
	{
		public readonly IRemoteDevice Device;

		public TextConsoleTextChangedEvent(IRemoteDevice device)
			: base(device)
		{
			Device = device;
		}
	}
}
