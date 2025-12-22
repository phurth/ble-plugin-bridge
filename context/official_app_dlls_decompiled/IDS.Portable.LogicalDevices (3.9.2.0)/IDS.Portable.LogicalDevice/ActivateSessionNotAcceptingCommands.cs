using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class ActivateSessionNotAcceptingCommands : LogicalDeviceSessionException
	{
		public ActivateSessionNotAcceptingCommands(string tag, ILogicalDevice logicalDevice)
			: base(tag, $"Unable to activate session because device not accepting commands {logicalDevice}", verbose: false)
		{
			TaggedLog.Debug(tag, "ActivateSessionNotAcceptingCommands: " + Message);
		}
	}
}
