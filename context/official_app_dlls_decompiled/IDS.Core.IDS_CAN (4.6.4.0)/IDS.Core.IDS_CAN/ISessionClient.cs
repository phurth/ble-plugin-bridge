using System;
using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public interface ISessionClient : IEventSender, ISession, IDisposable, System.IDisposable
	{
		bool IsValid { get; }

		bool TryOpenSession();

		bool CloseSession();
	}
}
