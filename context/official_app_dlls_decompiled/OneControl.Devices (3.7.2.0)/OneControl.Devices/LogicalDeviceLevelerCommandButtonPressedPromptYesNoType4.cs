using System.Collections.Generic;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandButtonPressedPromptYesNoType4 : LogicalDeviceLevelerCommandButtonPressedType4<LogicalDeviceLevelerButtonYesNoType4>
	{
		protected List<LogicalDeviceLevelerScreenType4> ValidScreens { get; } = new List<LogicalDeviceLevelerScreenType4> { LogicalDeviceLevelerScreenType4.PromptYesNo };


		protected override bool IsScreenSupported(LogicalDeviceLevelerScreenType4 screenSelected)
		{
			return ValidScreens.Contains(screenSelected);
		}

		public LogicalDeviceLevelerCommandButtonPressedPromptYesNoType4(LogicalDeviceLevelerButtonYesNoType4 buttonsPressed, int commandResponseTimeMs = 200)
			: base(LogicalDeviceLevelerScreenType4.PromptYesNo, buttonsPressed, commandResponseTimeMs)
		{
		}
	}
}
