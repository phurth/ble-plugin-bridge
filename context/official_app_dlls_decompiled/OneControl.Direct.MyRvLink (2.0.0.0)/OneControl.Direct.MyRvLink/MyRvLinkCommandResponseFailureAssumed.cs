using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandResponseFailureAssumed : IMyRvLinkCommandResponseFailure, IMyRvLinkCommandResponse, IMyRvLinkCommandEvent, IMyRvLinkEvent
	{
		public MyRvLinkEventType EventType => MyRvLinkEventType.DeviceCommand;

		public CommandResult CommandResult => CommandResult.ErrorAssumed;

		public ushort ClientCommandId { get; }

		public bool IsCommandCompleted => false;

		public MyRvLinkCommandResponseFailureCode FailureCode { get; }

		public MyRvLinkCommandResponseFailureAssumed(ushort clientCommandId, MyRvLinkCommandResponseFailureCode originalFailureCode)
		{
			ClientCommandId = clientCommandId;
			FailureCode = originalFailureCode;
		}
	}
}
