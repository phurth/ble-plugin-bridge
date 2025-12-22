using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class LogicalDeviceSessionMyRvLink : ILogicalDeviceSessionMyRvLink, ILogicalDeviceSession
	{
		private long _lastActivatedTimestampMs;

		public bool IsActivated => _lastActivatedTimestampMs != 0;

		internal void ActivateSession()
		{
			_lastActivatedTimestampMs = LogicalDeviceFreeRunningTimer.ElapsedMilliseconds;
		}

		internal void DeactivateSession()
		{
			_lastActivatedTimestampMs = 0L;
		}
	}
}
