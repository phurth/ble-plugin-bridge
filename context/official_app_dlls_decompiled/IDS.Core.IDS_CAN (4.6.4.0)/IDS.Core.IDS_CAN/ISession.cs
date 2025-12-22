using System;

namespace IDS.Core.IDS_CAN
{
	public interface ISession
	{
		SESSION_ID SessionID { get; }

		IBusEndpoint Host { get; }

		IBusEndpoint Client { get; }

		bool IsOpen { get; }

		TimeSpan OpenTime { get; }
	}
}
