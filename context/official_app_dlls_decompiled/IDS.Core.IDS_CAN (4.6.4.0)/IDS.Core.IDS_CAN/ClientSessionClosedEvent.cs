using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class ClientSessionClosedEvent : Event
	{
		public readonly ISessionClient Session;

		public ClientSessionClosedEvent(ISessionClient session)
			: base(session)
		{
			Session = session;
		}
	}
}
