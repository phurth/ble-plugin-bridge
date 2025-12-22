using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkSessionManager : CommonDisposable, ILogicalDeviceSessionManager, ICommonDisposable, IDisposable
	{
		private const string LogTag = "MyRvLinkSessionManager";

		private readonly IDirectConnectionMyRvLink _directManager;

		private readonly ConcurrentDictionary<(LogicalDeviceSessionType sessionType, ILogicalDevice logicalDevice), LogicalDeviceSessionMyRvLink> _sessionDict;

		public MyRvLinkSessionManager(IDirectConnectionMyRvLink directManager)
		{
			_directManager = directManager ?? throw new ArgumentNullException("directManager");
			_sessionDict = new ConcurrentDictionary<(LogicalDeviceSessionType, ILogicalDevice), LogicalDeviceSessionMyRvLink>();
		}

		public bool IsSessionActive(LogicalDeviceSessionType sessionType, ILogicalDevice logicalDevice)
		{
			if (!_sessionDict.TryGetValue((sessionType, logicalDevice), out var logicalDeviceSessionMyRvLink))
			{
				return false;
			}
			if (logicalDeviceSessionMyRvLink.IsActivated && _directManager != logicalDevice.DeviceService.GetPrimaryDeviceSourceDirect(logicalDevice))
			{
				TaggedLog.Information("MyRvLinkSessionManager", "MyRvLinkSessionManager Auto deactivating session because device no longer being controlled via this RvLink");
				DeactivateSession(sessionType, logicalDevice, closeSession: true);
			}
			return logicalDeviceSessionMyRvLink.IsActivated;
		}

		public Task<ILogicalDeviceSession> ActivateSessionAsync(LogicalDeviceSessionType sessionType, ILogicalDevice logicalDevice, CancellationToken cancelToken, uint msSessionKeepAliveTime = 15000u, uint msSessionGetTimeout = 3000u)
		{
			if (!_directManager.DeviceService.SessionsEnabled)
			{
				throw new ActivateSessionDisabledException("MyRvLinkSessionManager");
			}
			if (base.IsDisposed)
			{
				throw new ObjectDisposedException("ActivateSessionAsync not possible because MyRvLinkSessionManager Is Disposed");
			}
			if (logicalDevice.ActiveConnection == LogicalDeviceActiveConnection.Remote)
			{
				throw new ActivateSessionRemoteActiveException("MyRvLinkSessionManager");
			}
			if (!_directManager.IsLogicalDeviceOnline(logicalDevice))
			{
				throw new PhysicalDeviceNotFoundException("MyRvLinkSessionManager", logicalDevice);
			}
			if (!_directManager.GetMyRvDeviceFromLogicalDevice(logicalDevice).HasValue)
			{
				throw new PhysicalDeviceNotFoundException("MyRvLinkSessionManager", logicalDevice);
			}
			if (logicalDevice.IsDisposed)
			{
				throw new ObjectDisposedException("logicalDevice", "Logical Device Is Disposed");
			}
			if (logicalDevice.InTransitLockout.IsInLockout())
			{
				throw new ActivateSessionEnforcedInTransitLockout("MyRvLinkSessionManager", logicalDevice);
			}
			if (!_sessionDict.TryGetValue((sessionType, logicalDevice), out var session))
			{
				session = new LogicalDeviceSessionMyRvLink();
				_sessionDict.AddOrUpdate((sessionType, logicalDevice), session, ((LogicalDeviceSessionType sessionType, ILogicalDevice logicalDevice) key, LogicalDeviceSessionMyRvLink oldValue) => session);
			}
			session.ActivateSession();
			logicalDevice.UpdateSessionChanged(sessionType.ToIdsCanSessionId());
			return Task.FromResult((ILogicalDeviceSession)session);
		}

		public void DeactivateSession(LogicalDeviceSessionType sessionType, ILogicalDevice logicalDevice, bool closeSession)
		{
			if (_sessionDict.TryGetValue((sessionType, logicalDevice), out var logicalDeviceSessionMyRvLink))
			{
				logicalDeviceSessionMyRvLink.DeactivateSession();
				logicalDevice.UpdateSessionChanged(sessionType.ToIdsCanSessionId());
			}
		}

		public void CloseAllSessions()
		{
			_sessionDict.Clear();
		}

		public override void Dispose(bool disposing)
		{
			CloseAllSessions();
		}
	}
}
