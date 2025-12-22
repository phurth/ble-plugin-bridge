using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class TextConsoleSizeChangedEvent : Event
	{
		public readonly IRemoteDevice Device;

		public TextConsoleSizeChangedEvent(IRemoteDevice device)
			: base(device)
		{
			Device = device;
		}
	}
}
