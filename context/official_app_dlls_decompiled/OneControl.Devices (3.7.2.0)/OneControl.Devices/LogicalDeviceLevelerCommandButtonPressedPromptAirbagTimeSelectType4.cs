using System.Collections.Generic;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandButtonPressedPromptAirbagTimeSelectType4 : LogicalDeviceLevelerCommandButtonPressedType4<LogicalDeviceLevelerButtonAirbagTimeSelectType4>
	{
		protected List<LogicalDeviceLevelerScreenType4> ValidScreens { get; } = new List<LogicalDeviceLevelerScreenType4> { LogicalDeviceLevelerScreenType4.PromptAirbagTimeSelect };


		protected override bool IsScreenSupported(LogicalDeviceLevelerScreenType4 screenSelected)
		{
			return ValidScreens.Contains(screenSelected);
		}

		public LogicalDeviceLevelerCommandButtonPressedPromptAirbagTimeSelectType4(LogicalDeviceLevelerButtonAirbagTimeSelectType4 buttonsPressed, int commandResponseTimeMs = 200)
			: base(LogicalDeviceLevelerScreenType4.PromptAirbagTimeSelect, buttonsPressed, commandResponseTimeMs)
		{
		}
	}
}
