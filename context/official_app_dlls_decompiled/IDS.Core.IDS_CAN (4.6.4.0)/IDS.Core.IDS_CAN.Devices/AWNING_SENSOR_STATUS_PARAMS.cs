using System.Runtime.CompilerServices;

namespace IDS.Core.IDS_CAN.Devices
{
	public class AWNING_SENSOR_STATUS_PARAMS : IDeviceStatusParams
	{
		private CAN.PAYLOAD payload;

		private ushort mAngle;

		[DeviceDisplay("Angle")]
		public string AngleDisplay
		{
			get
			{
				double num = (double)(short)mAngle / 128.0;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 1);
				defaultInterpolatedStringHandler.AppendFormatted(num, "0.00");
				defaultInterpolatedStringHandler.AppendLiteral("°");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public ushort Angle
		{
			get
			{
				return mAngle;
			}
			set
			{
				if (mAngle != value)
				{
					mAngle = value;
					payload[0] = (byte)(value >> 8);
					payload[1] = (byte)(value & 0xFFu);
				}
			}
		}

		[DeviceDisplay("High Wind Alert")]
		public string HighWindAlertDisplay
		{
			get
			{
				bool flag = (HighWindAlert & 0x80) != 0;
				int num = HighWindAlert & 0x7F;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Active = ");
				defaultInterpolatedStringHandler.AppendFormatted(flag);
				defaultInterpolatedStringHandler.AppendLiteral(", Count = ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public byte HighWindAlert
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

		[DeviceDisplay("Medium Wind Alert")]
		public string MediumWindAlertDisplay
		{
			get
			{
				bool flag = (MediumWindAlert & 0x80) != 0;
				int num = MediumWindAlert & 0x7F;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Active = ");
				defaultInterpolatedStringHandler.AppendFormatted(flag);
				defaultInterpolatedStringHandler.AppendLiteral(", Count = ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public byte MediumWindAlert
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

		[DeviceDisplay("Low Wind Alert")]
		public string LowWindAlertDisplay
		{
			get
			{
				bool flag = (LowWindAlert & 0x80) != 0;
				int num = LowWindAlert & 0x7F;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Active = ");
				defaultInterpolatedStringHandler.AppendFormatted(flag);
				defaultInterpolatedStringHandler.AppendLiteral(", Count = ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public byte LowWindAlert
		{
			get
			{
				return payload[4];
			}
			set
			{
				payload[4] = value;
			}
		}

		[DeviceDisplay("Battery Level")]
		public string BatteryLevelDisplay
		{
			get
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 1);
				defaultInterpolatedStringHandler.AppendFormatted(BatteryLevel);
				defaultInterpolatedStringHandler.AppendLiteral("%");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public byte BatteryLevel
		{
			get
			{
				return payload[5];
			}
			set
			{
				payload[5] = value;
			}
		}

		[DeviceDisplay("Low Battery Alert")]
		public string LowBattAlertDisplay
		{
			get
			{
				bool flag = (LowBattAlert & 0x80) != 0;
				int num = LowBattAlert & 0x7F;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Active = ");
				defaultInterpolatedStringHandler.AppendFormatted(flag);
				defaultInterpolatedStringHandler.AppendLiteral(", Count = ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public byte LowBattAlert
		{
			get
			{
				return payload[6];
			}
			set
			{
				payload[6] = value;
			}
		}

		[DeviceDisplay("Misc Status")]
		public string MiscStatusDisplay => "Audible Alert = " + (MiscStatus & 3) switch
		{
			0 => "not sounding", 
			1 => "Pre-movement active", 
			2 => "Intra-movement active", 
			_ => "Reserved", 
		} + "\nPerceived Awning State = " + ((MiscStatus & 0xC) >> 2) switch
		{
			0 => "Unknown/Unavailable", 
			1 => "Extending", 
			2 => "Retracting", 
			_ => "Stationary", 
		};

		public byte MiscStatus
		{
			get
			{
				return payload[7];
			}
			set
			{
				payload[7] = value;
			}
		}

		public AWNING_SENSOR_STATUS_PARAMS()
		{
			payload = new CAN.PAYLOAD(8);
			Angle = 0;
			HighWindAlert = 0;
			MediumWindAlert = 0;
			LowWindAlert = 0;
			BatteryLevel = 0;
			LowBattAlert = 0;
			MiscStatus = 0;
		}

		public void SetPayload(CAN.PAYLOAD pl)
		{
			if (pl.Length >= 2)
			{
				Angle = (ushort)((pl[0] << 8) | pl[1]);
			}
			if (pl.Length >= 3)
			{
				HighWindAlert = pl[2];
			}
			if (pl.Length >= 4)
			{
				MediumWindAlert = pl[3];
			}
			if (pl.Length >= 5)
			{
				LowWindAlert = pl[4];
			}
			if (pl.Length >= 6)
			{
				BatteryLevel = pl[5];
			}
			if (pl.Length >= 7)
			{
				LowBattAlert = pl[6];
			}
			if (pl.Length >= 8)
			{
				MiscStatus = pl[7];
			}
		}

		public CAN.PAYLOAD GetPayload()
		{
			return payload;
		}
	}
}
