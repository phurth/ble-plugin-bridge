using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public interface IMyRvLinkCommandResponse : IMyRvLinkCommandEvent, IMyRvLinkEvent
	{
		CommandResult CommandResult { get; }
	}
}
