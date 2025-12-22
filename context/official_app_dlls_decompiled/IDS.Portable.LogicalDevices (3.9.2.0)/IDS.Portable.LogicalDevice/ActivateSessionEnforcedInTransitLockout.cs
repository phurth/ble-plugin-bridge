using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class ActivateSessionEnforcedInTransitLockout : ActivateSessionNotAcceptingCommands
	{
		public ActivateSessionEnforcedInTransitLockout(string tag, ILogicalDevice logicalDevice)
			: base(tag, logicalDevice)
		{
			TaggedLog.Debug(tag, "ActivateSessionEnforcedInTransitLockout: " + Message);
		}
	}
}
