using System.ComponentModel;
using IDS.Core.IDS_CAN;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IRelayBasic : ISwitchableDevice, ILogicalDeviceSwitchableReadonly, IDevicesCommon, INotifyPropertyChanged
	{
		bool Off { get; }

		bool Faulted { get; }

		bool UserClearRequired { get; }

		DTC_ID UserMessageDtc { get; }

		bool IsValid { get; }
	}
}
