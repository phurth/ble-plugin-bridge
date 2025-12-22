using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class LocalDeviceOnlineEvent : Event
	{
		public readonly ILocalDevice Device;

		public LocalDeviceOnlineEvent(ILocalDevice device)
			: base(device)
		{
			Device = device;
		}
	}
}
