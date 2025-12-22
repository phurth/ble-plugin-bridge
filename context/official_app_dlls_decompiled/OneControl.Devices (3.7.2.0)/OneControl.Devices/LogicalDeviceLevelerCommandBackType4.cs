namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandBackType4 : LogicalDeviceLevelerCommandType4
	{
		private const string LogTag = "LogicalDeviceLevelerCommandBackType4";

		public LogicalDeviceLevelerScreenType4 ScreenSelected => base.ScreenSelectedImpl;

		public LogicalDeviceLevelerCommandBackType4(LogicalDeviceLevelerScreenType4 screenSelected, int commandResponseTimeMs = 200)
			: base(LevelerCommandCode.Back, screenSelected, commandResponseTimeMs)
		{
		}

		public override string ToString()
		{
			return $"{base.ToString()} screen: {ScreenSelected}";
		}
	}
}
