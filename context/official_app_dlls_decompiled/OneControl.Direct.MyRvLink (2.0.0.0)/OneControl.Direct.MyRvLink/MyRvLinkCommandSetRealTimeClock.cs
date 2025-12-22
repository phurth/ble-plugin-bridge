using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandSetRealTimeClock : MyRvLinkCommand
	{
		private const int PayloadLength = 10;

		private const int MonthIndex = 3;

		private const int DayIndex = 4;

		private const int YearIndex = 5;

		private const int HourIndex = 7;

		private const int MinutesIndex = 8;

		private const int SecondsIndex = 9;

		private readonly byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandSetRealTimeClock";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.SetRealTimeClock;


		protected override int MinPayloadLength => 10;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte Month => _rawData[3];

		public byte Day => _rawData[4];

		public ushort Year => _rawData.GetValueUInt16(5);

		public byte Hour => _rawData[7];

		public byte Minutes => _rawData[8];

		public byte Seconds => _rawData[9];

		public DateTime DateTime => new DateTime(Year, Month, Day, Hour, Minutes, Seconds);

		public MyRvLinkCommandSetRealTimeClock(ushort clientCommandId, DateTime dateTime)
		{
			_rawData = new byte[10];
			_rawData[2] = (byte)CommandType;
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[3] = (byte)dateTime.Month;
			_rawData[4] = (byte)dateTime.Day;
			_rawData.SetValueUInt16((ushort)dateTime.Year, 5);
			_rawData[7] = (byte)dateTime.Hour;
			_rawData[8] = (byte)dateTime.Minute;
			_rawData[9] = (byte)dateTime.Second;
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandSetRealTimeClock(IReadOnlyList<byte> rawData)
		{
			ValidateCommand(rawData);
			if (rawData.Count > 10)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(CommandType);
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(10);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkCommandSetRealTimeClock Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandSetRealTimeClock(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 4);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(", Datetime: ");
			defaultInterpolatedStringHandler.AppendFormatted(DateTime);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
