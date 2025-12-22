using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDevicePowerMonitorSetecStatus : LogicalDeviceStatusPacketMutable
	{
		private const int MinimumStatusPacketSize = 1;

		public const int ModeByteIndex = 0;

		public const byte ModeBitmask = 7;

		public PowerMonitorSetecMode Mode => PowerMonitorSetecModeFromRawValue(GetByte(7, 0));

		public LogicalDevicePowerMonitorSetecStatus()
			: base(1u)
		{
		}

		public LogicalDevicePowerMonitorSetecStatus(PowerMonitorSetecMode mode)
		{
			SetMode(mode);
		}

		public override string ToString()
		{
			return $"Power Monitor: {Mode}";
		}

		public static PowerMonitorSetecMode PowerMonitorSetecModeFromRawValue(byte rawValue)
		{
			return (rawValue & 7) switch
			{
				0 => PowerMonitorSetecMode.Unknown, 
				1 => PowerMonitorSetecMode.TurningOn, 
				2 => PowerMonitorSetecMode.Off, 
				3 => PowerMonitorSetecMode.TurningOff, 
				4 => PowerMonitorSetecMode.On, 
				5 => PowerMonitorSetecMode.HardwareSwitchOn, 
				_ => PowerMonitorSetecMode.Unknown, 
			};
		}

		public void SetMode(PowerMonitorSetecMode mode)
		{
			SetByte(7, (byte)mode, 0);
		}
	}
}
