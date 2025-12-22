using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Color;
using IDS.Portable.Common.Extensions;
using OneControl.Devices.LightRgb;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandActionRgb : MyRvLinkCommand
	{
		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int DeviceCommandIndex = 5;

		private readonly byte[] _rawData;

		private const int DeviceRedColorByteIndex = 6;

		private const int DeviceGreenColorByteIndex = 7;

		private const int DeviceBlueColorByteIndex = 8;

		private const int DeviceAutoOffModeOnByteIndex = 6;

		private const int DeviceAutoOffModeBlinkByteIndex = 7;

		private const int DeviceAutoOffModeRainbowByteIndex = 8;

		private const int DeviceInterval1ModeBlinkByteIndex = 10;

		private const int DeviceInterval2ModeBlinkByteIndex = 11;

		private const int DeviceInterval1ModeRainbowByteIndex = 7;

		private const int DeviceInterval2ModeRainbowByteIndex = 8;

		protected virtual string LogTag { get; } = "MyRvLinkCommandActionRgb";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.ActionRgb;


		protected override int MinPayloadLength => 6;

		private int MaxPayloadLength => 12;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public RgbLightMode Mode => (RgbLightMode)_rawData[5];

		public RgbColor? Color => Mode switch
		{
			RgbLightMode.Off => new RgbColor(_rawData[6], _rawData[7], _rawData[8]), 
			RgbLightMode.On => new RgbColor(_rawData[6], _rawData[7], _rawData[8]), 
			RgbLightMode.Blink => new RgbColor(_rawData[6], _rawData[7], _rawData[8]), 
			_ => null, 
		};

		public byte? AutoOffDuration => Mode switch
		{
			RgbLightMode.On => _rawData[6], 
			RgbLightMode.Blink => _rawData[7], 
			RgbLightMode.Rainbow => _rawData[8], 
			_ => null, 
		};

		public ushort? Interval
		{
			get
			{
				if (Mode == RgbLightMode.Rainbow)
				{
					return (ushort)((_rawData[7] << 8) | _rawData[8]);
				}
				return null;
			}
		}

		public byte? OnInterval
		{
			get
			{
				if (Mode == RgbLightMode.Blink)
				{
					return _rawData[10];
				}
				return null;
			}
		}

		public byte? OffInterval
		{
			get
			{
				if (Mode == RgbLightMode.Blink)
				{
					return _rawData[11];
				}
				return null;
			}
		}

		public string CommandLogString
		{
			get
			{
				switch (Mode)
				{
				case RgbLightMode.Off:
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
					defaultInterpolatedStringHandler.AppendFormatted(Mode);
					return defaultInterpolatedStringHandler.ToStringAndClear();
				}
				case RgbLightMode.On:
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 3);
					defaultInterpolatedStringHandler.AppendFormatted(Mode);
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					defaultInterpolatedStringHandler.AppendFormatted(Color);
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					defaultInterpolatedStringHandler.AppendFormatted(AutoOffDuration);
					return defaultInterpolatedStringHandler.ToStringAndClear();
				}
				case RgbLightMode.Blink:
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 5);
					defaultInterpolatedStringHandler.AppendFormatted(Mode);
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					defaultInterpolatedStringHandler.AppendFormatted(Color);
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					defaultInterpolatedStringHandler.AppendFormatted(AutoOffDuration);
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					defaultInterpolatedStringHandler.AppendFormatted(OnInterval);
					defaultInterpolatedStringHandler.AppendLiteral("/");
					defaultInterpolatedStringHandler.AppendFormatted(OffInterval);
					return defaultInterpolatedStringHandler.ToStringAndClear();
				}
				case RgbLightMode.Rainbow:
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 3);
					defaultInterpolatedStringHandler.AppendFormatted(Mode);
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					defaultInterpolatedStringHandler.AppendFormatted(AutoOffDuration);
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					defaultInterpolatedStringHandler.AppendFormatted(Interval);
					return defaultInterpolatedStringHandler.ToStringAndClear();
				}
				default:
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(15, 1);
					defaultInterpolatedStringHandler.AppendFormatted(Mode);
					defaultInterpolatedStringHandler.AppendLiteral(" (Unknown mode)");
					return defaultInterpolatedStringHandler.ToStringAndClear();
				}
				}
			}
		}

		public MyRvLinkCommandActionRgb(ushort clientCommandId, byte deviceTableId, byte deviceId, LogicalDeviceLightRgbCommand command)
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

		protected MyRvLinkCommandActionRgb(IReadOnlyList<byte> rawData)
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

		public static MyRvLinkCommandActionRgb Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandActionRgb(rawData);
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
			defaultInterpolatedStringHandler.AppendFormatted(CommandLogString);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
