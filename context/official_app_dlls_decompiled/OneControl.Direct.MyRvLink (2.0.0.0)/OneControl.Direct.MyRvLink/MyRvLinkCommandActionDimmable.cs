using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandActionDimmable : MyRvLinkCommand
	{
		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int DeviceCommandIndex = 5;

		private readonly byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandActionDimmable";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.ActionDimmable;


		protected override int MinPayloadLength => 6;

		private int MaxPayloadLength => 12;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public LogicalDeviceLightDimmableCommand Command => new LogicalDeviceLightDimmableCommand(new ArraySegment<byte>(_rawData, 5, _rawData.Length - 5));

		public MyRvLinkCommandActionDimmable(ushort clientCommandId, byte deviceTableId, byte deviceId, LogicalDeviceLightDimmableCommand command)
		{
			IReadOnlyList<byte> dataMinimum = command.DataMinimum;
			int num = MinPayloadLength + dataMinimum.Count - 1;
			_rawData = new byte[num];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			int num2 = 5;
			foreach (byte item in dataMinimum)
			{
				_rawData[num2++] = item;
			}
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandActionDimmable(IReadOnlyList<byte> rawData)
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

		public static MyRvLinkCommandActionDimmable Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandActionDimmable(rawData);
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
