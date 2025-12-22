using System;
using System.ComponentModel;
using System.Text;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayBasicStatusType1 : LogicalDeviceStatusPacketMutable, ILogicalDeviceRelayBasicStatus, ILogicalDeviceStatus<ILogicalDeviceRelayBasicStatusSerializable>, ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged
	{
		private const int MinimumStatusPacketSize = 1;

		public const BasicBitMask FaultBit = BasicBitMask.BitMask0X40;

		public const BasicBitMask RelayStateBit = BasicBitMask.BitMask0X01;

		public bool RelayState => GetBit(BasicBitMask.BitMask0X01);

		public bool IsOn => RelayState;

		public bool IsOff => !RelayState;

		public bool IsValid => base.HasData;

		public bool IsFaulted => GetBit(BasicBitMask.BitMask0X40);

		public bool UserClearRequired => IsFaulted;

		public DTC_ID UserMessageDtc => DTC_ID.UNKNOWN;

		public bool CommandOnAllowed => true;

		public bool IsPositionKnown => Position <= 100;

		public byte Position => byte.MaxValue;

		public bool IsCurrentDrawAmpsKnown => false;

		public float CurrentDrawAmps => 0f;

		public void SetState(bool isOn)
		{
			SetBit(BasicBitMask.BitMask0X01, isOn);
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

		public bool SetCommandOnAllowed(bool allowed)
		{
			return false;
		}

		public bool SetPosition(byte position)
		{
			return false;
		}

		public bool SetCurrentDrawAmps(float voltage)
		{
			return false;
		}

		public LogicalDeviceRelayBasicStatusType1()
			: base(1u)
		{
		}

		public ILogicalDeviceRelayBasicStatus CopyStatus()
		{
			LogicalDeviceRelayBasicStatusType1 logicalDeviceRelayBasicStatusType = new LogicalDeviceRelayBasicStatusType1();
			logicalDeviceRelayBasicStatusType.Update(base.Data, base.Data.Length);
			return logicalDeviceRelayBasicStatusType;
		}

		public ILogicalDeviceRelayBasicStatusSerializable CopyAsSerializable()
		{
			return new LogicalDeviceRelayBasicStatusType1Serializable(this);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder($"Relay Status: {IsOn}");
			try
			{
				if (IsFaulted)
				{
					stringBuilder.Append(", Faulted");
				}
				stringBuilder.Append(": " + base.Data.DebugDump());
			}
			catch (Exception ex)
			{
				stringBuilder.Append(Environment.NewLine + "    ERROR Trying to Get Device " + ex.Message);
			}
			return stringBuilder.ToString();
		}
	}
}
