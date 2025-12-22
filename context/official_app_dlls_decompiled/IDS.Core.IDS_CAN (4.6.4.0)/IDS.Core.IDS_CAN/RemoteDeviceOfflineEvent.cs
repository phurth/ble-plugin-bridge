using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class RemoteDeviceOfflineEvent : Event
	{
		public readonly IRemoteDevice Device;

		public ADDRESS PrevAddress { get; private set; }

		public RemoteDeviceOfflineEvent(IEventSender sender, IRemoteDevice device)
			: base(sender)
		{
			Device = device;
		}

		public void Publish(ADDRESS a)
		{
			PrevAddress = a;
			Publish();
		}
	}
}
