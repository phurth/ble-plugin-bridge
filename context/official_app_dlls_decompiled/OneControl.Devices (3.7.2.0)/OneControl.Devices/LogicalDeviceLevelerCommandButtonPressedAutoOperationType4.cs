using System.Collections.Generic;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandButtonPressedAutoOperationType4 : LogicalDeviceLevelerCommandButtonPressedType4<LogicalDeviceLevelerButtonNoneType4>
	{
		protected List<LogicalDeviceLevelerScreenType4> ValidScreens { get; }

		protected override bool IsScreenSupported(LogicalDeviceLevelerScreenType4 screenSelected)
		{
			return ValidScreens.Contains(screenSelected);
		}

		public LogicalDeviceLevelerCommandButtonPressedAutoOperationType4(LogicalDeviceLevelerOperationAutoType4 operationAuto, int commandResponseTimeMs = 200)
			: base(operationAuto.ToScreen(), LogicalDeviceLevelerButtonNoneType4.None, commandResponseTimeMs)
		{
			ValidScreens = new List<LogicalDeviceLevelerScreenType4> { operationAuto.ToScreen() };
		}
	}
}
