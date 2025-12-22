using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace IDS.Core.IDS_CAN.Devices
{
	public class TEMPERATURE_SENSOR_STATUS_PARAMS : IDeviceStatusParams
	{
		private CAN.PAYLOAD payload;

		private short mTemperatureC;

		private byte mBatteryVoltage;

		private byte mBatteryLevel;

		private byte mLowBattAlert;

		private byte mHighTempAlert;

		private byte mLowTempAlert;

		private byte mTempInRangeAlert;

		[DeviceDisplay("Temperature (C)")]
		public string TemperatureCDisplay
		{
			get
			{
				if (TemperatureC < short.MinValue || TemperatureC > 32512)
				{
					return "INVALID";
				}
				double num = (double)mTemperatureC / 256.0;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 1);
				defaultInterpolatedStringHandler.AppendFormatted(num, "0.00");
				defaultInterpolatedStringHandler.AppendLiteral("°C");
				return defaultInterpolatedStringHandler.ToStringAndClear();
			}
		}

		public short TemperatureC
		{
			get
			{
				return mTemperatureC;
			}
			set
			{
				mTemperatureC = value;
				payload[0] = (byte)(mTemperatureC >> 8);
				payload[1] = (byte)mTemperatureC;
			}
		}

		public byte BatteryVoltage
		{
			get
			{
				return mBatteryVoltage;
			}
			set
			{
				byte b2 = (payload[2] = value);
				mBatteryVoltage = b2;
			}
		}

		public byte BatteryLevel
		{
			get
			{
				return mBatteryLevel;
			}
			set
			{
				byte b2 = (payload[3] = value);
				mBatteryLevel = b2;
			}
		}

		public byte LowBattAlert
		{
			get
			{
				return mLowBattAlert;
			}
			set
			{
				byte b2 = (payload[4] = value);
				mLowBattAlert = b2;
			}
		}

		public byte HighTempAlert
		{
			get
			{
				return mHighTempAlert;
			}
			set
			{
				byte b2 = (payload[5] = value);
				mHighTempAlert = b2;
			}
		}

		public byte LowTempAlert
		{
			get
			{
				return mLowTempAlert;
			}
			set
			{
				byte b2 = (payload[6] = value);
				mLowTempAlert = b2;
			}
		}

		public byte TempInRangeAlert
		{
			get
			{
				return mTempInRangeAlert;
			}
			set
			{
				byte b2 = (payload[7] = value);
				mTempInRangeAlert = b2;
			}
		}

		public CAN.PAYLOAD GetPayload()
		{
			return payload;
		}

		public TEMPERATURE_SENSOR_STATUS_PARAMS()
		{
			payload = new CAN.PAYLOAD(8);
			TemperatureC = 0;
			BatteryVoltage = 0;
			BatteryLevel = 0;
			LowBattAlert = 0;
			HighTempAlert = 0;
			LowTempAlert = 0;
			TempInRangeAlert = 0;
		}

		public void SetPayload(CAN.PAYLOAD pl)
		{
			TemperatureC = (short)((short)(pl[0] << 8) + pl[1]);
			BatteryVoltage = pl[2];
			BatteryLevel = pl[3];
			LowBattAlert = pl[4];
			HighTempAlert = pl[5];
			LowTempAlert = pl[6];
			TempInRangeAlert = pl[7];
		}

		public IEnumerable<MemberInfo> GetMembers()
		{
			return Enumerable.Where(GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public), (MemberInfo m) => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field);
		}
	}
}
