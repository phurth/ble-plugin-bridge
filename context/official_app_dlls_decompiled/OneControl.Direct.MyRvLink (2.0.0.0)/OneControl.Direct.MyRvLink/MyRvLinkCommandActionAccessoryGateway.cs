using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using OneControl.Devices.AccessoryGateway;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandActionAccessoryGateway : MyRvLinkCommand
	{
		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int DeviceCommandIndex = 5;

		private readonly byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandActionAccessoryGateway";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.ActionAccessoryGateway;


		protected override int MinPayloadLength => 6;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public LogicalDeviceAccessoryGatewayCommand Command
		{
			get
			{
				int num = 6;
				return LogicalDeviceAccessoryGatewayCommand.MakeCommand(_rawData[5], new ArraySegment<byte>(_rawData, num, _rawData.Length - num));
			}
		}

		public MyRvLinkCommandActionAccessoryGateway(ushort clientCommandId, byte deviceTableId, byte deviceId, LogicalDeviceAccessoryGatewayCommand command)
		{
			IReadOnlyList<byte> rawData = command.RawData;
			int num = MinPayloadLength + rawData.Count + 1 - 1;
			_rawData = new byte[num];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			_rawData[5] = command.CommandByte;
			for (int i = 0; i < rawData.Count; i++)
			{
				_rawData[5 + i + 1] = rawData[i];
			}
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandActionAccessoryGateway(IReadOnlyList<byte> rawData)
		{
			ValidateCommand(rawData);
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkCommandActionAccessoryGateway Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandActionAccessoryGateway(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override string ToString()
		{
			try
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
			catch
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 3);
				defaultInterpolatedStringHandler.AppendFormatted(LogTag);
				defaultInterpolatedStringHandler.AppendLiteral("[Client Command: ");
				defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandActionAccessoryGateway");
				defaultInterpolatedStringHandler.AppendLiteral(", UNABLE TO DECODE]: ");
				defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}
	}
}
