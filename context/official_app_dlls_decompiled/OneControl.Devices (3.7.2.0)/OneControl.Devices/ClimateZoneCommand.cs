namespace OneControl.Devices
{
	public struct ClimateZoneCommand
	{
		private const byte HeatModeBitmask0 = 1;

		private const byte HeatModeBitmask1 = 2;

		private const byte HeatModeBitmask2 = 4;

		private const byte ReservedBitmask = 8;

		private const byte HeatSourceBitmask0 = 16;

		private const byte HeatSourceBitmask1 = 32;

		private const byte FanModeBitmask0 = 64;

		private const byte FanModeBitmask1 = 128;

		private const byte HeatModeBitmask = 7;

		private const byte HeatSourceBitmask = 48;

		private const byte FanModeBitmask = 192;

		private const byte HeatModeBitShift = 0;

		private const byte HeatSourceBitShift = 4;

		private const byte FanModeBitShift = 6;

		private readonly byte _command;

		public ClimateZoneHeatMode HeatMode => (ClimateZoneHeatMode)UnpackValueFromCommand(_command, 7, 0);

		public ClimateZoneHeatSource HeatSource => (ClimateZoneHeatSource)UnpackValueFromCommand(_command, 48, 4);

		public ClimateZoneFanMode FanMode => (ClimateZoneFanMode)UnpackValueFromCommand(_command, 192, 6);

		public ClimateZoneCommand(byte newCommand)
		{
			_command = newCommand;
		}

		public ClimateZoneCommand(ClimateZoneHeatMode heatMode, ClimateZoneHeatSource heatSource, ClimateZoneFanMode fanSetting)
		{
			byte command = 0;
			command = PackValueIntoCommand(command, 7, 0, (byte)heatMode);
			command = PackValueIntoCommand(command, 48, 4, (byte)heatSource);
			command = (_command = PackValueIntoCommand(command, 192, 6, (byte)fanSetting));
		}

		public static implicit operator byte(ClimateZoneCommand newCommand)
		{
			return newCommand._command;
		}

		public static implicit operator ClimateZoneCommand(byte newCommand)
		{
			return new ClimateZoneCommand(newCommand);
		}

		private static byte UnpackValueFromCommand(byte command, byte commandBitmaskForValue, byte bitshift)
		{
			return (byte)((command & commandBitmaskForValue) >> (int)bitshift);
		}

		private static byte PackValueIntoCommand(byte command, byte commandBitmaskForValue, byte bitshift, byte value)
		{
			return (byte)((command & ~commandBitmaskForValue) | ((value << (int)bitshift) & commandBitmaskForValue));
		}

		public bool IsHeating()
		{
			if (HeatMode != ClimateZoneHeatMode.Both)
			{
				return HeatMode == ClimateZoneHeatMode.Heating;
			}
			return true;
		}

		public override string ToString()
		{
			return $"[ClimateZoneCommand: FanMode={FanMode}, HeatMode={HeatMode}, HeatSource={HeatSource}]";
		}
	}
}
