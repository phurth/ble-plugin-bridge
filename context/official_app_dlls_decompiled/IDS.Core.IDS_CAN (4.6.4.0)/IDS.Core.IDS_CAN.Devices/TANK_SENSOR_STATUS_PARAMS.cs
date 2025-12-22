using System.Runtime.CompilerServices;

namespace IDS.Core.IDS_CAN.Devices
{
	public class TANK_SENSOR_STATUS_PARAMS : IDeviceStatusParams
	{
		private CAN.PAYLOAD payload;

		[DeviceDisplay("Fill Level")]
		public string FillLevelDisplay
		{
			get
			{
				if (FillLevel <= 100)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 1);
					defaultInterpolatedStringHandler.AppendFormatted(FillLevel);
					defaultInterpolatedStringHandler.AppendLiteral("%");
					return defaultInterpolatedStringHandler.ToStringAndClear();
				}
				return "INVALID";
			}
		}

		public byte FillLevel
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

		[DeviceDisplay("Battery Level")]
		public string BatteryLevelDisplay
		{
			get
			{
				if (BatteryLevel <= 100)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 1);
					defaultInterpolatedStringHandler.AppendFormatted(BatteryLevel);
					defaultInterpolatedStringHandler.AppendLiteral("%");
					return defaultInterpolatedStringHandler.ToStringAndClear();
				}
				return "Unknown/Not Supported";
			}
		}

		public byte BatteryLevel
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

		[DeviceDisplay("Measurement Quality")]
		public string MeasurementQualityDisplay
		{
			get
			{
				if (MeasurementQuality == byte.MaxValue)
				{
					return "Not Supported";
				}
				if (MeasurementQuality > 100)
				{
					return "Unknown/Invalid";
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(1, 1);
				defaultInterpolatedStringHandler.AppendFormatted(MeasurementQuality);
				defaultInterpolatedStringHandler.AppendLiteral("%");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public byte MeasurementQuality
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

		[DeviceDisplay("X Acceleration (G)")]
		public string XAccelerationDisplay
		{
			get
			{
				sbyte b = (sbyte)payload[3];
				if (b == sbyte.MinValue)
				{
					return "Unknown/Invalid";
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
				defaultInterpolatedStringHandler.AppendFormatted((double)b / 1024.0, "0.000");
				defaultInterpolatedStringHandler.AppendLiteral(" G");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public sbyte XAcceleration
		{
			get
			{
				return (sbyte)payload[3];
			}
			set
			{
				payload[3] = (byte)value;
			}
		}

		[DeviceDisplay("Y Acceleration (G)")]
		public string YAccelerationDisplay
		{
			get
			{
				sbyte b = (sbyte)payload[4];
				if (b == sbyte.MinValue)
				{
					return "Unknown/Invalid";
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
				defaultInterpolatedStringHandler.AppendFormatted((double)b / 1024.0, "0.000");
				defaultInterpolatedStringHandler.AppendLiteral(" G");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public sbyte YAcceleration
		{
			get
			{
				return (sbyte)payload[4];
			}
			set
			{
				payload[4] = (byte)value;
			}
		}

		[DeviceDisplay("Tank Level Alert")]
		public string TankLevelAlertDisplay
		{
			get
			{
				bool flag = (TankLevelAlert & 0x80) != 0;
				int num = TankLevelAlert & 0x7F;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Active = ");
				defaultInterpolatedStringHandler.AppendFormatted(flag);
				defaultInterpolatedStringHandler.AppendLiteral(", Count = ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public byte TankLevelAlert
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

		[DeviceDisplay("User Message")]
		public string UserMessageDisplay => ((DTC_ID)UserMessage).ToString();

		public ushort UserMessage
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

		public TANK_SENSOR_STATUS_PARAMS()
		{
			payload = new CAN.PAYLOAD(8);
			FillLevel = 0;
			BatteryLevel = 101;
			MeasurementQuality = byte.MaxValue;
			XAcceleration = sbyte.MinValue;
			YAcceleration = sbyte.MinValue;
			TankLevelAlert = 0;
			UserMessage = 0;
		}

		public void SetPayload(CAN.PAYLOAD pl)
		{
			if (pl.Length >= 1)
			{
				FillLevel = pl[0];
			}
			if (pl.Length >= 2)
			{
				BatteryLevel = pl[1];
			}
			if (pl.Length >= 3)
			{
				MeasurementQuality = pl[2];
			}
			if (pl.Length >= 4)
			{
				XAcceleration = (sbyte)pl[3];
			}
			if (pl.Length >= 5)
			{
				YAcceleration = (sbyte)pl[4];
			}
			if (pl.Length >= 6)
			{
				TankLevelAlert = pl[5];
			}
			if (pl.Length >= 8)
			{
				UserMessage = (ushort)((pl[6] << 8) | pl[7]);
			}
		}

		public CAN.PAYLOAD GetPayload()
		{
			return payload;
		}
	}
}
