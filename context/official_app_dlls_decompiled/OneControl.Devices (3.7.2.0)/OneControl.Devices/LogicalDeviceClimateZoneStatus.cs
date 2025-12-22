using System.ComponentModel;
using System.Linq;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Hvac;

namespace OneControl.Devices
{
	public class LogicalDeviceClimateZoneStatus : LogicalDeviceStatusPacketMutable, ILogicalDeviceStatus<LogicalDeviceClimateZoneStatusSerializable>, ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged
	{
		public const float MinimumValidTemperatureFahrenheit = -40f;

		public const float MaximumValidTemperatureFahrenheit = 140f;

		private const int MinimumStatusPacketSize = 8;

		public const uint CommandByteIndex = 0u;

		public const uint LowTripTempByteIndex = 1u;

		public const uint HighTripTempByteIndex = 2u;

		public const uint StatusByteIndex = 3u;

		public const uint IndoorTempStartByteIndex = 4u;

		public const uint OutdoorTempStartByteIndex = 6u;

		public const byte StatusBitmaskZoneStatus = 143;

		public const byte StatusBitmaskFailedThermistor = 128;

		public readonly ushort[] TemperatureSensorInvalidValueList = new ushort[2] { 32768, 12272 };

		private string _outdoorTemperatureFahrenheitStr
		{
			get
			{
				if (!IsOutdoorTemperatureSensorValid)
				{
					return "Invalid";
				}
				return $"{OutdoorTemperatureFahrenheit}\ufffdF";
			}
		}

		private string _indoorTemperatureFahrenheitStr
		{
			get
			{
				if (!IsIndoorTemperatureSensorValid)
				{
					return "Invalid";
				}
				return $"{IndoorTemperatureFahrenheit}\ufffdF";
			}
		}

		public float OutdoorTemperatureFahrenheit => FixedPointSignedBigEndian8X8.ToFloat(base.Data, 6u);

		public float IndoorTemperatureFahrenheit => FixedPointSignedBigEndian8X8.ToFloat(base.Data, 4u);

		public bool IsOutdoorTemperatureSensorValid => !Enumerable.Contains(TemperatureSensorInvalidValueList, FixedPointUnsignedBigEndian8X8.ToFixedPoint(base.Data, 6u));

		public bool IsIndoorTemperatureSensorValid => !Enumerable.Contains(TemperatureSensorInvalidValueList, FixedPointUnsignedBigEndian8X8.ToFixedPoint(base.Data, 4u));

		public ClimateZoneStatus ZoneStatus
		{
			get
			{
				try
				{
					return ZoneModeFromRawValue((byte)(base.Data[3] & 0x8Fu));
				}
				catch
				{
					return ClimateZoneStatus.FailOff;
				}
			}
		}

		public byte HighTripTemperatureFahrenheit => base.Data[2];

		public byte LowTripTemperatureFahrenheit => base.Data[1];

		public ClimateZoneCommand ProgramedCommand => base.Data[0];

		public bool IsFailedThermistor => (base.Data[3] & 0x80) != 0;

		public bool IsError => IsFailedThermistor;

		public bool IsOutdoorTemperatureWithinValidRange
		{
			get
			{
				if (IsOutdoorTemperatureSensorValid)
				{
					if (OutdoorTemperatureFahrenheit >= -40f)
					{
						return OutdoorTemperatureFahrenheit <= 140f;
					}
					return false;
				}
				return false;
			}
		}

		public bool IsIndoorTemperatureWithinValidRange
		{
			get
			{
				if (IsIndoorTemperatureSensorValid)
				{
					if (IndoorTemperatureFahrenheit >= -40f)
					{
						return IndoorTemperatureFahrenheit <= 140f;
					}
					return false;
				}
				return false;
			}
		}

		public LogicalDeviceClimateZoneStatus()
			: base(8u)
		{
		}

		public LogicalDeviceClimateZoneStatusSerializable CopyAsSerializable()
		{
			return new LogicalDeviceClimateZoneStatusSerializable(this);
		}

		public LogicalDeviceClimateZoneStatus(ClimateZoneHeatMode heatMode, ClimateZoneHeatSource heatSource, ClimateZoneFanMode fanMode, ClimateZoneStatus zoneStatus, byte lowTripTempF, byte highTripTempF, float indoorTempF, float outdoorTempF)
		{
			SetZoneStatus(zoneStatus);
			ClimateZoneCommand commandStatus = new ClimateZoneCommand(heatMode, heatSource, fanMode);
			SetCommandStatus(commandStatus);
			SetLowTripTemperatureFahrenheit(lowTripTempF);
			SetHighTripTemperatureFahrenheit(highTripTempF);
			SetIndoorTemperatureFahrenheit(indoorTempF);
			SetOutdoorTemperatureFahrenheit(outdoorTempF);
		}

		public LogicalDeviceClimateZoneStatus(LogicalDeviceClimateZoneStatus originalStatus)
		{
			byte[] data = originalStatus.Data;
			Update(data, data.Length);
		}

		public override string ToString()
		{
			return $"ZoneMode: {ZoneStatus}, OutdoorTemp: {_outdoorTemperatureFahrenheitStr}, IndoorTemp: {_indoorTemperatureFahrenheitStr}, ProgramedCommand: {ProgramedCommand}, HighTrip: {HighTripTemperatureFahrenheit}, LowTrip: {LowTripTemperatureFahrenheit}";
		}

		public static ClimateZoneStatus ZoneModeFromRawValue(byte rawValue)
		{
			return rawValue switch
			{
				0 => ClimateZoneStatus.Off, 
				1 => ClimateZoneStatus.Idle, 
				2 => ClimateZoneStatus.Cooling, 
				3 => ClimateZoneStatus.HeatingWithHeatPump, 
				4 => ClimateZoneStatus.HeatingWithElectric, 
				5 => ClimateZoneStatus.HeatingWithGasFurnace, 
				6 => ClimateZoneStatus.HeatingWithGasOverride, 
				7 => ClimateZoneStatus.DeadTime, 
				8 => ClimateZoneStatus.LoadShedding, 
				128 => ClimateZoneStatus.FailOff, 
				129 => ClimateZoneStatus.FailReserved17, 
				130 => ClimateZoneStatus.FailReserved18, 
				131 => ClimateZoneStatus.FailHeatingWithHeatPump, 
				132 => ClimateZoneStatus.FailHeatingWithElectric, 
				133 => ClimateZoneStatus.FailHeatingWithGasFurnace, 
				134 => ClimateZoneStatus.FailHeatingWithGasOverride, 
				135 => ClimateZoneStatus.FailReserved23, 
				136 => ClimateZoneStatus.FailReserved24, 
				_ => ClimateZoneStatus.Off, 
			};
		}

		public bool IsHeatOn()
		{
			ClimateZoneStatus zoneStatus = ZoneStatus;
			if (zoneStatus - 3 <= ClimateZoneStatus.HeatingWithHeatPump)
			{
				return true;
			}
			return false;
		}

		public bool IsCoolOn()
		{
			return ZoneStatus == ClimateZoneStatus.Cooling;
		}

		public void SetOutdoorTemperatureFahrenheit(float temperature)
		{
			SetFixedPoint(FixedPointType.SignedBigEndian8x8, temperature, 6u);
		}

		public void SetIndoorTemperatureFahrenheit(float temperature)
		{
			SetFixedPoint(FixedPointType.SignedBigEndian8x8, temperature, 4u);
		}

		public void SetZoneStatus(ClimateZoneStatus zoneStatus)
		{
			SetByte(143, (byte)zoneStatus, 3);
		}

		public void SetHighTripTemperatureFahrenheit(byte temperature)
		{
			SetByte(byte.MaxValue, temperature, 2);
		}

		public void SetLowTripTemperatureFahrenheit(byte temperature)
		{
			SetByte(byte.MaxValue, temperature, 1);
		}

		public void SetCommandStatus(ClimateZoneCommand zoneCommand)
		{
			SetByte(byte.MaxValue, zoneCommand, 0);
		}
	}
}
