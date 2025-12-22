using System.Collections;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public interface IClientSessionManager : IEnumerable<SESSION_ID>, IEnumerable
	{
		IRemoteDevice Device { get; }

		int Count { get; }

		bool DeviceQueryComplete { get; }

		void QueryDevice();

		bool Contains(SESSION_ID id);

		ISessionClient GetSession(ILocalDevice localhost, SESSION_ID session_id);
	}
}
