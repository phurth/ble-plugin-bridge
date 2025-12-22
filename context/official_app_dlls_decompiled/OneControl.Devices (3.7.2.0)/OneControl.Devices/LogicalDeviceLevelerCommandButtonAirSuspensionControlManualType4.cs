using System.Collections.Generic;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandButtonAirSuspensionControlManualType4 : LogicalDeviceLevelerCommandButtonPressedType4<LogicalDeviceLevelerButtonAirSuspensionType4>
	{
		protected List<LogicalDeviceLevelerScreenType4> ValidScreens { get; } = new List<LogicalDeviceLevelerScreenType4> { LogicalDeviceLevelerScreenType4.AirSuspensionControlManual };


		protected override bool IsScreenSupported(LogicalDeviceLevelerScreenType4 screenSelected)
		{
			return ValidScreens.Contains(screenSelected);
		}

		public LogicalDeviceLevelerCommandButtonAirSuspensionControlManualType4(LogicalDeviceLevelerButtonAirSuspensionType4 buttonsPressed, int commandResponseTimeMs = 200)
			: base(LogicalDeviceLevelerScreenType4.AirSuspensionControlManual, buttonsPressed, commandResponseTimeMs)
		{
		}
	}
}
