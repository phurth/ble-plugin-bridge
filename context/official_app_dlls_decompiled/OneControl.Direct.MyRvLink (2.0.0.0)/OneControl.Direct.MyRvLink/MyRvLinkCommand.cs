using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public abstract class MyRvLinkCommand : IMyRvLinkCommand
	{
		private const string LogTag = "MyRvLinkCommand";

		protected const int CommandTypeIndex = 2;

		protected const int ClientCommandIdStartIndex = 0;

		protected abstract int MinPayloadLength { get; }

		public abstract MyRvLinkCommandType CommandType { get; }

		public abstract ushort ClientCommandId { get; }

		public MyRvLinkResponseState ResponseState { get; protected set; }

		protected virtual void ValidateCommand(IReadOnlyList<byte> rawData, ushort? clientCommandId = null)
		{
			if (rawData == null)
			{
				throw new ArgumentNullException("rawData", "No data was given to validate!");
			}
			if (rawData.Count < MinPayloadLength)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(53, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkCommandGetDevices));
				defaultInterpolatedStringHandler.AppendLiteral(" because less then ");
				defaultInterpolatedStringHandler.AppendFormatted(MinPayloadLength);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (CommandType != DecodeCommandType(rawData))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(56, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkCommandGetDevices));
				defaultInterpolatedStringHandler.AppendLiteral(" command type doesn't match ");
				defaultInterpolatedStringHandler.AppendFormatted(CommandType);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (clientCommandId.HasValue && clientCommandId.Value != DecodeClientCommandId(rawData))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(61, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkCommandGetDevices));
				defaultInterpolatedStringHandler.AppendLiteral(" client command id doesn't match ");
				defaultInterpolatedStringHandler.AppendFormatted(CommandType);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		public virtual bool ProcessResponse(IMyRvLinkCommandResponse commandResponse)
		{
			if (ResponseState != 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(55, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Ignoring Process Command Response because command is ");
				defaultInterpolatedStringHandler.AppendFormatted(ResponseState);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(commandResponse);
				TaggedLog.Debug("MyRvLinkCommand", defaultInterpolatedStringHandler.ToStringAndClear());
				return true;
			}
			if (!(commandResponse is MyRvLinkCommandResponseSuccess myRvLinkCommandResponseSuccess))
			{
				if (commandResponse is MyRvLinkCommandResponseFailure myRvLinkCommandResponseFailure)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Command failed ");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseFailure);
					defaultInterpolatedStringHandler.AppendLiteral(" Completed: ");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseFailure.IsCommandCompleted);
					defaultInterpolatedStringHandler.AppendLiteral(" Type: ");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseFailure.GetType());
					TaggedLog.Debug("MyRvLinkCommand", defaultInterpolatedStringHandler.ToStringAndClear());
					if (myRvLinkCommandResponseFailure.IsCommandCompleted)
					{
						ResponseState = MyRvLinkResponseState.Failed;
					}
				}
				else
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Unexpected response received ");
					defaultInterpolatedStringHandler.AppendFormatted(commandResponse);
					TaggedLog.Debug("MyRvLinkCommand", defaultInterpolatedStringHandler.ToStringAndClear());
					ResponseState = MyRvLinkResponseState.Failed;
				}
			}
			else
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Command Success Completed=");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseSuccess.IsCommandCompleted);
				TaggedLog.Debug("MyRvLinkCommand", defaultInterpolatedStringHandler.ToStringAndClear());
				if (myRvLinkCommandResponseSuccess.IsCommandCompleted)
				{
					ResponseState = MyRvLinkResponseState.Completed;
				}
			}
			return ResponseState != MyRvLinkResponseState.Pending;
		}

		protected static ushort DecodeClientCommandId(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer.GetValueUInt16(0);
		}

		public static MyRvLinkCommandType DecodeCommandType(IReadOnlyList<byte> decodeBuffer)
		{
			return (MyRvLinkCommandType)decodeBuffer[2];
		}

		public abstract IReadOnlyList<byte> Encode();

		public virtual IMyRvLinkCommandEvent DecodeCommandEvent(IMyRvLinkCommandEvent commandEvent)
		{
			return commandEvent;
		}
	}
}
