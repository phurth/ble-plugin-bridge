using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandLeveler1ButtonCommand : MyRvLinkCommand
	{
		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int ButtonState1Index = 5;

		private const int ButtonState2Index = 6;

		private readonly byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandLeveler1ButtonCommand";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.Leveler1ButtonCommand;


		protected override int MinPayloadLength => 7;

		private int MaxPayloadLength => MinPayloadLength;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public byte ButtonStateData1 => _rawData[5];

		public byte ButtonStateData2 => _rawData[6];

		public int DeviceCount => 1;

		public MyRvLinkCommandLeveler1ButtonCommand(ushort clientCommandId, byte deviceTableId, byte deviceId, LogicalDeviceLevelerCommandType1 command)
		{
			_rawData = new byte[MaxPayloadLength];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			byte[] array = command.CopyCurrentData();
			int num = 5;
			byte[] array2 = array;
			foreach (byte b in array2)
			{
				_rawData[num++] = b;
			}
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandLeveler1ButtonCommand(IReadOnlyList<byte> rawData)
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

		public static MyRvLinkCommandLeveler1ButtonCommand Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandLeveler1ButtonCommand(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(88, 7);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", DeviceId: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendLiteral("Button State Data: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ButtonStateData1, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(" 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ButtonStateData2, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(" Raw Data: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
