using System.ComponentModel;
using IDS.Core.IDS_CAN;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface ILogicalDeviceRelayBasicStatus : ILogicalDeviceStatus<ILogicalDeviceRelayBasicStatusSerializable>, ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged
	{
		bool IsFaulted { get; }

		bool RelayState { get; }

		bool IsOn { get; }

		bool IsOff { get; }

		bool IsValid { get; }

		bool UserClearRequired { get; }

		DTC_ID UserMessageDtc { get; }

		bool CommandOnAllowed { get; }

		bool IsPositionKnown { get; }

		byte Position { get; }

		bool IsCurrentDrawAmpsKnown { get; }

		float CurrentDrawAmps { get; }

		void SetState(bool isOn);

		bool SetFault(bool isFaulted);

		bool SetUserClearRequired(bool disabled);

		bool SetUserMessageDtc(DTC_ID dtc);

		bool SetCommandOnAllowed(bool allowed);

		bool SetPosition(byte position);

		bool SetCurrentDrawAmps(float voltage);

		ILogicalDeviceRelayBasicStatus CopyStatus();
	}
}
