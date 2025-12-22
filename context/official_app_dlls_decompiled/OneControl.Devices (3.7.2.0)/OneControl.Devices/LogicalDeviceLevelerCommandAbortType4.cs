namespace OneControl.Devices
{
	public class LogicalDeviceLevelerCommandAbortType4 : LogicalDeviceLevelerCommandType4
	{
		private const string LogTag = "LogicalDeviceLevelerCommandAbortType4";

		public LogicalDeviceLevelerCommandAbortType4(int commandResponseTimeMs = 200)
			: base(LevelerCommandCode.Abort, commandResponseTimeMs)
		{
		}
	}
}
