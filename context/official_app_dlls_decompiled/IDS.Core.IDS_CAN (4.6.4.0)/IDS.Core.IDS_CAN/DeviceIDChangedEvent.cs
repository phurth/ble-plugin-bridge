using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class DeviceIDChangedEvent : Event
	{
		public readonly IRemoteDevice Device;

		public DeviceIDChangedEvent(IRemoteDevice device)
			: base(device.Adapter)
		{
			Device = device;
		}
	}
}
