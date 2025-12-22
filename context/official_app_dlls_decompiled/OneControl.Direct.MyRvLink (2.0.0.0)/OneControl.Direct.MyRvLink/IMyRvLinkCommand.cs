using System.Collections.Generic;

namespace OneControl.Direct.MyRvLink
{
	public interface IMyRvLinkCommand
	{
		MyRvLinkCommandType CommandType { get; }

		ushort ClientCommandId { get; }

		MyRvLinkResponseState ResponseState { get; }

		IReadOnlyList<byte> Encode();

		IMyRvLinkCommandEvent DecodeCommandEvent(IMyRvLinkCommandEvent commandEvent);

		bool ProcessResponse(IMyRvLinkCommandResponse commandResponse);
	}
}
