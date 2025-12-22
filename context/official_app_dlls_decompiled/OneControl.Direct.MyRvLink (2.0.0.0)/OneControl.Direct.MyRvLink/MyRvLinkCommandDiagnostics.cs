using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandDiagnostics : MyRvLinkCommand
	{
		private const int MaxPayloadLength = 9;

		private const int DiagnosticCommandTypeIndex = 3;

		private const int DiagnosticCommandStateIndex = 4;

		private const int DiagnosticEventTypeIndex = 5;

		private const int DiagnosticEventStateIndex = 6;

		private const int DiagnosticHostValue = 7;

		private const int DiagnosticDeviceLinkId = 8;

		private readonly byte[] _rawData;

		private MyRvLinkCommandDiagnosticsResponseCompleted? _response;

		protected virtual string LogTag { get; } = "MyRvLinkCommandDiagnostics";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.Diagnostics;


		protected override int MinPayloadLength => 9;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public IReadOnlyList<MyRvLinkCommandType>? EnabledDiagnosticCommands => _response?.EnabledDiagnosticCommands;

		public IReadOnlyList<MyRvLinkEventType>? EnabledDiagnosticEvents => _response?.EnabledDiagnosticEvents;

		public MyRvLinkCommandType DiagnosticCommandType => (MyRvLinkCommandType)_rawData[3];

		public DiagnosticState DiagnosticCommandState => (DiagnosticState)_rawData[4];

		public MyRvLinkEventType DiagnosticEventType => (MyRvLinkEventType)_rawData[5];

		public DiagnosticState DiagnosticEventState => (DiagnosticState)_rawData[6];

		public MyRvLinkCommandDiagnostics(ushort clientCommandId, MyRvLinkCommandType diagCommandType, DiagnosticState diagCommandState, MyRvLinkEventType diagEventType, DiagnosticState diagEventState)
		{
			_rawData = new byte[9];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = (byte)diagCommandType;
			_rawData[4] = (byte)diagCommandState;
			_rawData[5] = (byte)diagEventType;
			_rawData[6] = (byte)diagEventState;
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandDiagnostics(IReadOnlyList<byte> rawData)
		{
			ValidateCommand(rawData);
			_rawData = rawData.ToNewArray(0, rawData.Count);
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
			if (!(commandResponse is MyRvLinkCommandDiagnosticsResponseCompleted response))
			{
				if (commandResponse is MyRvLinkCommandResponseSuccess myRvLinkCommandResponseSuccess)
				{
					string logTag2 = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(65, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Unexpected success response received ");
					defaultInterpolatedStringHandler.AppendFormatted(commandResponse);
					defaultInterpolatedStringHandler.AppendLiteral(" (should have been of type ");
					defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkCommandDiagnosticsResponseCompleted));
					defaultInterpolatedStringHandler.AppendLiteral(")");
					TaggedLog.Debug(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
					return myRvLinkCommandResponseSuccess.IsCommandCompleted;
				}
				if (commandResponse is MyRvLinkCommandResponseFailure myRvLinkCommandResponseFailure)
				{
					string logTag3 = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Command failed ");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseFailure);
					TaggedLog.Debug(logTag3, defaultInterpolatedStringHandler.ToStringAndClear());
					base.ResponseState = MyRvLinkResponseState.Failed;
				}
				else
				{
					string logTag4 = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Unexpected response received ");
					defaultInterpolatedStringHandler.AppendFormatted(commandResponse);
					TaggedLog.Debug(logTag4, defaultInterpolatedStringHandler.ToStringAndClear());
					base.ResponseState = MyRvLinkResponseState.Failed;
				}
			}
			else
			{
				try
				{
					if (_response != null)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Already processed response for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" with value ");
						defaultInterpolatedStringHandler.AppendFormatted(_response);
						throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					_response = response;
					base.ResponseState = MyRvLinkResponseState.Completed;
				}
				catch (Exception ex)
				{
					TaggedLog.Warning(LogTag, "Warning processing response: " + ex.Message);
					base.ResponseState = MyRvLinkResponseState.Failed;
				}
			}
			return base.ResponseState != MyRvLinkResponseState.Pending;
		}

		public static MyRvLinkCommandDiagnostics Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandDiagnostics(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override IMyRvLinkCommandEvent DecodeCommandEvent(IMyRvLinkCommandEvent commandEvent)
		{
			if (commandEvent is MyRvLinkCommandResponseSuccess myRvLinkCommandResponseSuccess)
			{
				if (myRvLinkCommandResponseSuccess.IsCommandCompleted)
				{
					return new MyRvLinkCommandDiagnosticsResponseCompleted(myRvLinkCommandResponseSuccess);
				}
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(53, 2);
				defaultInterpolatedStringHandler.AppendLiteral("DecodeCommandEvent for ");
				defaultInterpolatedStringHandler.AppendFormatted(LogTag);
				defaultInterpolatedStringHandler.AppendLiteral(" Received UNEXPECTED event of ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseSuccess);
				TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				return commandEvent;
			}
			return commandEvent;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(22, 2);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			object value;
			if (DiagnosticCommandType != 0)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 2);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted(DiagnosticCommandType);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted(DiagnosticCommandState);
				value = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			else
			{
				value = " No Command Diagnostics";
			}
			stringBuilder.Append((string)value);
			stringBuilder.Append(",");
			object value2;
			if (DiagnosticEventType != 0)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 2);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted(DiagnosticEventType);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted(DiagnosticEventState);
				value2 = defaultInterpolatedStringHandler.ToStringAndClear();
			}
			else
			{
				value2 = " No Event Diagnostics";
			}
			stringBuilder.Append((string)value2);
			stringBuilder.Append(",");
			stringBuilder.Append((_response == null) ? " No Response]" : ("] " + _response!.ToString()));
			return stringBuilder.ToString();
		}
	}
}
