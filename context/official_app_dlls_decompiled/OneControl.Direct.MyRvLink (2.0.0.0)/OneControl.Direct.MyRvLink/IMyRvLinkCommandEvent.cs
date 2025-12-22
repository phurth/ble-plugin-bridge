namespace OneControl.Direct.MyRvLink
{
	public interface IMyRvLinkCommandEvent : IMyRvLinkEvent
	{
		ushort ClientCommandId { get; }

		bool IsCommandCompleted { get; }
	}
}
