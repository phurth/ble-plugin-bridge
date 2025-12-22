using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSessionManager : ICommonDisposable, IDisposable
	{
		bool IsSessionActive(LogicalDeviceSessionType sessionType, ILogicalDevice logicalDevice);

		Task<ILogicalDeviceSession> ActivateSessionAsync(LogicalDeviceSessionType sessionType, ILogicalDevice logicalDevice, CancellationToken cancelToken, uint msSessionKeepAliveTime = 15000u, uint msSessionGetTimeout = 3000u);

		void DeactivateSession(LogicalDeviceSessionType sessionType, ILogicalDevice logicalDevice, bool closeSession);

		void CloseAllSessions();
	}
}
