using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandResponseFailure : MyRvLinkCommandEvent, IMyRvLinkCommandResponseFailure, IMyRvLinkCommandResponse, IMyRvLinkCommandEvent, IMyRvLinkEvent
	{
		protected const int CommandFailureCodeIndex = 4;

		protected const int CommandExtraDataStartIndex = 5;

		private byte[]? _encodedData;

		public CommandResult CommandResult => ToCommandResult(FailureCode);

		public MyRvLinkCommandResponseFailureCode FailureCode { get; }

		protected override int MinPayloadLength { get; } = 5;


		private static CommandResult ToCommandResult(MyRvLinkCommandResponseFailureCode failureCode)
		{
			return failureCode switch
			{
				MyRvLinkCommandResponseFailureCode.Success => CommandResult.Completed, 
				MyRvLinkCommandResponseFailureCode.Offline => CommandResult.ErrorDeviceOffline, 
				MyRvLinkCommandResponseFailureCode.DeviceInUse => CommandResult.ErrorNoSession, 
				MyRvLinkCommandResponseFailureCode.CommandTimeout => CommandResult.ErrorCommandTimeout, 
				MyRvLinkCommandResponseFailureCode.CommandNotSupported => CommandResult.ErrorRemoteOperationNotSupported, 
				MyRvLinkCommandResponseFailureCode.CommandAborted => CommandResult.Canceled, 
				MyRvLinkCommandResponseFailureCode.DeviceHazardousToOperate => CommandResult.ErrorCommandNotAllowed, 
				MyRvLinkCommandResponseFailureCode.SessionTimeout => CommandResult.ErrorSessionTimeout, 
				MyRvLinkCommandResponseFailureCode.CantOpenSession => CommandResult.ErrorNoSession, 
				_ => CommandResult.ErrorOther, 
			};
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			if (base.ExtendedData.Count == 0)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
				defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
				defaultInterpolatedStringHandler.AppendLiteral(") ");
				defaultInterpolatedStringHandler.AppendFormatted(base.CommandResponseType);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted(FailureCode);
				defaultInterpolatedStringHandler.AppendLiteral("/");
				defaultInterpolatedStringHandler.AppendFormatted(CommandResult);
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 5);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") ");
			defaultInterpolatedStringHandler.AppendFormatted(base.CommandResponseType);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(FailureCode);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(CommandResult);
			defaultInterpolatedStringHandler.AppendLiteral(": ");
			defaultInterpolatedStringHandler.AppendFormatted(base.ExtendedData.DebugDump(0, base.ExtendedData.Count));
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		protected static MyRvLinkCommandResponseType MakeFailureCommandResponseType(bool commandCompleted)
		{
			if (!commandCompleted)
			{
				return MyRvLinkCommandResponseType.FailureMultipleResponse;
			}
			return MyRvLinkCommandResponseType.FailureCompleted;
		}

		public MyRvLinkCommandResponseFailure(ushort clientCommandId, bool commandCompleted, MyRvLinkCommandResponseFailureCode failureCode, IReadOnlyList<byte>? extendedData = null)
			: base(clientCommandId, MakeFailureCommandResponseType(commandCompleted), 5, extendedData)
		{
			FailureCode = failureCode;
		}

		public MyRvLinkCommandResponseFailure(ushort clientCommandId, MyRvLinkCommandResponseFailureCode failureCode, IReadOnlyList<byte>? extendedData = null)
			: this(clientCommandId, commandCompleted: true, failureCode, extendedData)
		{
		}

		public MyRvLinkCommandResponseFailure(IReadOnlyList<byte> rawData)
			: base(rawData, MyRvLinkCommandEvent.DecodeCommandResponseType(rawData), 5)
		{
			FailureCode = DecodeStandardFailureCode(rawData);
		}

		protected static MyRvLinkCommandResponseFailureCode DecodeStandardFailureCode(IReadOnlyList<byte> decodeBuffer)
		{
			return (MyRvLinkCommandResponseFailureCode)decodeBuffer[4];
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
			_encodedData[4] = (byte)FailureCode;
			return _encodedData;
		}
	}
}
