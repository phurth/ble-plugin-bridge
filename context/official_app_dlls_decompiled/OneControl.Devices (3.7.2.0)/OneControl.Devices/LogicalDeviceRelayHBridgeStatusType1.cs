using IDS.Core.IDS_CAN;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.AwningSensor;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayHBridgeStatusType1 : LogicalDeviceStatusPacketMutable, ILogicalDeviceRelayHBridgeStatus, IDeviceDataPacketMutable, IDeviceDataPacket
	{
		public const BasicBitMask FaultBit = BasicBitMask.BitMask0X40;

		public const BasicBitMask Relay2StateBit = BasicBitMask.BitMask0X02;

		public const BasicBitMask Relay1StateBit = BasicBitMask.BitMask0X01;

		public bool IsStopped
		{
			get
			{
				if (!GetBit(BasicBitMask.BitMask0X01))
				{
					return !GetBit(BasicBitMask.BitMask0X02);
				}
				return false;
			}
		}

		public bool IsValid => base.HasData;

		public bool IsFaulted => GetBit(BasicBitMask.BitMask0X40);

		public bool IsHoming => false;

		public bool UserClearRequired => IsFaulted;

		public DTC_ID UserMessageDtc => DTC_ID.UNKNOWN;

		public AwningWindStrength WindProtectionLevel => AwningWindStrength.Unknown;

		public bool CommandForwardNotHazardous => true;

		public bool CommandReverseNotHazardous => true;

		public bool IsPositionKnown => Position <= 100;

		public byte Position => byte.MaxValue;

		public bool IsCurrentDrawAmpsKnown => false;

		public float CurrentDrawAmps => 0f;

		public bool Relay1State(ILogicalDeviceId logicalId)
		{
			return GetBit(BasicBitMask.BitMask0X01);
		}

		public bool Relay2State(ILogicalDeviceId logicalId)
		{
			return GetBit(BasicBitMask.BitMask0X02);
		}

		public RelayHBridgeDirection GetHBridgeDirection(ILogicalDeviceId logicalId)
		{
			return RelayHBridgeDirectionExtension.ConvertToHBridgeDirection(GetBit(BasicBitMask.BitMask0X01), GetBit(BasicBitMask.BitMask0X02), logicalId);
		}

		public void SetState(bool relay1State, bool relay2State, ILogicalDeviceId logicalId)
		{
			SetBit(BasicBitMask.BitMask0X02, relay2State);
			SetBit(BasicBitMask.BitMask0X01, relay1State);
		}

		public bool SetFault(bool isFaulted)
		{
			SetBit(BasicBitMask.BitMask0X40, isFaulted);
			return true;
		}

		public bool SetUserClearRequired(bool disabled)
		{
			SetFault(disabled);
			return true;
		}

		public bool SetUserMessageDtc(DTC_ID dtc)
		{
			return false;
		}

		public bool SetCommandForwardNotHazardous(bool notHazardous)
		{
			return false;
		}

		public bool SetCommandReverseNotHazardous(bool notHazardous)
		{
			return false;
		}

		public bool CommandForwardAllowed(ILogicalDeviceWithStatus<ILogicalDeviceRelayHBridgeStatus> logicalDevice)
		{
			return RelayHBridgeDirection.Forward.Allowed(logicalDevice);
		}

		public bool CommandReverseAllowed(ILogicalDeviceWithStatus<ILogicalDeviceRelayHBridgeStatus> logicalDevice)
		{
			return RelayHBridgeDirection.Reverse.Allowed(logicalDevice);
		}

		public bool SetPosition(byte position)
		{
			return false;
		}

		public bool SetCurrentDrawAmps(float voltage)
		{
			return false;
		}

		public ILogicalDeviceRelayHBridgeStatus CopyStatus()
		{
			LogicalDeviceRelayHBridgeStatusType1 logicalDeviceRelayHBridgeStatusType = new LogicalDeviceRelayHBridgeStatusType1();
			logicalDeviceRelayHBridgeStatusType.Update(base.Data, base.Data.Length);
			return logicalDeviceRelayHBridgeStatusType;
		}

		public override string ToString()
		{
			return "OutputState = Type1";
		}
	}
}
