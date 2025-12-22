using System;
using System.Collections.Generic;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceClimateZoneCommand : LogicalDeviceCommandPacket
	{
		private const int CommandPacketSize = 3;

		private const uint CommandByteIndex = 0u;

		private const uint LowTripTemperatureFahrenheitByteIndex = 1u;

		private const uint HighTripTemperatureFahrenheitByteIndex = 2u;

		public ClimateZoneCommand Command => base.Data[0];

		public ClimateZoneHeatMode HeatMode => Command.HeatMode;

		public ClimateZoneHeatSource HeatSource => Command.HeatSource;

		public ClimateZoneFanMode FanMode => Command.FanMode;

		public byte LowTripTemperatureFahrenheit => base.Data[1];

		public byte HighTripTemperatureFahrenheit => base.Data[2];

		public IReadOnlyList<byte> DataMinimum => new ArraySegment<byte>(base.Data, 0, 3);

		public bool IsHeating()
		{
			return Command.IsHeating();
		}

		public LogicalDeviceClimateZoneCommand(ClimateZoneCommand command, byte lowTripTemperatureFahrenheit, byte highTripTemperatureFahrenheit)
			: base(0, new byte[3])
		{
			base.Data[0] = command;
			base.Data[1] = lowTripTemperatureFahrenheit;
			base.Data[2] = highTripTemperatureFahrenheit;
		}

		public LogicalDeviceClimateZoneCommand(ClimateZoneHeatMode heatMode, ClimateZoneHeatSource heatSource, ClimateZoneFanMode fanMode, byte lowTripTemperatureFahrenheit, byte highTripTemperatureFahrenheit)
			: this(new ClimateZoneCommand(heatMode, heatSource, fanMode), lowTripTemperatureFahrenheit, highTripTemperatureFahrenheit)
		{
		}

		public LogicalDeviceClimateZoneCommand(IReadOnlyList<byte> data)
			: base(0, data)
		{
			if (data.Count != 3)
			{
				throw new ArgumentOutOfRangeException("data", $"Buffer size is {data.Count} when expecting {3}: {data.DebugDump(0, data.Count)}");
			}
		}

		public override string ToString()
		{
			return $"[ClimateZoneCommand: FanMode={FanMode}, HeatMode={HeatMode}, HeatSource={HeatSource}, LowTrip={LowTripTemperatureFahrenheit} HighTrip={HighTripTemperatureFahrenheit}]";
		}
	}
}
