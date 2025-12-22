namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandHomeType4 : LogicalDeviceLevelerCommandType4
	{
		private const string LogTag = "LogicalDeviceLevelerCommandHomeType4";

		public LogicalDeviceLevelerCommandHomeType4(int commandResponseTimeMs = 200)
			: base(LevelerCommandCode.Home, commandResponseTimeMs)
		{
		}
	}
}
