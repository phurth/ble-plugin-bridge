using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Core.Types;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetDevicePid : MyRvLinkCommand
	{
		private const int MaxPayloadLength = 7;

		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int PidIdIndex = 5;

		private readonly byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandGetDevicePid";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.GetDevicePid;


		protected override int MinPayloadLength => 7;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public Pid Pid => (Pid)_rawData.GetValueUInt16(5);

		public UInt48? PidValue { get; private set; }

		public MyRvLinkCommandGetDevicePid(ushort clientCommandId, byte deviceTableId, byte deviceId, Pid pidId)
		{
			_rawData = new byte[7];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			_rawData.SetValueUInt16((ushort)pidId, 5);
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandGetDevicePid(IReadOnlyList<byte> rawData)
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
			if (!(commandResponse is MyRvLinkCommandGetDevicePidResponseCompleted myRvLinkCommandGetDevicePidResponseCompleted))
			{
				if (commandResponse is MyRvLinkCommandResponseSuccess myRvLinkCommandResponseSuccess)
				{
					string logTag2 = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(65, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Unexpected success response received ");
					defaultInterpolatedStringHandler.AppendFormatted(commandResponse);
					defaultInterpolatedStringHandler.AppendLiteral(" (should have been of type ");
					defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkCommandGetDevicePidResponseCompleted));
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
					if (PidValue.HasValue)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Already processed response for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" with value ");
						defaultInterpolatedStringHandler.AppendFormatted(PidValue);
						throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					PidValue = myRvLinkCommandGetDevicePidResponseCompleted.PidValue;
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

		public static MyRvLinkCommandGetDevicePid Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandGetDevicePid(rawData);
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
					return new MyRvLinkCommandGetDevicePidResponseCompleted(myRvLinkCommandResponseSuccess);
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
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(65, 6);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(" Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Device Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(" Pid: ");
			defaultInterpolatedStringHandler.AppendFormatted(Pid);
			defaultInterpolatedStringHandler.AppendLiteral(" Value: ");
			object obj;
			if (PidValue.HasValue)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler2 = new DefaultInterpolatedStringHandler(2, 1);
				defaultInterpolatedStringHandler2.AppendLiteral("0x");
				defaultInterpolatedStringHandler2.AppendFormatted(PidValue, "X");
				obj = defaultInterpolatedStringHandler2.ToStringAndClear();
			}
			else
			{
				obj = "Not Loaded";
			}
			defaultInterpolatedStringHandler.AppendFormatted((string)obj);
			defaultInterpolatedStringHandler.AppendLiteral("]");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
