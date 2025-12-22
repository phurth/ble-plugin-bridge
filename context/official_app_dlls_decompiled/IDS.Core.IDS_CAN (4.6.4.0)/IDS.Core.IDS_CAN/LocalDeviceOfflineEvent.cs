using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class LocalDeviceOfflineEvent : Event
	{
		public readonly ILocalDevice Device;

		public ADDRESS PrevAddress { get; private set; }

		public LocalDeviceOfflineEvent(ILocalDevice device)
			: base(device)
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
