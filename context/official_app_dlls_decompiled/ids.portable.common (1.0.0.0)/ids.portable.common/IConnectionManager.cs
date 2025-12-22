using System;

namespace IDS.Portable.Common
{
	public interface IConnectionManager
	{
		ConnectionManagerStatus Status { get; }

		event Action<IConnectionManager> DidConnectEvent;

		event Action<IConnectionManager> DidDisconnectEvent;

		void Start();

		void Stop();
	}
}
