using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandRenameDevice : MyRvLinkCommand
	{
		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int ToFunctionNameIndex = 5;

		private const int ToFunctionNameSessionIndex = 7;

		private const int ToFunctionInstanceIndex = 9;

		private const int ToFunctionInstanceSessionIndex = 10;

		private readonly byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandRenameDevice";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.RenameDevice;


		protected override int MinPayloadLength => MaxPayloadLength;

		private int MaxPayloadLength => 12;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public FUNCTION_NAME FunctionName => _rawData.GetValueUInt16(5);

		public SESSION_ID FunctionSession => _rawData.GetValueUInt16(7);

		public int FunctionInstance => _rawData[9] & 0xF;

		public FUNCTION_NAME FunctionInstanceSession => _rawData.GetValueUInt16(10);

		public MyRvLinkCommandRenameDevice(ushort clientCommandId, byte deviceTableId, byte deviceId, FUNCTION_NAME functionName, int functionInstance, SESSION_ID sessionId)
		{
			_rawData = new byte[MaxPayloadLength];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			_rawData.SetValueUInt16(functionName, 5);
			_rawData.SetValueUInt16(sessionId, 7);
			_rawData[9] = (byte)((uint)functionInstance & 0xFu);
			_rawData.SetValueUInt16(sessionId, 10);
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandRenameDevice(IReadOnlyList<byte> rawData)
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

		public static MyRvLinkCommandRenameDevice Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandRenameDevice(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(70, 10);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command ");
			defaultInterpolatedStringHandler.AppendFormatted(CommandType);
			defaultInterpolatedStringHandler.AppendLiteral(" Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Device Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Command: ");
			defaultInterpolatedStringHandler.AppendFormatted(FunctionName);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(FunctionInstance);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(FunctionSession);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(FunctionInstanceSession);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
