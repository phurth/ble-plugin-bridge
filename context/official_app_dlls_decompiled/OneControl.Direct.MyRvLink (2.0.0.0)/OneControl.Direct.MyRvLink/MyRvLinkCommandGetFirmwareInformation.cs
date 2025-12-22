using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetFirmwareInformation : MyRvLinkCommand
	{
		private const int MaxPayloadLength = 4;

		private const int FirmwareInformationCodeIndex = 3;

		private readonly byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandGetFirmwareInformation";


		protected override int MinPayloadLength => 4;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.GetFirmwareInformation;


		public FirmwareInformationCode FirmwareInformationCode => (FirmwareInformationCode)_rawData[3];

		public bool IsCommandCompleted
		{
			get
			{
				if (base.ResponseState != 0)
				{
					return CompletedCommandResponse != null;
				}
				return false;
			}
		}

		public IMyRvLinkCommandResponse? CompletedCommandResponse { get; private set; }

		public MyRvLinkCommandGetFirmwareInformation(ushort clientCommandId, FirmwareInformationCode firmwareInformationCode)
		{
			_rawData = new byte[4];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = (byte)firmwareInformationCode;
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandGetFirmwareInformation(IReadOnlyList<byte> rawData)
		{
			ValidateCommand(rawData);
			if (rawData.Count > 4)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(CommandType);
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(4);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkCommandGetFirmwareInformation Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandGetFirmwareInformation(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override bool ProcessResponse(IMyRvLinkCommandResponse commandResponse)
		{
			if (base.ResponseState != 0)
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(55, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Ignoring Process Command Response because command is ");
				defaultInterpolatedStringHandler.AppendFormatted(base.ResponseState);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(commandResponse);
				TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				return true;
			}
			if (!base.ProcessResponse(commandResponse))
			{
				return false;
			}
			CompletedCommandResponse = commandResponse;
			return true;
		}

		public override IMyRvLinkCommandEvent DecodeCommandEvent(IMyRvLinkCommandEvent commandEvent)
		{
			if (!(commandEvent is MyRvLinkCommandResponseSuccess response))
			{
				if (commandEvent is MyRvLinkCommandResponseFailure commandEvent2)
				{
					return base.DecodeCommandEvent(commandEvent2);
				}
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
				defaultInterpolatedStringHandler.AppendLiteral("DecodeCommandEvent unexpected event type for ");
				defaultInterpolatedStringHandler.AppendFormatted(commandEvent);
				TaggedLog.Warning(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				return commandEvent;
			}
			MyRvLinkCommandGetFirmwareInformationResponseSuccess myRvLinkCommandGetFirmwareInformationResponseSuccess = new MyRvLinkCommandGetFirmwareInformationResponseSuccess(response);
			switch (myRvLinkCommandGetFirmwareInformationResponseSuccess.FirmwareInformationCode)
			{
			case FirmwareInformationCode.Version:
				return new MyRvLinkCommandGetFirmwareInformationResponseSuccessVersion(myRvLinkCommandGetFirmwareInformationResponseSuccess);
			case FirmwareInformationCode.Cpu:
				return new MyRvLinkCommandGetFirmwareInformationResponseSuccessCpu(myRvLinkCommandGetFirmwareInformationResponseSuccess);
			default:
			{
				string logTag2 = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unknown FirmwareInformationCode ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandGetFirmwareInformationResponseSuccess.FirmwareInformationCode);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandGetFirmwareInformationResponseSuccess);
				TaggedLog.Warning(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
				return myRvLinkCommandGetFirmwareInformationResponseSuccess;
			}
			}
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 4);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted(FirmwareInformationCode);
			defaultInterpolatedStringHandler.AppendLiteral(" Response: ");
			defaultInterpolatedStringHandler.AppendFormatted(CompletedCommandResponse?.ToString() ?? "Pending");
			defaultInterpolatedStringHandler.AppendLiteral("]");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
