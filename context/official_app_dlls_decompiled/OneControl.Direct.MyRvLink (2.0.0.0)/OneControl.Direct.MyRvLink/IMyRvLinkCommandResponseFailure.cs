namespace OneControl.Direct.MyRvLink
{
	public interface IMyRvLinkCommandResponseFailure : IMyRvLinkCommandResponse, IMyRvLinkCommandEvent, IMyRvLinkEvent
	{
		MyRvLinkCommandResponseFailureCode FailureCode { get; }
	}
}
