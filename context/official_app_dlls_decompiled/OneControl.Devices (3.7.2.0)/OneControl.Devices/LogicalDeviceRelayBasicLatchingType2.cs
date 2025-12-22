using System.Collections.Generic;
using System.Diagnostics;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayBasicLatchingType2 : LogicalDeviceRelayBasicLatching<LogicalDeviceRelayBasicStatusType2, LogicalDeviceRelayBasicCommandFactoryLatchingType2, LogicalDeviceRelayCapabilityType2>
	{
		private const uint DebugUpdateDeviceStatusChangedThrottleTimeMs = 60000u;

		private readonly Stopwatch _debugUpdateDeviceStatusChangedThrottleTimer = Stopwatch.StartNew();

		private uint _debugUpdateDeviceStatusChangedThrottleCount;

		public LogicalDeviceRelayBasicLatchingType2(ILogicalDeviceId logicalDeviceId, LogicalDeviceRelayCapabilityType2 capability, ILogicalDeviceService service = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, capability, service, isFunctionClassChangeable)
		{
		}

		protected override void DebugUpdateDeviceStatusChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, uint dataLength, string optionalText = "")
		{
			if (LogicalDeviceRelayStatusType2<RelayBasicOutputState>.IsSignificantlyDifferent(oldStatusData, statusData))
			{
				base.DebugUpdateDeviceStatusChanged(oldStatusData, statusData, dataLength, optionalText);
				_debugUpdateDeviceStatusChangedThrottleTimer.Restart();
				_debugUpdateDeviceStatusChangedThrottleCount = 1u;
			}
			else if (_debugUpdateDeviceStatusChangedThrottleCount != 0 && _debugUpdateDeviceStatusChangedThrottleTimer.ElapsedMilliseconds < 60000)
			{
				_debugUpdateDeviceStatusChangedThrottleCount++;
			}
			else
			{
				base.DebugUpdateDeviceStatusChanged(oldStatusData, statusData, dataLength, $" found {_debugUpdateDeviceStatusChangedThrottleCount} changes over {60000u}ms");
				_debugUpdateDeviceStatusChangedThrottleTimer.Restart();
				_debugUpdateDeviceStatusChangedThrottleCount = 1u;
			}
		}
	}
}
