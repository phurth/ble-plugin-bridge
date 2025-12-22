using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class RemoteDeviceOnlineEvent : Event
	{
		public readonly IRemoteDevice Device;

		public RemoteDeviceOnlineEvent(IEventSender sender, IRemoteDevice device)
			: base(sender)
		{
			Device = device;
		}
	}
}
