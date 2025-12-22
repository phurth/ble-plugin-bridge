using System;

namespace IDS.Core.IDS_CAN
{
	internal struct TIME_MESSAGE_PAYLOAD
	{
		public uint Epoch { get; set; }

		public ushort TimeSinceClockSet { get; set; }

		public byte TimeZone { get; set; }

		public CAN.PAYLOAD Payload
		{
			private get
			{
				return CAN.PAYLOAD.FromArgs(Epoch, TimeSinceClockSet, TimeZone, (byte)0);
			}
			set
			{
				if (value.Length != 8)
				{
					throw new ArgumentOutOfRangeException("Payload.Length must be 8 bytes");
				}
				Epoch = value.GetUINT32(0);
				TimeSinceClockSet = value.GetUINT16(4);
				TimeZone = value[6];
			}
		}

		public TIME_MESSAGE_PAYLOAD(uint epoch, ushort time_since_clock_set, byte time_zone)
		{
			Epoch = epoch;
			TimeSinceClockSet = time_since_clock_set;
			TimeZone = time_zone;
		}

		public TIME_MESSAGE_PAYLOAD(CAN.PAYLOAD payload)
		{
			if (payload.Length != 8)
			{
				throw new ArgumentOutOfRangeException("payload.Length must be 8 bytes");
			}
			Epoch = payload.GetUINT32(0);
			TimeSinceClockSet = payload.GetUINT16(4);
			TimeZone = payload[6];
		}

		public static implicit operator CAN.PAYLOAD(TIME_MESSAGE_PAYLOAD p)
		{
			return p.Payload;
		}
	}
}
