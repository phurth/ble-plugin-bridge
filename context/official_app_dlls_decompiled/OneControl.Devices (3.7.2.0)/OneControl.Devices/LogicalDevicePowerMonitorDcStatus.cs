using System;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDevicePowerMonitorDcStatus : LogicalDeviceStatusPacketMutable
	{
		private const int MinimumStatusPacketSize = 7;

		public const int VoltageStartByteIndex = 0;

		public const int CurrentStartByteIndex = 2;

		public const int RemainingCapacityPercentageByteIndex = 4;

		public const int ChargingByteIndex = 4;

		public const int EstimatedRemainingMinutesByteStartIndex = 5;

		public const ushort VoltageInvalidValue = ushort.MaxValue;

		public const ushort CurrentInvalidValue = ushort.MaxValue;

		public const ushort EstimatedRemainingMinutesInvalidValue = ushort.MaxValue;

		public const byte RemainingCapacityPercentageBitmask = 127;

		public const BasicBitMask ChargingBitmask = BasicBitMask.BitMask0X80;

		private string _voltageStr
		{
			get
			{
				if (!IsVoltageValid)
				{
					return "-- V";
				}
				return $"{Voltage} V";
			}
		}

		private string _currentStr
		{
			get
			{
				if (!IsCurrentValid)
				{
					return "-- Amps";
				}
				return $"{Current} Amps";
			}
		}

		private string _remainingCapacityPercentageStr
		{
			get
			{
				if (!IsRemainingCapacityPercentageValid)
				{
					return "-- %";
				}
				return $"{RemainingCapacityPercentage} %";
			}
		}

		private string _estimatedRemainingMinutesStr
		{
			get
			{
				if (!IsEstimatedRemainingMinutesValid)
				{
					return "invalid";
				}
				return TimeSpan.FromMinutes((int)EstimatedRemainingMinutes).ToString("hh\\:mm") ?? "";
			}
		}

		public float Voltage => FixedPointUnsignedBigEndian16X16.ToFloat(base.Data, 0u);

		public float Current => FixedPointUnsignedBigEndian16X16.ToFloat(base.Data, 2u);

		public byte RemainingCapacityPercentage => GetByte(127, 4);

		public ushort EstimatedRemainingMinutes => GetUInt16(5u);

		public bool IsVoltageValid => FixedPointUnsignedBigEndian16X16.ToFixedPoint(base.Data, 0u) != 65535;

		public bool IsCurrentValid => FixedPointUnsignedBigEndian16X16.ToFixedPoint(base.Data, 2u) != 65535;

		public bool IsRemainingCapacityPercentageValid => RemainingCapacityPercentage <= 100;

		public bool IsCharging => GetBit(BasicBitMask.BitMask0X80, 4);

		public bool IsEstimatedRemainingMinutesValid => EstimatedRemainingMinutes != ushort.MaxValue;

		public LogicalDevicePowerMonitorDcStatus()
			: base(7u)
		{
		}

		public LogicalDevicePowerMonitorDcStatus(float voltage, float current, byte remainingCapacityPercentage, bool charging, ushort estimatedRemainingMinutes = ushort.MaxValue)
		{
			SetVoltage(voltage);
			SetCurrent(current);
		}

		public override string ToString()
		{
			return $"DC Power: {_voltageStr} / {_currentStr} Percent Remaining: {_remainingCapacityPercentageStr} Time Remaining: {_estimatedRemainingMinutesStr} charging: {IsCharging}";
		}

		public void SetVoltage(float voltage)
		{
			SetFixedPoint(FixedPointType.UnsignedBigEndian16x16, voltage, 0u);
		}

		public void SetCurrent(float current)
		{
			SetFixedPoint(FixedPointType.UnsignedBigEndian16x16, current, 2u);
		}

		public void SetRemainingCapacityPercentage(byte percent)
		{
			SetByte(127, percent, 4);
		}

		public void SetEstimatedRemainingMinutes(ushort minutes)
		{
			SetUInt16(minutes, 5);
		}
	}
}
