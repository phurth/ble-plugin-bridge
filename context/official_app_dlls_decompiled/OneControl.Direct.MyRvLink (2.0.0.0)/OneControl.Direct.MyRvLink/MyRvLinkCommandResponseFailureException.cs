namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandResponseFailureException : MyRvLinkException
	{
		public IMyRvLinkCommandResponseFailure Failure { get; }

		public MyRvLinkCommandResponseFailureException(IMyRvLinkCommandResponseFailure failure)
			: base(failure.ToString())
		{
			Failure = failure;
		}
	}
}
