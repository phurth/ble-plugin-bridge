using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandResponseSuccessNoWait : IMyRvLinkCommandResponseSuccess, IMyRvLinkCommandResponse, IMyRvLinkCommandEvent, IMyRvLinkEvent
	{
		public MyRvLinkEventType EventType => MyRvLinkEventType.DeviceCommand;

		public CommandResult CommandResult => CommandResult.Completed;

		public ushort ClientCommandId { get; }

		public bool IsCommandCompleted => false;

		public MyRvLinkCommandResponseSuccessNoWait(ushort clientCommandId)
		{
			ClientCommandId = clientCommandId;
		}
	}
}
