using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class ClientSessionOpenEvent : Event
	{
		public readonly ISessionClient Session;

		public ClientSessionOpenEvent(ISessionClient session)
			: base(session)
		{
			Session = session;
		}
	}
}
