using System.Collections.Generic;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandButtonPressedHomeType4 : LogicalDeviceLevelerCommandButtonPressedType4<LogicalDeviceLevelerButtonHomeType4>
	{
		protected List<LogicalDeviceLevelerScreenType4> ValidScreens { get; } = new List<LogicalDeviceLevelerScreenType4> { LogicalDeviceLevelerScreenType4.Home };


		protected override bool IsScreenSupported(LogicalDeviceLevelerScreenType4 screenSelected)
		{
			return ValidScreens.Contains(screenSelected);
		}

		public LogicalDeviceLevelerCommandButtonPressedHomeType4(LogicalDeviceLevelerButtonHomeType4 buttonsPressed, int commandResponseTimeMs = 200)
			: base(LogicalDeviceLevelerScreenType4.Home, buttonsPressed, commandResponseTimeMs)
		{
		}
	}
}
