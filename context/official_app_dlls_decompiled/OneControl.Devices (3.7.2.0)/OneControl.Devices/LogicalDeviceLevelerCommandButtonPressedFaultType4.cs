using System.Collections.Generic;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandButtonPressedFaultType4 : LogicalDeviceLevelerCommandButtonPressedType4<LogicalDeviceLevelerButtonOkType4>
	{
		protected List<LogicalDeviceLevelerScreenType4> ValidScreens { get; } = new List<LogicalDeviceLevelerScreenType4> { LogicalDeviceLevelerScreenType4.PromptFault };


		protected override bool IsScreenSupported(LogicalDeviceLevelerScreenType4 screenSelected)
		{
			return ValidScreens.Contains(screenSelected);
		}

		public LogicalDeviceLevelerCommandButtonPressedFaultType4(LogicalDeviceLevelerButtonOkType4 buttonsPressed, int commandResponseTimeMs = 200)
			: base(LogicalDeviceLevelerScreenType4.PromptFault, buttonsPressed, commandResponseTimeMs)
		{
		}
	}
}
