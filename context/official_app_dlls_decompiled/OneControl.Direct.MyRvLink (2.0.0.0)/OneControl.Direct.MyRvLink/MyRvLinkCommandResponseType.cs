namespace OneControl.Direct.MyRvLink
{
	public enum MyRvLinkCommandResponseType : byte
	{
		SuccessMultipleResponse = 1,
		SuccessCompleted = 129,
		FailureMultipleResponse = 2,
		FailureCompleted = 130
	}
}
