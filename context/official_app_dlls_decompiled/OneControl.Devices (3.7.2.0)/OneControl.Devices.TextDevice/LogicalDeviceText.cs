using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices.TextDevice
{
	public class LogicalDeviceText : LogicalDevice<LogicalDeviceCapability>, ILogicalDeviceText, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ITextDevice
	{
		public const string LogTag = "LogicalDeviceText";

		public string Title => (base.LogicalId as LogicalDeviceTextId)?.Title ?? string.Empty;

		public string Notes => (base.LogicalId as LogicalDeviceTextId)?.Notes ?? string.Empty;

		public TimeSpan Duration => (base.LogicalId as LogicalDeviceTextId)?.Duration ?? default(TimeSpan);

		public bool IsReminder => (base.LogicalId as LogicalDeviceTextId)?.IsReminder ?? false;

		public override string DeviceName => Title;

		public override LogicalDeviceActiveConnection ActiveConnection => LogicalDeviceActiveConnection.Direct;

		public override Version ProtocolVersion => new Version(1, 0);

		public LogicalDeviceText(LogicalDeviceTextId logicalDeviceId, ILogicalDeviceService deviceService)
			: base((ILogicalDeviceId)logicalDeviceId, new LogicalDeviceCapability(), deviceService, isFunctionClassChangeable: false)
		{
		}

		public override string ToString()
		{
			return string.Format("TITLE[{0}] = \"{1}\", NOTES = {2}, DURATION = {3}", base.LogicalId.DeviceInstance, Title, (Notes.Length > 15) ? (Notes.Substring(0, 15) + "...") : Notes, Duration);
		}

		public override Task<string> GetSoftwarePartNumberAsync(CancellationToken cancelToken)
		{
			return Task.FromResult(string.Empty);
		}
	}
}
