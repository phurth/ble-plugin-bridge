using IDS.Core.IDS_CAN;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.AwningSensor;

namespace OneControl.Devices
{
	public interface ILogicalDeviceRelayHBridgeStatus : IDeviceDataPacketMutable, IDeviceDataPacket
	{
		bool IsFaulted { get; }

		bool IsStopped { get; }

		bool IsValid { get; }

		bool IsHoming { get; }

		bool UserClearRequired { get; }

		DTC_ID UserMessageDtc { get; }

		AwningWindStrength WindProtectionLevel { get; }

		bool CommandForwardNotHazardous { get; }

		bool CommandReverseNotHazardous { get; }

		bool IsPositionKnown { get; }

		byte Position { get; }

		bool IsCurrentDrawAmpsKnown { get; }

		float CurrentDrawAmps { get; }

		bool Relay1State(ILogicalDeviceId logicalId);

		bool Relay2State(ILogicalDeviceId logicalId);

		RelayHBridgeDirection GetHBridgeDirection(ILogicalDeviceId logicalId);

		bool CommandForwardAllowed(ILogicalDeviceWithStatus<ILogicalDeviceRelayHBridgeStatus> logicalDevice);

		bool CommandReverseAllowed(ILogicalDeviceWithStatus<ILogicalDeviceRelayHBridgeStatus> logicalDevice);

		void SetState(bool relay1State, bool relay2State, ILogicalDeviceId logicalId);

		bool SetFault(bool isFaulted);

		bool SetUserClearRequired(bool disabled);

		bool SetUserMessageDtc(DTC_ID dtc);

		bool SetCommandForwardNotHazardous(bool notHazardous);

		bool SetCommandReverseNotHazardous(bool notHazardous);

		bool SetPosition(byte position);

		bool SetCurrentDrawAmps(float voltage);

		ILogicalDeviceRelayHBridgeStatus CopyStatus();
	}
}
