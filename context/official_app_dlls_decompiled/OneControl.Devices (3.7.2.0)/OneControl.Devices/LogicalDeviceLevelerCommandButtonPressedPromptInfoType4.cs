using System.Collections.Generic;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandButtonPressedPromptInfoType4 : LogicalDeviceLevelerCommandButtonPressedType4<LogicalDeviceLevelerButtonOkType4>
	{
		protected List<LogicalDeviceLevelerScreenType4> ValidScreens { get; } = new List<LogicalDeviceLevelerScreenType4> { LogicalDeviceLevelerScreenType4.PromptInfo };


		protected override bool IsScreenSupported(LogicalDeviceLevelerScreenType4 screenSelected)
		{
			return ValidScreens.Contains(screenSelected);
		}

		public LogicalDeviceLevelerCommandButtonPressedPromptInfoType4(LogicalDeviceLevelerButtonOkType4 buttonsPressed, int commandResponseTimeMs = 200)
			: base(LogicalDeviceLevelerScreenType4.PromptInfo, buttonsPressed, commandResponseTimeMs)
		{
		}
	}
}
