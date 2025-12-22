using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Interfaces;

namespace OneControl.Devices.OneControlTouchPanel
{
	public class LogicalDeviceOneControlTouchPanel : LogicalDevice<LogicalDeviceOneControlTouchPanelStatus, ILogicalDeviceOneControlTouchPanelCapability>, ILogicalDeviceOneControlTouchPanelDirect, ILogicalDeviceOneControlTouchPanel, IOneControlTouchPanel, ILogicalDeviceWithCapability<ILogicalDeviceOneControlTouchPanelCapability>, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatus<LogicalDeviceOneControlTouchPanelStatus>, ILogicalDeviceWithStatus, ILogicalDeviceWithStatusUpdate<LogicalDeviceOneControlTouchPanelStatus>, IHighResolutionTankSupport, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink
	{
		private const string LogTag = "LogicalDeviceOneControlTouchPanel";

		private static readonly Version HighResolutionTankSupportVersion = Version.Parse("10.0.0");

		public LogicalDeviceOneControlTouchPanel(ILogicalDeviceId logicalDeviceId, ILogicalDeviceOneControlTouchPanelCapability capability, LogicalDeviceOneControlTouchPanelStatus status, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, status, capability, service, isFunctionClassChangeable)
		{
		}

		public LogicalDeviceOneControlTouchPanel(ILogicalDeviceId logicalDeviceId, LogicalDeviceOneControlTouchPanelCapability capability, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: this(logicalDeviceId, capability, new LogicalDeviceOneControlTouchPanelStatus(), service, isFunctionClassChangeable)
		{
		}

		public async Task<bool> TryAreHighResolutionTanksSupportedAsync(CancellationToken cancellationToken)
		{
			string text = await GetSoftwarePartNumberAsync(cancellationToken);
			try
			{
				return Version.Parse(text) >= HighResolutionTankSupportVersion;
			}
			catch (Exception arg)
			{
				TaggedLog.Error("LogicalDeviceOneControlTouchPanel", $"Error parsing OCTP partNumber: {text} {arg}");
				return false;
			}
		}

		NETWORK_STATUS ILogicalDeviceIdsCan.get_LastReceivedNetworkStatus()
		{
			return base.LastReceivedNetworkStatus;
		}
	}
}
