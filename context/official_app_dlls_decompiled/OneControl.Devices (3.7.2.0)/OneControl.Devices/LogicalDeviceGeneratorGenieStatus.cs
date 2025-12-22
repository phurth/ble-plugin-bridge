using System.ComponentModel;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceGeneratorGenieStatus : LogicalDeviceStatusPacketMutable, ILogicalDeviceStatus<LogicalDeviceGeneratorGenieStatusSerializable>, ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged
	{
		private const int MinimumStatusPacketSize = 5;

		public const byte StateBitMask = 7;

		public const BasicBitMask QuietHoursBitMask = BasicBitMask.BitMask0X80;

		public const uint StatusByteIndex = 0u;

		public const uint BatteryVoltageStartingIndex = 1u;

		public const uint TemperatureStartingIndex = 3u;

		public const ushort TemperatureSensorInvalidValue = 32767;

		public const ushort TemperatureSensorNotSupportedValue = 32768;

		public GeneratorState State => (base.Data[0] & 7) switch
		{
			0 => GeneratorState.Off, 
			1 => GeneratorState.Priming, 
			2 => GeneratorState.Starting, 
			3 => GeneratorState.Running, 
			4 => GeneratorState.Stopping, 
			_ => GeneratorState.Unknown, 
		};

		public bool QuietHoursActive => (base.Data[0] & 0x80) != 0;

		public float BatteryVoltage => FixedPointUnsignedBigEndian8X8.ToFloat(base.Data, 1u);

		public bool IsTemperatureSupported => (ushort)FixedPointSignedBigEndian8X8.ToFixedPoint(base.Data, 3u) != 32768;

		public bool IsTemperatureSensorValid
		{
			get
			{
				if (IsTemperatureSupported)
				{
					return FixedPointUnsignedBigEndian8X8.ToFixedPoint(base.Data, 3u) != 32767;
				}
				return false;
			}
		}

		public float TemperatureFahrenheit => FixedPointSignedBigEndian8X8.ToFloat(base.Data, 3u);

		public void SetState(GeneratorState state)
		{
			SetByte(7, (byte)state, 0);
		}

		public void SetQuietHoursActive(bool active)
		{
			SetBit(BasicBitMask.BitMask0X80, active);
		}

		public void SetBatteryVoltage(float voltage)
		{
			SetFixedPoint(FixedPointType.UnsignedBigEndian8x8, voltage, 1u);
		}

		public void SetTemperature(float temperature)
		{
			SetFixedPoint(FixedPointType.SignedBigEndian8x8, temperature, 3u);
		}

		public LogicalDeviceGeneratorGenieStatus()
			: base(5u)
		{
		}

		public LogicalDeviceGeneratorGenieStatus(LogicalDeviceGeneratorGenieStatus originalStatus)
		{
			byte[] data = originalStatus.Data;
			Update(data, data.Length);
		}

		public LogicalDeviceGeneratorGenieStatusSerializable CopyAsSerializable()
		{
			return new LogicalDeviceGeneratorGenieStatusSerializable(State, QuietHoursActive, BatteryVoltage, TemperatureFahrenheit);
		}
	}
}
