using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayBasicStatusType2 : LogicalDeviceRelayStatusType2<RelayBasicOutputState>, ILogicalDeviceRelayBasicStatus, ILogicalDeviceStatus<ILogicalDeviceRelayBasicStatusSerializable>, ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged
	{
		public const BasicBitMask OnCommandAllowedBitmask = BasicBitMask.BitMask0X80;

		public override RelayBasicOutputState State
		{
			get
			{
				RelayBasicOutputState rawOutputState = (RelayBasicOutputState)base.RawOutputState;
				if ((uint)rawOutputState <= 1u)
				{
					return rawOutputState;
				}
				return RelayBasicOutputState.Unknown;
			}
		}

		public bool RelayState => State == RelayBasicOutputState.On;

		public bool IsOn => RelayState;

		public bool IsOff => !RelayState;

		public bool CommandOnAllowed
		{
			get
			{
				if (base.IsValid)
				{
					return GetBit(BasicBitMask.BitMask0X80, 0);
				}
				return false;
			}
		}

		public void SetState(bool isOn)
		{
			SetState(isOn ? RelayBasicOutputState.On : RelayBasicOutputState.Off);
		}

		public bool SetCommandOnAllowed(bool allowed)
		{
			SetBit(BasicBitMask.BitMask0X80, allowed, 0);
			return true;
		}

		public LogicalDeviceRelayBasicStatusType2()
			: base(6u)
		{
		}

		public ILogicalDeviceRelayBasicStatus CopyStatus()
		{
			LogicalDeviceRelayBasicStatusType2 logicalDeviceRelayBasicStatusType = new LogicalDeviceRelayBasicStatusType2();
			logicalDeviceRelayBasicStatusType.Update(base.Data, base.Data.Length);
			return logicalDeviceRelayBasicStatusType;
		}

		public ILogicalDeviceRelayBasicStatusSerializable CopyAsSerializable()
		{
			return new LogicalDeviceRelayBasicStatusType2Serializable(this);
		}
	}
}
