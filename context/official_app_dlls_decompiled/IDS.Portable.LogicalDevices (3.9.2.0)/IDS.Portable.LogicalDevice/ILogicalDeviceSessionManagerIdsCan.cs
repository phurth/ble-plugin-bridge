using System;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSessionManagerIdsCan : ILogicalDeviceSessionManager, ICommonDisposable, IDisposable
	{
		void DeviceSessionOpened(ISessionClient session, IRemoteDevice remoteDevice);

		void DeviceSessionClosed(ISessionClient session, IRemoteDevice remoteDevice);

		void DeviceSessionTendCoreIfNeeded(IRemoteDevice physicalDevice);
	}
}
