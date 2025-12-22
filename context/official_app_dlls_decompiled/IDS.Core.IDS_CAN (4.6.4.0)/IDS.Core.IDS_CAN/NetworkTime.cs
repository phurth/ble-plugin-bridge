using System;
using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class NetworkTime : Adapter.BackgroundTaskObject, INetworkTime
	{
		private static readonly TimeSpan TIME_MESSAGE_TX_PERIOD = TimeSpan.FromSeconds(1.0);

		private static readonly TimeSpan MINIMUM_AUTHORITY_TIME = TimeSpan.FromSeconds(5.0);

		private Timer TimeSinceLastClockAuthoritySeen = new Timer();

		private Timer AuthorityTime = new Timer();

		private Timer TimeSinceLastTx = new Timer();

		private TimeSpan NetworkClockMissingTimeout = TimeSpan.FromSeconds(10.0);

		private uint LastTxEpoch;

		private Timer Stopwatch = new Timer();

		private static readonly DateTime Jan_1_2000 = new DateTime(2000, 1, 1, 0, 0, 0);

		public bool IsValid { get; private set; }

		public bool IsTimeAuthority { get; private set; }

		public DateTime CurrentDateTime
		{
			get
			{
				return Jan_1_2000 + TimeSpan.FromSeconds(RTC_EPOCH_SEC);
			}
			set
			{
				RTC_EPOCH_SEC = (uint)(value - Jan_1_2000).TotalSeconds;
			}
		}

		public DateTime TimeLastSet => Jan_1_2000 + TimeSpan.FromSeconds(RTC_SET_TIME_SEC);

		public byte TIME_ZONE { get; private set; }

		public byte RTC_TIME_SEC
		{
			get
			{
				return (byte)CurrentDateTime.Second;
			}
			set
			{
				SetTime(RTC_TIME_YEAR, RTC_TIME_MONTH, RTC_TIME_DAY, RTC_TIME_HOUR, RTC_TIME_MIN, value);
			}
		}

		public byte RTC_TIME_MIN
		{
			get
			{
				return (byte)CurrentDateTime.Minute;
			}
			set
			{
				SetTime(RTC_TIME_YEAR, RTC_TIME_MONTH, RTC_TIME_DAY, RTC_TIME_HOUR, value, RTC_TIME_SEC);
			}
		}

		public byte RTC_TIME_HOUR
		{
			get
			{
				return (byte)CurrentDateTime.Hour;
			}
			set
			{
				SetTime(RTC_TIME_YEAR, RTC_TIME_MONTH, RTC_TIME_DAY, value, RTC_TIME_MIN, RTC_TIME_SEC);
			}
		}

		public byte RTC_TIME_DAY
		{
			get
			{
				return (byte)CurrentDateTime.Day;
			}
			set
			{
				SetTime(RTC_TIME_YEAR, RTC_TIME_MONTH, value, RTC_TIME_HOUR, RTC_TIME_MIN, RTC_TIME_SEC);
			}
		}

		public byte RTC_TIME_MONTH
		{
			get
			{
				return (byte)CurrentDateTime.Month;
			}
			set
			{
				SetTime(RTC_TIME_YEAR, value, RTC_TIME_DAY, RTC_TIME_HOUR, RTC_TIME_MIN, RTC_TIME_SEC);
			}
		}

		public ushort RTC_TIME_YEAR
		{
			get
			{
				return (ushort)CurrentDateTime.Year;
			}
			set
			{
				SetTime(value, RTC_TIME_MONTH, RTC_TIME_DAY, RTC_TIME_HOUR, RTC_TIME_MIN, RTC_TIME_SEC);
			}
		}

		public uint RTC_EPOCH_SEC
		{
			get
			{
				return RTC_SET_TIME_SEC + (uint)Stopwatch.ElapsedTime.TotalSeconds;
			}
			set
			{
				RTC_SET_TIME_SEC = value;
				Stopwatch.Reset();
				IsValid = true;
				IsTimeAuthority = true;
				CalcRandomClockMissingTimeout();
				AuthorityTime.Reset();
			}
		}

		public uint RTC_SET_TIME_SEC { get; private set; }

		public ushort TIME_SINCE_CLOCK_SET
		{
			get
			{
				if (!IsValid)
				{
					return ushort.MaxValue;
				}
				uint num = RTC_EPOCH_SEC - RTC_SET_TIME_SEC;
				if (num < 16384)
				{
					return (ushort)(0u | num);
				}
				uint num2 = num / 60u;
				if (num2 < 16384)
				{
					return (ushort)(0x4000u | num2);
				}
				uint num3 = num2 / 60u;
				if (num3 < 16384)
				{
					return (ushort)(0x8000u | num3);
				}
				uint num4 = num3 / 24u;
				if (num4 < 16383)
				{
					return (ushort)(0xC000u | num4);
				}
				return 65534;
			}
		}

		public NetworkTime(Adapter adapter)
			: base(adapter)
		{
			IsValid = false;
			IsTimeAuthority = false;
			TIME_ZONE = 0;
			CalcRandomClockMissingTimeout();
			adapter.Events.Subscribe<AdapterRxEvent>(OnAdapterRx, SubscriptionType.Weak, base.Subscriptions);
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				IsTimeAuthority = false;
				IsValid = false;
				base.Subscriptions.Dispose();
			}
		}

		private void CalcRandomClockMissingTimeout()
		{
			NetworkClockMissingTimeout = TimeSpan.FromSeconds(5.0 + 5.0 * ThreadLocalRandom.NextDouble());
		}

		public void SetTime(int year, int month, int day, int hour, int minute, int second)
		{
			CurrentDateTime = new DateTime(year, month, day, hour, minute, second);
		}

		private void OnAdapterRx(AdapterRxEvent rx)
		{
			if (base.IsDisposed || (byte)rx.MessageType != 7 || rx.Count != 8)
			{
				return;
			}
			TimeSinceLastClockAuthoritySeen.Reset();
			ILocalDevice localHost = base.LocalHost;
			if ((localHost != null && localHost.IsOnline && rx.SourceAddress == base.LocalHost?.Address) || (IsTimeAuthority && AuthorityTime.ElapsedTime < MINIMUM_AUTHORITY_TIME))
			{
				return;
			}
			TIME_MESSAGE_PAYLOAD tIME_MESSAGE_PAYLOAD = new TIME_MESSAGE_PAYLOAD(rx.Payload);
			bool num = tIME_MESSAGE_PAYLOAD.TimeSinceClockSet <= TIME_SINCE_CLOCK_SET;
			bool flag = Math.Abs((double)tIME_MESSAGE_PAYLOAD.Epoch - (double)RTC_EPOCH_SEC) < 30.0;
			if (num || flag)
			{
				IsTimeAuthority = false;
				uint num2 = 1u;
				switch (tIME_MESSAGE_PAYLOAD.TimeSinceClockSet & 0xC000)
				{
				case 0:
					num2 = 1u;
					break;
				case 16384:
					num2 = 60u;
					break;
				case 32768:
					num2 = 3600u;
					break;
				case 49152:
					num2 = 86400u;
					break;
				}
				uint num3 = (uint)(((tIME_MESSAGE_PAYLOAD.TimeSinceClockSet & 0x3FFF) + 1) * (int)num2 - 1);
				uint num4 = tIME_MESSAGE_PAYLOAD.Epoch - num3;
				if (num4 > RTC_SET_TIME_SEC || tIME_MESSAGE_PAYLOAD.Epoch < RTC_SET_TIME_SEC)
				{
					RTC_SET_TIME_SEC = num4;
				}
				Stopwatch.ElapsedTime = TimeSpan.FromSeconds(tIME_MESSAGE_PAYLOAD.Epoch - RTC_SET_TIME_SEC);
				TIME_ZONE = rx[6];
				IsValid = tIME_MESSAGE_PAYLOAD.TimeSinceClockSet != ushort.MaxValue;
			}
			else
			{
				IsTimeAuthority = true;
			}
		}

		public override void BackgroundTask()
		{
			if (!IsTimeAuthority)
			{
				if (TimeSinceLastClockAuthoritySeen.ElapsedTime < NetworkClockMissingTimeout)
				{
					return;
				}
				IsTimeAuthority = true;
			}
			ILocalDevice localHost = base.LocalHost;
			if (localHost != null && localHost.IsOnline && (RTC_EPOCH_SEC != LastTxEpoch || TimeSinceLastTx.ElapsedTime >= TIME_MESSAGE_TX_PERIOD))
			{
				TIME_MESSAGE_PAYLOAD tIME_MESSAGE_PAYLOAD = new TIME_MESSAGE_PAYLOAD(RTC_EPOCH_SEC, TIME_SINCE_CLOCK_SET, TIME_ZONE);
				if (base.LocalHost.Transmit11((byte)7, tIME_MESSAGE_PAYLOAD))
				{
					LastTxEpoch = tIME_MESSAGE_PAYLOAD.Epoch;
					TimeSinceLastTx.Reset();
				}
			}
		}
	}
}
