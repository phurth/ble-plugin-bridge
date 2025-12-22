using System.Runtime.CompilerServices;

namespace IDS.Core.IDS_CAN.Devices
{
	public class DIMMABLE_LIGHT_STATUS_PARAMS : IDeviceStatusParams
	{
		private CAN.PAYLOAD payload;

		[DeviceDisplay("Mode")]
		public string ModeDisplay => Mode switch
		{
			0 => "OFF", 
			1 => "ON/DIMMING", 
			2 => "BLINK", 
			3 => "SWELL_TRIANGLE", 
			_ => "INVALID", 
		};

		public byte Mode
		{
			get
			{
				return payload[0];
			}
			set
			{
				payload[0] = value;
			}
		}

		[DeviceDisplay("Max Brightness")]
		public string MaxBrightnessDisplay
		{
			get
			{
				if (MaxBrightness == 0)
				{
					return "Off";
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
				defaultInterpolatedStringHandler.AppendFormatted(MaxBrightness);
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public byte MaxBrightness
		{
			get
			{
				return payload[1];
			}
			set
			{
				payload[1] = value;
			}
		}

		[DeviceDisplay("Sleep Timer")]
		public string SleepTimerDisplay
		{
			get
			{
				if (AutoOffTimeStatus == 0)
				{
					return "Infinite (disabled)";
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 1);
				defaultInterpolatedStringHandler.AppendFormatted(AutoOffTimeStatus);
				defaultInterpolatedStringHandler.AppendLiteral(" minutes");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public byte AutoOffTimeStatus
		{
			get
			{
				return payload[2];
			}
			set
			{
				payload[2] = value;
			}
		}

		[DeviceDisplay("Current Brightness")]
		public string CurrentBrightnessDisplay
		{
			get
			{
				if (CurrentBrightness == 0)
				{
					return "Off";
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 1);
				defaultInterpolatedStringHandler.AppendFormatted(CurrentBrightness);
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public byte CurrentBrightness
		{
			get
			{
				return payload[3];
			}
			set
			{
				payload[3] = value;
			}
		}

		[DeviceDisplay("Cycle Time T1")]
		public string T1Display
		{
			get
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 1);
				defaultInterpolatedStringHandler.AppendFormatted(T1);
				defaultInterpolatedStringHandler.AppendLiteral(" ms");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public ushort T1
		{
			get
			{
				return (ushort)((payload[4] << 8) | payload[5]);
			}
			set
			{
				payload[4] = (byte)(value >> 8);
				payload[5] = (byte)(value & 0xFFu);
			}
		}

		[DeviceDisplay("Cycle Time T2")]
		public string T2Display
		{
			get
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 1);
				defaultInterpolatedStringHandler.AppendFormatted(T2);
				defaultInterpolatedStringHandler.AppendLiteral(" ms");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public ushort T2
		{
			get
			{
				return (ushort)((payload[6] << 8) | payload[7]);
			}
			set
			{
				payload[6] = (byte)(value >> 8);
				payload[7] = (byte)(value & 0xFFu);
			}
		}

		public DIMMABLE_LIGHT_STATUS_PARAMS()
		{
			payload = new CAN.PAYLOAD(8);
			Mode = 0;
			MaxBrightness = 0;
			AutoOffTimeStatus = 0;
			CurrentBrightness = 0;
			T1 = 0;
			T2 = 0;
		}

		public void SetPayload(CAN.PAYLOAD pl)
		{
			if (pl.Length >= 1)
			{
				Mode = pl[0];
			}
			if (pl.Length >= 2)
			{
				MaxBrightness = pl[1];
			}
			if (pl.Length >= 3)
			{
				AutoOffTimeStatus = pl[2];
			}
			if (pl.Length >= 4)
			{
				CurrentBrightness = pl[3];
			}
			if (pl.Length >= 6)
			{
				T1 = (ushort)((pl[4] << 8) | pl[5]);
			}
			if (pl.Length >= 8)
			{
				T2 = (ushort)((pl[6] << 8) | pl[7]);
			}
		}

		public CAN.PAYLOAD GetPayload()
		{
			return payload;
		}
	}
}
