using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	internal class MyRvLinkCommandGetDeviceBlockList : MyRvLinkCommand
	{
		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private readonly byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandGetDeviceBlockList";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.GetDeviceBlockList;


		protected override int MinPayloadLength => 5;

		private int MaxPayloadLength => MinPayloadLength;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public MyRvLinkCommandGetDeviceBlockList(ushort clientCommandId, byte deviceTableId, byte deviceId)
		{
			_rawData = new byte[MaxPayloadLength];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandGetDeviceBlockList(IReadOnlyList<byte> rawData)
		{
			ValidateCommand(rawData);
			if (rawData.Count > MaxPayloadLength)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(CommandType);
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(MaxPayloadLength);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public override IMyRvLinkCommandEvent DecodeCommandEvent(IMyRvLinkCommandEvent commandEvent)
		{
			if (commandEvent is MyRvLinkCommandResponseSuccess myRvLinkCommandResponseSuccess)
			{
				if (myRvLinkCommandResponseSuccess.IsCommandCompleted)
				{
					return new MyRvLinkGetDeviceBlockListCommandResponse(myRvLinkCommandResponseSuccess);
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

		public static MyRvLinkCommandGetDeviceBlockList Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandGetDeviceBlockList(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(63, 5);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", DeviceId: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendLiteral("Raw Data: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
