using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.ChassisInfo;

namespace OneControl.Devices
{
	public class LogicalDeviceChassisInfoStatus : LogicalDeviceStatusPacketMutable, ILogicalDeviceStatus<LogicalDeviceChassisInfoStatusSerializable>, ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged
	{
		private const int MinimumStatusPacketSize = 4;

		private const float VoltageConversionFactor = 16f;

		private const int ChassisInfoIndex = 0;

		private const int TowableInfoIndex = 1;

		private const int TowableBatteryVoltageIndex = 2;

		private const int TowableBrakeVoltageIndex = 3;

		public const int UnknownVoltageRaw = 255;

		public const float UnknownVoltage = -1f;

		private static readonly BitPositionValue ParkBrakeBitPosition = new BitPositionValue(3u);

		private static readonly BitPositionValue IgnitionPowerSignalBitPosition = new BitPositionValue(12u);

		private static readonly BitPositionValue TowableLeftTurnSignalBitPosition = new BitPositionValue(3u, 1);

		private static readonly BitPositionValue TowableRightTurnSignalBitPosition = new BitPositionValue(12u, 1);

		private static readonly BitPositionValue TowableRunningLightsSignalBitPosition = new BitPositionValue(48u, 1);

		private static readonly BitPositionValue TowableBackupLightsSignalBitPosition = new BitPositionValue(192u, 1);

		public ParkBrake ParkBreak => (ParkBrake)GetValue(ParkBrakeBitPosition);

		public IgnitionPowerSignal IgnitionPowerSignal => (IgnitionPowerSignal)GetValue(IgnitionPowerSignalBitPosition);

		public TowableSignalLightState TowableLeftTurnSignal
		{
			get
			{
				if (base.Size > 1)
				{
					return (TowableSignalLightState)GetValue(TowableLeftTurnSignalBitPosition);
				}
				return TowableSignalLightState.Unknown;
			}
		}

		public TowableSignalLightState TowableRightTurnSignal
		{
			get
			{
				if (base.Size > 1)
				{
					return (TowableSignalLightState)GetValue(TowableRightTurnSignalBitPosition);
				}
				return TowableSignalLightState.Unknown;
			}
		}

		public TowableSignalLightState TowableRunningLightsSignal
		{
			get
			{
				if (base.Size > 1)
				{
					return (TowableSignalLightState)GetValue(TowableRunningLightsSignalBitPosition);
				}
				return TowableSignalLightState.Unknown;
			}
		}

		public TowableSignalLightState TowableBackupLightsSignal
		{
			get
			{
				if (base.Size > 1)
				{
					return (TowableSignalLightState)GetValue(TowableBackupLightsSignalBitPosition);
				}
				return TowableSignalLightState.Unknown;
			}
		}

		public bool IsTowableBatteryVoltageValid
		{
			get
			{
				if (base.Size > 2)
				{
					return base.Data[2] != byte.MaxValue;
				}
				return false;
			}
		}

		public float TowableBatteryVoltage
		{
			get
			{
				if (!IsTowableBatteryVoltageValid)
				{
					return -1f;
				}
				return (float)(int)base.Data[2] / 16f;
			}
		}

		public bool IsTowableBrakeVoltageValid
		{
			get
			{
				if (base.Size > 3)
				{
					return base.Data[3] != byte.MaxValue;
				}
				return false;
			}
		}

		public float TowableBrakeVoltageVoltage
		{
			get
			{
				if (!IsTowableBrakeVoltageValid)
				{
					return -1f;
				}
				return (float)(int)base.Data[3] / 16f;
			}
		}

		public void SetParkBreak(ParkBrake parkBrake)
		{
			SetValue((uint)parkBrake, ParkBrakeBitPosition);
		}

		public void SetIgnitionPowerSignal(IgnitionPowerSignal ignitionPowerSignal)
		{
			SetValue((uint)ignitionPowerSignal, IgnitionPowerSignalBitPosition);
		}

		public void SetTowableLeftTurnSignal(TowableSignalLightState lightState)
		{
			SetValue((uint)lightState, TowableLeftTurnSignalBitPosition);
		}

		public void SetTowableRightTurnSignal(TowableSignalLightState lightState)
		{
			SetValue((uint)lightState, TowableRightTurnSignalBitPosition);
		}

		public void SetTowableRunningLightsSignal(TowableSignalLightState lightState)
		{
			SetValue((uint)lightState, TowableRunningLightsSignalBitPosition);
		}

		public void SetTowableBackupLightsSignal(TowableSignalLightState lightState)
		{
			SetValue((uint)lightState, TowableBackupLightsSignalBitPosition);
		}

		public void SetBatteryVoltage(float voltage)
		{
			byte value = (byte)(voltage * 16f);
			SetByte(byte.MaxValue, value, 2);
		}

		public void SetBrakeVoltage(float voltage)
		{
			byte value = (byte)(voltage * 16f);
			SetByte(byte.MaxValue, value, 3);
		}

		public LogicalDeviceChassisInfoStatus()
			: base(4u)
		{
		}

		public LogicalDeviceChassisInfoStatus(byte rawStatus, byte rawTowableStatus, byte rawTowableBatteryVoltage, byte rawTowableBrakeVoltage)
			: base(4u)
		{
			base.Data[0] = rawStatus;
			base.Data[1] = rawTowableStatus;
			base.Data[2] = rawTowableBatteryVoltage;
			base.Data[3] = rawTowableBrakeVoltage;
		}

		public LogicalDeviceChassisInfoStatusSerializable CopyAsSerializable()
		{
			return new LogicalDeviceChassisInfoStatusSerializable(this);
		}
	}
}
