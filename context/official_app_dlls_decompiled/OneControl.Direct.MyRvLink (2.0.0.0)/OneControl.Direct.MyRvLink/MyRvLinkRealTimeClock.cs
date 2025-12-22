using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkRealTimeClock : MyRvLinkEvent<MyRvLinkRealTimeClock>
	{
		[Flags]
		private enum Flags
		{
			None = 0,
			HasRtcHardware = 1,
			UsesRtcHardware = 2,
			Reserved = 0xFC
		}

		public enum TimeSinceStartUnits
		{
			Seconds = 0,
			Minutes = 1,
			Hours = 16,
			Days = 17
		}

		public readonly struct TimeSinceStart
		{
			internal const ushort UnitsShift = 14;

			internal const ushort UnitsMask = 49152;

			public const ushort MaxValue = 16383;

			public ushort Value { get; }

			public TimeSinceStartUnits Units { get; }

			private bool IsClockSet
			{
				get
				{
					if (Value != 16383)
					{
						return Units != TimeSinceStartUnits.Days;
					}
					return false;
				}
			}

			public TimeSpan TimeSpan => Units switch
			{
				TimeSinceStartUnits.Seconds => TimeSpan.FromSeconds((int)Value), 
				TimeSinceStartUnits.Minutes => TimeSpan.FromMinutes((int)Value), 
				TimeSinceStartUnits.Hours => TimeSpan.FromHours((int)Value), 
				TimeSinceStartUnits.Days => TimeSpan.FromDays((int)Value), 
				_ => TimeSpan.FromSeconds((int)Value), 
			};

			internal ushort ValueRaw => (ushort)(((ushort)Units << 14) | Value);

			public TimeSinceStart(TimeSpan timespan)
			{
				if (timespan == TimeSpan.MaxValue)
				{
					Value = 16383;
					Units = TimeSinceStartUnits.Days;
					return;
				}
				TimeSpan timeSpan = timespan;
				if (timeSpan.TotalSeconds < 16383.0)
				{
					Value = (ushort)timeSpan.TotalSeconds;
					Units = TimeSinceStartUnits.Seconds;
				}
				else
				{
					TimeSpan timeSpan2 = timeSpan;
					if (timeSpan2.TotalMinutes < 16383.0)
					{
						Value = (ushort)timeSpan2.TotalMinutes;
						Units = TimeSinceStartUnits.Minutes;
					}
					else
					{
						TimeSpan timeSpan3 = timeSpan;
						if (timeSpan3.TotalHours < 16383.0)
						{
							Value = (ushort)timeSpan3.TotalHours;
							Units = TimeSinceStartUnits.Hours;
						}
						else
						{
							TimeSpan timeSpan4 = timeSpan;
							if (!(timeSpan4.TotalDays < 16383.0))
							{
								throw new ArgumentOutOfRangeException("timespan", "timespan to big to fit in TimeSinceStart");
							}
							Value = (ushort)timeSpan4.TotalDays;
							Units = TimeSinceStartUnits.Days;
						}
					}
				}
				if (timespan.TotalSeconds < 16383.0)
				{
					Value = (ushort)timespan.TotalSeconds;
					Units = TimeSinceStartUnits.Seconds;
				}
			}

			internal TimeSinceStart(ushort rawTime)
			{
				Units = (TimeSinceStartUnits)((rawTime & 0xC000) >> 14);
				Value = (ushort)(rawTime & 0x3FFFu);
			}

			public override string ToString()
			{
				return (IsClockSet ? TimeSpan.ToString() : "Not Set") ?? "";
			}
		}

		public static readonly DateTime EpochStart = new DateTime(2000, 1, 1, 0, 0, 0);

		private const int MaxPayloadLength = 9;

		private const int SecondsFromEpicStartIndex = 1;

		private const int ClockSetStartIndex = 5;

		private const int FlagsIndex = 8;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.RealTimeClock;

		protected override int MinPayloadLength => 9;

		protected override byte[] _rawData { get; }

		public uint SecondsFromEpoch => _rawData.GetValueUInt32(1);

		public TimeSinceStart TimeSinceClockSet => new TimeSinceStart(_rawData.GetValueUInt16(5));

		private Flags FlagsRaw => (Flags)_rawData[8];

		public DateTime DateTime => EpochStart + TimeSpan.FromSeconds(SecondsFromEpoch);

		public MyRvLinkRealTimeClock(uint secondsFromEpoch, TimeSinceStart timeSinceClockSet, bool hasRtcHardware, bool usesRtcHardware)
		{
			_rawData = new byte[9];
			_rawData[0] = (byte)EventType;
			_rawData.SetValueUInt32(secondsFromEpoch, 1);
			_rawData.SetValueUInt16(timeSinceClockSet.ValueRaw, 5);
			Flags flags = Flags.None;
			if (hasRtcHardware)
			{
				flags.SetFlag(Flags.HasRtcHardware);
			}
			if (usesRtcHardware)
			{
				flags.SetFlag(Flags.UsesRtcHardware);
			}
			_rawData[8] = (byte)flags;
		}

		protected MyRvLinkRealTimeClock(IReadOnlyList<byte> rawData)
		{
			ValidateEventRawDataBasic(rawData);
			if (rawData.Count > 9)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(EventType);
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(9);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkRealTimeClock Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkRealTimeClock(rawData);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(48, 5);
			defaultInterpolatedStringHandler.AppendFormatted(EventType);
			defaultInterpolatedStringHandler.AppendLiteral(" Datetime = ");
			defaultInterpolatedStringHandler.AppendFormatted(DateTime);
			defaultInterpolatedStringHandler.AppendLiteral(" Time Since Clock Set = ");
			defaultInterpolatedStringHandler.AppendFormatted(TimeSinceClockSet);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(FlagsRaw.DebugDumpAsFlags());
			defaultInterpolatedStringHandler.AppendLiteral(" Raw Data: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
