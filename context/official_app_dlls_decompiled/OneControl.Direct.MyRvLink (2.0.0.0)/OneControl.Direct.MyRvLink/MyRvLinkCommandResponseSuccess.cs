using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandResponseSuccess : MyRvLinkCommandEvent, IMyRvLinkCommandResponseSuccess, IMyRvLinkCommandResponse, IMyRvLinkCommandEvent, IMyRvLinkEvent
	{
		protected const int CommandExtraDataStartIndex = 4;

		private byte[]? _encodedData;

		public CommandResult CommandResult => CommandResult.Completed;

		protected override int MinPayloadLength { get; } = 4;


		protected static MyRvLinkCommandResponseType MakeSuccessCommandResponseType(bool commandCompleted)
		{
			if (!commandCompleted)
			{
				return MyRvLinkCommandResponseType.SuccessMultipleResponse;
			}
			return MyRvLinkCommandResponseType.SuccessCompleted;
		}

		public MyRvLinkCommandResponseSuccess(ushort clientCommandId, bool commandCompleted, IReadOnlyList<byte>? extendedData = null)
			: base(clientCommandId, MakeSuccessCommandResponseType(commandCompleted), 4, extendedData)
		{
		}

		public MyRvLinkCommandResponseSuccess(IReadOnlyList<byte> rawData)
			: base(rawData, MyRvLinkCommandEvent.DecodeCommandResponseType(rawData), 4)
		{
		}

		public IReadOnlyList<byte> Encode()
		{
			if (_encodedData != null)
			{
				return _encodedData;
			}
			int count = base.ExtendedData.Count;
			int num = MinPayloadLength + count;
			_encodedData = new byte[num];
			EncodeBaseEventIntoBuffer(_encodedData);
			return _encodedData;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			if (base.ExtendedData.Count == 0)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
				defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
				defaultInterpolatedStringHandler.AppendLiteral(") ");
				defaultInterpolatedStringHandler.AppendFormatted(base.CommandResponseType);
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") ");
			defaultInterpolatedStringHandler.AppendFormatted(base.CommandResponseType);
			defaultInterpolatedStringHandler.AppendLiteral(": ");
			defaultInterpolatedStringHandler.AppendFormatted(base.ExtendedData.DebugDump(0, base.ExtendedData.Count));
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
