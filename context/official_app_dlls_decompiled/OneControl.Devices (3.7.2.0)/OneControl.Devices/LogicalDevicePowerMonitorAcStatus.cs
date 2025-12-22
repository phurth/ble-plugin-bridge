using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDevicePowerMonitorAcStatus : LogicalDeviceStatusPacketMutable
	{
		private const int MinimumStatusPacketSize = 5;

		public const int VoltageStartByteIndex = 0;

		public const int CurrentStartByteIndex = 2;

		public const int QualityByteIndex = 4;

		public const ushort VoltageInvalidValue = ushort.MaxValue;

		public const ushort CurrentInvalidValue = ushort.MaxValue;

		public const byte PowerQualityBitmask = 15;

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

		public float Voltage => FixedPointUnsignedBigEndian16X16.ToFloat(base.Data, 0u);

		public float Current => FixedPointUnsignedBigEndian16X16.ToFloat(base.Data, 2u);

		public bool IsVoltageValid => FixedPointUnsignedBigEndian16X16.ToFixedPoint(base.Data, 0u) != 65535;

		public bool IsCurrentValid => FixedPointUnsignedBigEndian16X16.ToFixedPoint(base.Data, 2u) != 65535;

		public byte PowerQuality => GetByte(15, 4);

		public LogicalDevicePowerMonitorAcStatus()
			: base(5u)
		{
		}

		public LogicalDevicePowerMonitorAcStatus(float voltage, float current, byte powerQuality)
		{
			SetVoltage(voltage);
			SetCurrent(current);
			SetPowerQuality(powerQuality);
		}

		public override string ToString()
		{
			return $"AC Power: {_voltageStr} / {_currentStr} Quality: {PowerQuality}";
		}

		public void SetVoltage(float voltage)
		{
			SetFixedPoint(FixedPointType.UnsignedBigEndian16x16, voltage, 0u);
		}

		public void SetCurrent(float current)
		{
			SetFixedPoint(FixedPointType.UnsignedBigEndian16x16, current, 2u);
		}

		public void SetPowerQuality(byte quality)
		{
			SetByte(15, quality, 4);
		}
	}
}
