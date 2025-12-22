using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandActionHvac : MyRvLinkCommand
	{
		private const int ClimateZoneCommandLength = 3;

		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int DeviceCommandStartIndex = 5;

		private readonly byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandActionHvac";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.ActionHvac;


		protected override int MinPayloadLength => 8;

		private int MaxPayloadLength => MinPayloadLength;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public LogicalDeviceClimateZoneCommand Command => new LogicalDeviceClimateZoneCommand(new ArraySegment<byte>(_rawData, 5, 3));

		public MyRvLinkCommandActionHvac(ushort clientCommandId, byte deviceTableId, byte deviceId, LogicalDeviceClimateZoneCommand command)
		{
			IReadOnlyList<byte> dataMinimum = command.DataMinimum;
			if (dataMinimum.Count != 3)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(LogTag);
				defaultInterpolatedStringHandler.AppendLiteral(" because size is ");
				defaultInterpolatedStringHandler.AppendFormatted(dataMinimum.Count);
				defaultInterpolatedStringHandler.AppendLiteral(" when expecting ");
				defaultInterpolatedStringHandler.AppendFormatted(3);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(dataMinimum.DebugDump(0, dataMinimum.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = new byte[MaxPayloadLength];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			int num = 5;
			foreach (byte item in dataMinimum)
			{
				_rawData[num++] = item;
			}
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandActionHvac(IReadOnlyList<byte> rawData)
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

		public static MyRvLinkCommandActionHvac Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandActionHvac(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(66, 6);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Device Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Command: ");
			defaultInterpolatedStringHandler.AppendFormatted(Command);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
