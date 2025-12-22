using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandLeveler4ButtonCommand : MyRvLinkCommand
	{
		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int DeviceModeIndex = 5;

		private const int UiModeIndex = 6;

		private const int UiButtonData1Index = 7;

		private const int UiButtonData2Index = 8;

		private const int UiButtonData3Index = 9;

		public const int UIButtonDataSize = 3;

		private readonly byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandLeveler4ButtonCommand";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.Leveler4ButtonCommand;


		protected override int MinPayloadLength => 10;

		private int MaxPayloadLength => MinPayloadLength;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public byte DeviceMode => _rawData[5];

		public byte UiMode => _rawData[6];

		public byte UiButtonData1 => _rawData[7];

		public byte UiButtonData2 => _rawData[8];

		public byte UiButtonData3 => _rawData[9];

		public int DeviceCount => 1;

		public MyRvLinkCommandLeveler4ButtonCommand(ushort clientCommandId, byte deviceTableId, byte deviceId, ILogicalDeviceLevelerCommandType4 command)
		{
			_rawData = new byte[MaxPayloadLength];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			_rawData[5] = command.CommandByte;
			if (!(command is ILogicalDeviceLevelerCommandButtonPressedType4 logicalDeviceLevelerCommandButtonPressedType))
			{
				if (!(command is LogicalDeviceLevelerCommandAbortType4))
				{
					if (!(command is LogicalDeviceLevelerCommandBackType4 logicalDeviceLevelerCommandBackType))
					{
						if (!(command is LogicalDeviceLevelerCommandHomeType4))
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 1);
							defaultInterpolatedStringHandler.AppendLiteral("Unable to decode command ");
							defaultInterpolatedStringHandler.AppendFormatted(command);
							throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
						}
					}
					else
					{
						_rawData[6] = (byte)logicalDeviceLevelerCommandBackType.ScreenSelected;
					}
				}
			}
			else
			{
				_rawData[6] = (byte)logicalDeviceLevelerCommandButtonPressedType.ScreenSelected;
				IReadOnlyList<byte> rawButtonData = logicalDeviceLevelerCommandButtonPressedType.RawButtonData;
				if (rawButtonData.Count != 3)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(63, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to decode command ");
					defaultInterpolatedStringHandler.AppendFormatted(command);
					defaultInterpolatedStringHandler.AppendLiteral(", expected button data to be ");
					defaultInterpolatedStringHandler.AppendFormatted(3);
					defaultInterpolatedStringHandler.AppendLiteral(" but was ");
					defaultInterpolatedStringHandler.AppendFormatted(rawButtonData.Count);
					throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				for (int i = 0; i < rawButtonData.Count; i++)
				{
					_rawData[7 + i] = rawButtonData[i];
				}
			}
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandLeveler4ButtonCommand(IReadOnlyList<byte> rawData)
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

		public static MyRvLinkCommandLeveler4ButtonCommand Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandLeveler4ButtonCommand(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(118, 10);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", DeviceId: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Device Mode: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceMode, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendLiteral("UI Mode: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(UiMode, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", UI Button Data: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(UiButtonData1, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(" 0x");
			defaultInterpolatedStringHandler.AppendFormatted(UiButtonData2, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(" 0x");
			defaultInterpolatedStringHandler.AppendFormatted(UiButtonData3, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(" Raw Data: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
