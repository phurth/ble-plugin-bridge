using System;
using System.Collections.Generic;

namespace IDS.Core.IDS_CAN
{
	public sealed class PID
	{
		private enum FORMAT
		{
			CHAR1,
			CHAR2,
			CHAR3,
			CHAR4,
			CHAR5,
			CHAR6,
			INT8,
			UINT8,
			INT16,
			UINT16,
			INT24,
			UINT24,
			INT32,
			UINT32,
			INT40,
			UINT40,
			INT48,
			UINT48,
			DATE_TIME,
			DATE_TIME_EPOCH,
			IPV4,
			IPV6,
			MAC2,
			MAC6,
			ADDRESS16_DATA32,
			BYTE1,
			BYTE2,
			BYTE3,
			BYTE4,
			BYTE5,
			BYTE6
		}

		private class Formatter
		{
			private readonly FORMAT Format;

			private readonly string FormatString;

			private readonly double Scale;

			private readonly double Offset;

			internal Formatter(FORMAT f)
			{
				Format = f;
			}

			internal Formatter(FORMAT f, string s)
			{
				Format = f;
				FormatString = s;
			}

			internal Formatter(FORMAT f, string s, double scale)
			{
				Format = f;
				FormatString = s;
				Scale = scale;
			}

			internal Formatter(FORMAT f, string s, double scale, double offset)
			{
				Format = f;
				FormatString = s;
				Scale = scale;
				Offset = offset;
			}

			public string ToString(ulong value)
			{
				switch (Format)
				{
				case FORMAT.UINT8:
					value &= 0xFF;
					break;
				case FORMAT.UINT16:
					value &= 0xFFFF;
					break;
				case FORMAT.UINT24:
					value &= 0xFFFFFF;
					break;
				case FORMAT.UINT32:
					value &= 0xFFFFFFFFu;
					break;
				case FORMAT.UINT40:
					value &= 0xFFFFFFFFFFuL;
					break;
				case FORMAT.UINT48:
					value &= 0xFFFFFFFFFFFFuL;
					break;
				case FORMAT.INT8:
					value &= 0xFF;
					if ((value & 0x80) != 0L)
					{
						value |= 0xFFFFFFFFFFFFFF00uL;
					}
					break;
				case FORMAT.INT16:
					value &= 0xFFFF;
					if ((value & 0x8000) != 0L)
					{
						value |= 0xFFFFFFFFFFFF0000uL;
					}
					break;
				case FORMAT.INT24:
					value &= 0xFFFFFF;
					if ((value & 0x800000) != 0L)
					{
						value |= 0xFFFFFFFFFF000000uL;
					}
					break;
				case FORMAT.INT32:
					value &= 0xFFFFFFFFu;
					if ((value & 0x80000000u) != 0L)
					{
						value |= 0xFFFFFFFF00000000uL;
					}
					break;
				case FORMAT.INT40:
					value &= 0xFFFFFFFFFFuL;
					if ((value & 0x8000000000L) != 0L)
					{
						value |= 0xFFFFFF0000000000uL;
					}
					break;
				case FORMAT.INT48:
					value &= 0xFFFFFFFFFFFFuL;
					if ((value & 0x800000000000L) != 0L)
					{
						value |= 0xFFFF000000000000uL;
					}
					break;
				case FORMAT.CHAR1:
					return CharString(value, 1);
				case FORMAT.CHAR2:
					return CharString(value, 2);
				case FORMAT.CHAR3:
					return CharString(value, 3);
				case FORMAT.CHAR4:
					return CharString(value, 4);
				case FORMAT.CHAR5:
					return CharString(value, 5);
				case FORMAT.CHAR6:
					return CharString(value, 6);
				case FORMAT.BYTE1:
					return ByteString(value, 1);
				case FORMAT.BYTE2:
					return ByteString(value, 2);
				case FORMAT.BYTE3:
					return ByteString(value, 3);
				case FORMAT.BYTE4:
					return ByteString(value, 4);
				case FORMAT.BYTE5:
					return ByteString(value, 5);
				case FORMAT.BYTE6:
					return ByteString(value, 6);
				case FORMAT.DATE_TIME:
				{
					int year = 2000 + (byte)(value >> 40);
					int month = (byte)(value >> 32);
					int day = (byte)(value >> 24);
					int hour = (byte)(value >> 16);
					int minute = (byte)(value >> 8);
					int second = (byte)value;
					try
					{
						return new DateTime(year, month, day, hour, minute, second).ToString();
					}
					catch (ArgumentOutOfRangeException)
					{
						return "00/00/2000 00:00:00 AM";
					}
				}
				case FORMAT.DATE_TIME_EPOCH:
					return new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(value).ToString();
				case FORMAT.IPV4:
					return IPString(value, 4);
				case FORMAT.IPV6:
					return IPString(value, 6);
				case FORMAT.MAC2:
					return MACstring(value, 2);
				case FORMAT.MAC6:
					return MACstring(value, 6);
				case FORMAT.ADDRESS16_DATA32:
					return "Address: $" + ((ushort)((value >> 32) & 0xFFFF)).ToString("X4") + " Read data: $" + ((uint)(value & 0xFFFFFFFFu)).ToString("X8");
				}
				long num = (long)value;
				if (Scale == 0.0 && Offset == 0.0)
				{
					if (FormatString == null)
					{
						return num.ToString();
					}
					return string.Format(FormatString, num);
				}
				double num2 = (double)num * 1.0;
				if (Scale != 0.0)
				{
					num2 *= Scale;
				}
				num2 += Offset;
				if (FormatString == null)
				{
					return num2.ToString();
				}
				return string.Format(FormatString, num2);
			}

			private string ByteString(ulong value, int digits)
			{
				string text = "";
				if (digits < 1)
				{
					digits = 1;
				}
				if (digits > 8)
				{
					digits = 8;
				}
				int num = 8 - digits;
				value <<= num * 8;
				for (int i = 0; i < digits; i++)
				{
					if (i != 0)
					{
						text += " ";
					}
					byte b = (byte)(value >> 56);
					value <<= 8;
					string text2 = text;
					byte b2 = b;
					text = text2 + b2.ToString("X2");
				}
				return text;
			}

			private string CharString(ulong value, int digits)
			{
				string text = "";
				if (digits < 1)
				{
					digits = 1;
				}
				if (digits > 8)
				{
					digits = 8;
				}
				int num = 8 - digits;
				value <<= num * 8;
				for (int i = 0; i < digits; i++)
				{
					char c = (char)(value >> 56);
					value <<= 8;
					if (c == '\0')
					{
						break;
					}
					text += c;
				}
				return text;
			}

			private string IPString(ulong value, int digits)
			{
				string text = "";
				int num = 0;
				while (num < digits)
				{
					if (num != 0)
					{
						text = "." + text;
					}
					text = (byte)value + text;
					num++;
					value >>= 8;
				}
				return text;
			}

			private string MACstring(ulong value, int digits)
			{
				string text = "";
				int num = 0;
				while (num < digits)
				{
					if (num != 0)
					{
						text = ":" + text;
					}
					text = ((byte)value).ToString("X2") + text;
					num++;
					value >>= 8;
				}
				return text;
			}
		}

		private static readonly Dictionary<ushort, PID> Lookup = new Dictionary<ushort, PID>();

		private static readonly List<PID> List = new List<PID>();

		public static readonly PID UNKNOWN = new PID(0, "UNKNOWN", new Formatter(FORMAT.UINT48, "${0:X12}"), 0);

		public static readonly PID PRODUCTION_BYTES = new PID(1, "PRODUCTION_BYTES", new Formatter(FORMAT.UINT48, "${0:X8}"), 1);

		public static readonly PID CAN_ADAPTER_MAC = new PID(2, "CAN_ADAPTER_MAC", new Formatter(FORMAT.MAC6), 1);

		public static readonly PID IDS_CAN_CIRCUIT_ID = new PID(3, "IDS_CAN_CIRCUIT_ID", new Formatter(FORMAT.UINT32, "${0:X8}"), 2);

		public static readonly PID IDS_CAN_FUNCTION_NAME = new PID(4, "IDS_CAN_FUNCTION_NAME", new Formatter(FORMAT.UINT16, "${0:X4}"), 2);

		public static readonly PID IDS_CAN_FUNCTION_INSTANCE = new PID(5, "IDS_CAN_FUNCTION_INSTANCE", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID IDS_CAN_NUM_DEVICES_ON_NETWORK = new PID(6, "IDS_CAN_NUM_DEVICES_ON_NETWORK", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID IDS_CAN_MAX_NETWORK_HEARTBEAT_TIME = new PID(7, "IDS_CAN_MAX_NETWORK_HEARTBEAT_TIME", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID SERIAL_NUMBER = new PID(8, "SERIAL_NUMBER", new Formatter(FORMAT.UINT48, "${0:X12}"), 1);

		public static readonly PID CAN_BYTES_TX = new PID(9, "CAN_BYTES_TX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID CAN_BYTES_RX = new PID(10, "CAN_BYTES_RX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID CAN_MESSAGES_TX = new PID(11, "CAN_MESSAGES_TX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID CAN_MESSAGES_RX = new PID(12, "CAN_MESSAGES_RX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID CAN_TX_BUFFER_OVERFLOW_COUNT = new PID(13, "CAN_TX_BUFFER_OVERFLOW_COUNT", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID CAN_RX_BUFFER_OVERFLOW_COUNT = new PID(14, "CAN_RX_BUFFER_OVERFLOW_COUNT", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID CAN_TX_MAX_BYTES_QUEUED = new PID(15, "CAN_TX_MAX_BYTES_QUEUED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID CAN_RX_MAX_BYTES_QUEUED = new PID(16, "CAN_RX_MAX_BYTES_QUEUED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID UART_BYTES_TX = new PID(17, "UART_BYTES_TX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID UART_BYTES_RX = new PID(18, "UART_BYTES_RX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID UART_MESSAGES_TX = new PID(19, "UART_MESSAGES_TX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID UART_MESSAGES_RX = new PID(20, "UART_MESSAGES_RX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID UART_TX_BUFFER_OVERFLOW_COUNT = new PID(21, "UART_TX_BUFFER_OVERFLOW_COUNT", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID UART_RX_BUFFER_OVERFLOW_COUNT = new PID(22, "UART_RX_BUFFER_OVERFLOW_COUNT", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID UART_TX_MAX_BYTES_QUEUED = new PID(23, "UART_TX_MAX_BYTES_QUEUED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID UART_RX_MAX_BYTES_QUEUED = new PID(24, "UART_RX_MAX_BYTES_QUEUED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID WIFI_BYTES_TX = new PID(25, "WIFI_BYTES_TX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID WIFI_BYTES_RX = new PID(26, "WIFI_BYTES_RX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID WIFI_MESSAGES_TX = new PID(27, "WIFI_MESSAGES_TX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID WIFI_MESSAGES_RX = new PID(28, "WIFI_MESSAGES_RX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID WIFI_TX_BUFFER_OVERFLOW_COUNT = new PID(29, "WIFI_TX_BUFFER_OVERFLOW_COUNT", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID WIFI_RX_BUFFER_OVERFLOW_COUNT = new PID(30, "WIFI_RX_BUFFER_OVERFLOW_COUNT", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID WIFI_TX_MAX_BYTES_QUEUED = new PID(31, "WIFI_TX_MAX_BYTES_QUEUED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID WIFI_RX_MAX_BYTES_QUEUED = new PID(32, "WIFI_RX_MAX_BYTES_QUEUED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID WIFI_RSSI = new PID(33, "WIFI_RSSI", new Formatter(FORMAT.INT32, "{0:0.###} dBm", 1.52587890625E-05), 0);

		public static readonly PID RF_BYTES_TX = new PID(34, "RF_BYTES_TX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID RF_BYTES_RX = new PID(35, "RF_BYTES_RX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID RF_MESSAGES_TX = new PID(36, "RF_MESSAGES_TX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID RF_MESSAGES_RX = new PID(37, "RF_MESSAGES_RX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID RF_TX_BUFFER_OVERFLOW_COUNT = new PID(38, "RF_TX_BUFFER_OVERFLOW_COUNT", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID RF_RX_BUFFER_OVERFLOW_COUNT = new PID(39, "RF_RX_BUFFER_OVERFLOW_COUNT", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID RF_TX_MAX_BYTES_QUEUED = new PID(40, "RF_TX_MAX_BYTES_QUEUED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID RF_RX_MAX_BYTES_QUEUED = new PID(41, "RF_RX_MAX_BYTES_QUEUED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID RF_RSSI = new PID(42, "RF_RSSI", new Formatter(FORMAT.INT32, "{0:0.###} dBm", 1.52587890625E-05), 0);

		public static readonly PID BATTERY_VOLTAGE = new PID(43, "BATTERY_VOLTAGE", new Formatter(FORMAT.UINT32, "{0:0.###} V", 1.52587890625E-05), 0);

		public static readonly PID REGULATOR_VOLTAGE = new PID(44, "REGULATOR_VOLTAGE", new Formatter(FORMAT.UINT32, "{0:0.###} V", 1.52587890625E-05), 0);

		public static readonly PID NUM_TILT_SENSOR_AXES = new PID(45, "NUM_TILT_SENSOR_AXES", new Formatter(FORMAT.UINT32, "{0:#,###0}"), 0);

		public static readonly PID TILT_AXIS_1_ANGLE = new PID(46, "TILT_AXIS_1_ANGLE", new Formatter(FORMAT.INT32, "{0:0.###}°", 1.52587890625E-05), 0);

		public static readonly PID TILT_AXIS_2_ANGLE = new PID(47, "TILT_AXIS_2_ANGLE", new Formatter(FORMAT.INT32, "{0:0.###}°", 1.52587890625E-05), 0);

		public static readonly PID TILT_AXIS_3_ANGLE = new PID(48, "TILT_AXIS_3_ANGLE", new Formatter(FORMAT.INT32, "{0:0.###}°", 1.52587890625E-05), 0);

		public static readonly PID TILT_AXIS_4_ANGLE = new PID(49, "TILT_AXIS_4_ANGLE", new Formatter(FORMAT.INT32, "{0:0.###}°", 1.52587890625E-05), 0);

		public static readonly PID TILT_AXIS_5_ANGLE = new PID(50, "TILT_AXIS_5_ANGLE", new Formatter(FORMAT.INT32, "{0:0.###}°", 1.52587890625E-05), 0);

		public static readonly PID TILT_AXIS_6_ANGLE = new PID(51, "TILT_AXIS_6_ANGLE", new Formatter(FORMAT.INT32, "{0:0.###}°", 1.52587890625E-05), 0);

		public static readonly PID TILT_AXIS_7_ANGLE = new PID(52, "TILT_AXIS_7_ANGLE", new Formatter(FORMAT.INT32, "{0:0.###}°", 1.52587890625E-05), 0);

		public static readonly PID TILT_AXIS_8_ANGLE = new PID(53, "TILT_AXIS_8_ANGLE", new Formatter(FORMAT.INT32, "{0:0.###}°", 1.52587890625E-05), 0);

		public static readonly PID IDS_CAN_FIXED_ADDRESS = new PID(54, "IDS_CAN_FIXED_ADDRESS", new Formatter(FORMAT.UINT8, "${0:X2}"), 1);

		public static readonly PID FUSE_SETTING_1 = new PID(55, "FUSE_SETTING_1", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_2 = new PID(56, "FUSE_SETTING_2", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_3 = new PID(57, "FUSE_SETTING_3", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_4 = new PID(58, "FUSE_SETTING_4", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_5 = new PID(59, "FUSE_SETTING_5", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_6 = new PID(60, "FUSE_SETTING_6", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_7 = new PID(61, "FUSE_SETTING_7", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_8 = new PID(62, "FUSE_SETTING_8", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_9 = new PID(63, "FUSE_SETTING_9", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_10 = new PID(64, "FUSE_SETTING_10", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_11 = new PID(65, "FUSE_SETTING_11", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_12 = new PID(66, "FUSE_SETTING_12", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_13 = new PID(67, "FUSE_SETTING_13", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_14 = new PID(68, "FUSE_SETTING_14", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_15 = new PID(69, "FUSE_SETTING_15", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID FUSE_SETTING_16 = new PID(70, "FUSE_SETTING_16", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_1 = new PID(71, "MANUFACTURING_PID_1", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_2 = new PID(72, "MANUFACTURING_PID_2", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_3 = new PID(73, "MANUFACTURING_PID_3", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_4 = new PID(74, "MANUFACTURING_PID_4", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_5 = new PID(75, "MANUFACTURING_PID_5", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_6 = new PID(76, "MANUFACTURING_PID_6", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_7 = new PID(77, "MANUFACTURING_PID_7", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_8 = new PID(78, "MANUFACTURING_PID_8", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_9 = new PID(79, "MANUFACTURING_PID_9", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_10 = new PID(80, "MANUFACTURING_PID_10", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_11 = new PID(81, "MANUFACTURING_PID_11", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_12 = new PID(82, "MANUFACTURING_PID_12", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_13 = new PID(83, "MANUFACTURING_PID_13", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_14 = new PID(84, "MANUFACTURING_PID_14", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_15 = new PID(85, "MANUFACTURING_PID_15", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_16 = new PID(86, "MANUFACTURING_PID_16", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_17 = new PID(87, "MANUFACTURING_PID_17", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_18 = new PID(88, "MANUFACTURING_PID_18", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_19 = new PID(89, "MANUFACTURING_PID_19", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_20 = new PID(90, "MANUFACTURING_PID_20", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_21 = new PID(91, "MANUFACTURING_PID_21", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_22 = new PID(92, "MANUFACTURING_PID_22", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_23 = new PID(93, "MANUFACTURING_PID_23", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_24 = new PID(94, "MANUFACTURING_PID_24", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_25 = new PID(95, "MANUFACTURING_PID_25", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_26 = new PID(96, "MANUFACTURING_PID_26", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_27 = new PID(97, "MANUFACTURING_PID_27", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_28 = new PID(98, "MANUFACTURING_PID_28", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_29 = new PID(99, "MANUFACTURING_PID_29", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_30 = new PID(100, "MANUFACTURING_PID_30", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_31 = new PID(101, "MANUFACTURING_PID_31", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID MANUFACTURING_PID_32 = new PID(102, "MANUFACTURING_PID_32", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID METERED_TIME_SEC = new PID(103, "METERED_TIME_SEC", new Formatter(FORMAT.UINT32, "${0:X8}"), 2);

		public static readonly PID MAINTENANCE_PERIOD_SEC = new PID(104, "MAINTENANCE_PERIOD_SEC", new Formatter(FORMAT.UINT32, "${0:X8}"), 2);

		public static readonly PID LAST_MAINTENANCE_TIME_SEC = new PID(105, "LAST_MAINTENANCE_TIME_SEC", new Formatter(FORMAT.UINT32, "${0:X8}"), 2);

		public static readonly PID TIME_ZONE = new PID(106, "TIME_ZONE", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID RTC_TIME_SEC = new PID(107, "RTC_TIME_SEC", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID RTC_TIME_MIN = new PID(108, "RTC_TIME_MIN", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID RTC_TIME_HOUR = new PID(109, "RTC_TIME_HOUR", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID RTC_TIME_DAY = new PID(110, "RTC_TIME_DAY", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID RTC_TIME_MONTH = new PID(111, "RTC_TIME_MONTH", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID RTC_TIME_YEAR = new PID(112, "RTC_TIME_YEAR", new Formatter(FORMAT.UINT16, "${0:X4}"), 2);

		public static readonly PID RTC_EPOCH_SEC = new PID(113, "RTC_EPOCH_SEC", new Formatter(FORMAT.DATE_TIME_EPOCH), 2);

		public static readonly PID RTC_SET_TIME_SEC = new PID(114, "RTC_SET_TIME_SEC", new Formatter(FORMAT.DATE_TIME_EPOCH), 2);

		public static readonly PID BLE_DEVICE_NAME_1 = new PID(115, "BLE_DEVICE_NAME_1", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID BLE_DEVICE_NAME_2 = new PID(116, "BLE_DEVICE_NAME_2", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID BLE_DEVICE_NAME_3 = new PID(117, "BLE_DEVICE_NAME_3", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID BLE_PIN = new PID(118, "BLE_PIN", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID SYSTEM_UPTIME_MS = new PID(119, "SYSTEM_UPTIME_MS", new Formatter(FORMAT.UINT48, "{0:0.###} sec", 0.001), 0);

		public static readonly PID ETH_ADAPTER_MAC = new PID(120, "ETH_ADAPTER_MAC", new Formatter(FORMAT.UINT48, "${0:X12}"), 1);

		public static readonly PID ETH_BYTES_TX = new PID(121, "ETH_BYTES_TX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_BYTES_RX = new PID(122, "ETH_BYTES_RX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_MESSAGES_TX = new PID(123, "ETH_MESSAGES_TX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_MESSAGES_RX = new PID(124, "ETH_MESSAGES_RX", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_TX_BUFFER_OVERFLOW_COUNT = new PID(125, "ETH_TX_BUFFER_OVERFLOW_COUNT", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_RX_BUFFER_OVERFLOW_COUNT = new PID(126, "ETH_RX_BUFFER_OVERFLOW_COUNT", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_PACKETS_TX_DISCARDED = new PID(127, "ETH_PACKETS_TX_DISCARDED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_PACKETS_RX_DISCARDED = new PID(128, "ETH_PACKETS_RX_DISCARDED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_PACKETS_TX_ERROR = new PID(129, "ETH_PACKETS_TX_ERROR", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_PACKETS_RX_ERROR = new PID(130, "ETH_PACKETS_RX_ERROR", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_PACKETS_TX_OVERFLOW = new PID(131, "ETH_PACKETS_TX_OVERFLOW", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_PACKETS_TX_LATE_COLLISION = new PID(132, "ETH_PACKETS_TX_LATE_COLLISION", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_PACKETS_TX_EXCESS_COLLISION = new PID(133, "ETH_PACKETS_TX_EXCESS_COLLISION", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_PACKETS_TX_UNDERFLOW = new PID(134, "ETH_PACKETS_TX_UNDERFLOW", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_PACKETS_RX_ALIGN_ERR = new PID(135, "ETH_PACKETS_RX_ALIGN_ERR", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_PACKETS_RX_CRC_ERR = new PID(136, "ETH_PACKETS_RX_CRC_ERR", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_PACKETS_RX_TRUNC_ERR = new PID(137, "ETH_PACKETS_RX_TRUNC_ERR", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_PACKETS_RX_LEN_ERR = new PID(138, "ETH_PACKETS_RX_LEN_ERR", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID ETH_PACKETS_RX_COLLISION = new PID(139, "ETH_PACKETS_RX_COLLISION", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID IP_ADDRESS = new PID(140, "IP_ADDRESS", new Formatter(FORMAT.IPV6), 1);

		public static readonly PID IP_SUBNETMASK = new PID(141, "IP_SUBNETMASK", new Formatter(FORMAT.IPV6), 1);

		public static readonly PID IP_GATEWAY = new PID(142, "IP_GATEWAY", new Formatter(FORMAT.IPV6), 1);

		public static readonly PID TCP_NUM_CONNECTIONS = new PID(143, "TCP_NUM_CONNECTIONS", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID AUX_BATTERY_VOLTAGE = new PID(144, "AUX_BATTERY_VOLTAGE", new Formatter(FORMAT.UINT32, "{0:0.###} V", 1.52587890625E-05), 0);

		public static readonly PID RGB_LIGHTING_GANG_ENABLE = new PID(145, "RGB_LIGHTING_GANG_ENABLE", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID INPUT_SWITCH_TYPE = new PID(146, "INPUT_SWITCH_TYPE", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID DOOR_LOCK_STATE = new PID(147, "DOOR_LOCK_STATE", new Formatter(FORMAT.UINT8, "${0:X2}"), 0);

		public static readonly PID GENERATOR_QUIET_HOURS_START_TIME = new PID(148, "GENERATOR_QUIET_HOURS_START_TIME", new Formatter(FORMAT.UINT16, "{0:0.###} hrs", 1.0 / 60.0), 2);

		public static readonly PID GENERATOR_QUIET_HOURS_END_TIME = new PID(149, "GENERATOR_QUIET_HOURS_END_TIME", new Formatter(FORMAT.UINT16, "{0:0.###} hrs", 1.0 / 60.0), 2);

		public static readonly PID GENERATOR_AUTO_START_LOW_VOLTAGE = new PID(150, "GENERATOR_AUTO_START_LOW_VOLTAGE", new Formatter(FORMAT.UINT32, "{0:0.###} V", 1.52587890625E-05), 2);

		public static readonly PID GENERATOR_AUTO_START_HI_TEMP_C = new PID(151, "GENERATOR_AUTO_START_HI_TEMP_C", new Formatter(FORMAT.INT32, "{0:0.###} °C", 1.52587890625E-05), 2);

		public static readonly PID GENERATOR_AUTO_RUN_DURATION_MINUTES = new PID(152, "GENERATOR_AUTO_RUN_DURATION_MINUTES", new Formatter(FORMAT.UINT16, "{0:#,###0} minutes"), 2);

		public static readonly PID GENERATOR_AUTO_RUN_MIN_OFF_TIME_MINUTES = new PID(153, "GENERATOR_AUTO_RUN_MIN_OFF_TIME_MINUTES", new Formatter(FORMAT.UINT16, "{0:#,###0} minutes"), 2);

		public static readonly PID SOFTWARE_BUILD_DATE_TIME = new PID(154, "SOFTWARE_BUILD_DATE_TIME", new Formatter(FORMAT.DATE_TIME), 0);

		public static readonly PID GENERATOR_QUIET_HOURS_ENABLED = new PID(155, "GENERATOR_QUIET_HOURS_ENABLED", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID SHORE_POWER_AMP_RATING = new PID(156, "SHORE_POWER_AMP_RATING", new Formatter(FORMAT.UINT32, "{0:0.###} A", 1.52587890625E-05), 2);

		public static readonly PID BATTERY_CAPACITY_AMP_HOURS = new PID(157, "BATTERY_CAPACITY_AMP_HOURS", new Formatter(FORMAT.UINT32, "{0:0.###} Amp-Hours", 1.52587890625E-05), 2);

		public static readonly PID PCB_ASSEMBLY_PART_NUMBER = new PID(158, "PCB_ASSEMBLY_PART_NUMBER", new Formatter(FORMAT.UINT48, "${0:X12}"), 1);

		public static readonly PID UNLOCK_PIN = new PID(159, "UNLOCK_PIN", new Formatter(FORMAT.UINT48), 2);

		public static readonly PID UNLOCK_PIN_MODE = new PID(160, "UNLOCK_PIN_MODE", new Formatter(FORMAT.UINT48), 2);

		public static readonly PID SIMULATE_ON_OFF_STYLE_LIGHT = new PID(161, "SIMULATE_ON_OFF_STYLE_LIGHT", new Formatter(FORMAT.UINT48), 2);

		public static readonly PID FAN_SPEED_CONTROL_TYPE = new PID(162, "FAN_SPEED_CONTROL_TYPE", new Formatter(FORMAT.UINT48), 2);

		public static readonly PID HVAC_CONTROL_TYPE = new PID(163, "HVAC_CONTROL_TYPE", new Formatter(FORMAT.UINT48), 2);

		public static readonly PID SOFTWARE_FUSE_RATING_AMPS = new PID(164, "SOFTWARE_FUSE_RATING_AMPS", new Formatter(FORMAT.UINT32, "{0:0.###} A", 1.52587890625E-05), 2);

		public static readonly PID SOFTWARE_FUSE_MAX_RATING_AMPS = new PID(165, "SOFTWARE_FUSE_MAX_RATING_AMPS", new Formatter(FORMAT.UINT32, "{0:0.###} A", 1.52587890625E-05), 2);

		public static readonly PID CUMMINS_ONAN_GENERATOR_FAULT_CODE = new PID(166, "CUMMINS_ONAN_GENERATOR_FAULT_CODE", new Formatter(FORMAT.UINT48), 2);

		public static readonly PID MOTOR_1_CURRENT_AMPS = new PID(167, "MOTOR_1_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_2_CURRENT_AMPS = new PID(168, "MOTOR_2_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_3_CURRENT_AMPS = new PID(169, "MOTOR_3_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_4_CURRENT_AMPS = new PID(170, "MOTOR_4_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_5_CURRENT_AMPS = new PID(171, "MOTOR_5_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_6_CURRENT_AMPS = new PID(172, "MOTOR_6_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_7_CURRENT_AMPS = new PID(173, "MOTOR_7_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_8_CURRENT_AMPS = new PID(174, "MOTOR_8_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_9_CURRENT_AMPS = new PID(175, "MOTOR_9_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_10_CURRENT_AMPS = new PID(176, "MOTOR_10_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_11_CURRENT_AMPS = new PID(177, "MOTOR_11_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_12_CURRENT_AMPS = new PID(178, "MOTOR_12_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_13_CURRENT_AMPS = new PID(179, "MOTOR_13_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_14_CURRENT_AMPS = new PID(180, "MOTOR_14_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_15_CURRENT_AMPS = new PID(181, "MOTOR_15_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID MOTOR_16_CURRENT_AMPS = new PID(182, "MOTOR_16_CURRENT_AMPS", new Formatter(FORMAT.INT32, "{0:0.###} A", 1.52587890625E-05), 0);

		public static readonly PID DEVICE_TYPE = new PID(183, "DEVICE_TYPE", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID IN_MOTION_LOCKOUT_BEHAVIOR = new PID(184, "IN_MOTION_LOCKOUT_BEHAVIOR", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID RVC_DETECTED_NODES = new PID(185, "RVC_DETECTED_NODES", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_LOST_NODES = new PID(186, "RVC_LOST_NODES", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_BYTES_TX = new PID(187, "RVC_BYTES_TX", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_BYTES_RX = new PID(188, "RVC_BYTES_RX", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_MESSAGES_TX = new PID(189, "RVC_MESSAGES_TX", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_MESSAGES_RX = new PID(190, "RVC_MESSAGES_RX", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_TX_BUFFERS_FREE = new PID(191, "RVC_TX_BUFFERS_FREE", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_TX_BUFFERS_USED = new PID(192, "RVC_TX_BUFFERS_USED", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_RX_BUFFERS_FREE = new PID(193, "RVC_RX_BUFFERS_FREE", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_RX_BUFFERS_USED = new PID(194, "RVC_RX_BUFFERS_USED", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_TX_OUT_OF_BUFFERS_COUNT = new PID(195, "RVC_TX_OUT_OF_BUFFERS_COUNT", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_RX_OUT_OF_BUFFERS_COUNT = new PID(196, "RVC_RX_OUT_OF_BUFFERS_COUNT", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_TX_FAILURE_COUNT = new PID(197, "RVC_TX_FAILURE_COUNT", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_DEFAULT_SRC_ADDR = new PID(198, "RVC_DEFAULT_SRC_ADDR", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_DYNAMIC_ADDR = new PID(199, "RVC_DYNAMIC_ADDR", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID RVC_MAKE = new PID(200, "RVC_MAKE", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID RVC_MODEL_1 = new PID(201, "RVC_MODEL_1", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID RVC_MODEL_2 = new PID(202, "RVC_MODEL_2", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID RVC_MODEL_3 = new PID(203, "RVC_MODEL_3", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID RVC_SERIAL = new PID(204, "RVC_SERIAL", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID RVC_ID_NUMBER = new PID(205, "RVC_ID_NUMBER", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID CLOUD_GATEWAY_ASSET_ID_PART_1 = new PID(206, "CLOUD_GATEWAY_ASSET_ID_PART_1", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID CLOUD_GATEWAY_ASSET_ID_PART_2 = new PID(207, "CLOUD_GATEWAY_ASSET_ID_PART_2", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID CLOUD_GATEWAY_ASSET_ID_PART_3 = new PID(208, "CLOUD_GATEWAY_ASSET_ID_PART_3", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID HVAC_ZONE_CAPABILITIES = new PID(209, "HVAC_ZONE_CAPABILITIES", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID IGNITION_BEHAVIOR = new PID(210, "IGNITION_BEHAVIOR", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID BLE_NUMBER_OF_FORWARDED_CAN_DEVICES = new PID(211, "BLE_NUMBER_OF_FORWARDED_CAN_DEVICES", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID BLE_NUMBER_OF_CONNECTS = new PID(212, "BLE_NUMBER_OF_CONNECTS", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_NUMBER_OF_DISCONNECTS = new PID(213, "BLE_NUMBER_OF_DISCONNECTS", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_TOTAL_TRAFFIC = new PID(214, "BLE_TOTAL_TRAFFIC", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_WRITES_FROM_PHONE = new PID(215, "BLE_WRITES_FROM_PHONE", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_NOTIFICATIONS_TO_PHONE_SUCCESSFUL = new PID(216, "BLE_NOTIFICATIONS_TO_PHONE_SUCCESSFUL", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_NOTIFICATIONS_TO_PHONE_FAILURE = new PID(217, "BLE_NOTIFICATIONS_TO_PHONE_FAILURE", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_MTU_SIZE_CENTRAL = new PID(218, "BLE_MTU_SIZE_CENTRAL", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_MTU_SIZE_PERIPHERAL = new PID(219, "BLE_MTU_SIZE_PERIPHERAL", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_DATA_LENGTH_TIME = new PID(220, "BLE_DATA_LENGTH_TIME", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_SECURITY_UNLOCKED = new PID(221, "BLE_SECURITY_UNLOCKED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_CLIENT_CONNECTED = new PID(222, "BLE_CLIENT_CONNECTED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_CCCD_WRITTEN = new PID(223, "BLE_CCCD_WRITTEN", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_NUM_BUFFERS_FREE = new PID(224, "BLE_NUM_BUFFERS_FREE", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_LAST_TX_ERROR = new PID(225, "BLE_LAST_TX_ERROR", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_CONNECTED_DEVICE_RSSI = new PID(226, "BLE_CONNECTED_DEVICE_RSSI", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_DEAD_CLIENT_COUNTER = new PID(227, "BLE_DEAD_CLIENT_COUNTER", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_LAST_DISCONNECT_REASON = new PID(228, "BLE_LAST_DISCONNECT_REASON", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_SPI_RX_MSGS_DROPPED = new PID(229, "BLE_SPI_RX_MSGS_DROPPED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID BLE_SPI_TX_MSGS_DROPPED = new PID(230, "BLE_SPI_TX_MSGS_DROPPED", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID LOW_VOLTAGE_BEHAVIOR = new PID(231, "LOW_VOLTAGE_BEHAVIOR", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID DHCP_ENABLED = new PID(232, "DHCP_ENABLED", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID UDP_DEVICE_NAME_1 = new PID(233, "UDP_DEVICE_NAME_1", new Formatter(FORMAT.CHAR6), 2);

		public static readonly PID UDP_DEVICE_NAME_2 = new PID(234, "UDP_DEVICE_NAME_2", new Formatter(FORMAT.CHAR6), 2);

		public static readonly PID UDP_DEVICE_NAME_3 = new PID(235, "UDP_DEVICE_NAME_3", new Formatter(FORMAT.CHAR6), 2);

		public static readonly PID TCP_BATCH_SIZE = new PID(236, "TCP_BATCH_SIZE", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID TCP_BATCH_TIME = new PID(237, "TCP_BATCH_TIME", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 2);

		public static readonly PID ON_OFF_INPUT_PIN = new PID(238, "ON_OFF_INPUT_PIN", new Formatter(FORMAT.UINT48), 2);

		public static readonly PID EXTEND_INPUT_PIN = new PID(239, "EXTEND_INPUT_PIN", new Formatter(FORMAT.UINT48), 2);

		public static readonly PID RETRACT_INPUT_PIN = new PID(240, "RETRACT_INPUT_PIN", new Formatter(FORMAT.UINT48), 2);

		public static readonly PID INPUT_PIN_COUNT = new PID(241, "INPUT_PIN_COUNT", new Formatter(FORMAT.UINT48, "{0:#,###0}"), 0);

		public static readonly PID DSI_FAULT_INPUT_PIN = new PID(242, "DSI_FAULT_INPUT_PIN", new Formatter(FORMAT.UINT48), 2);

		public static readonly PID DEVICE_ACTIVATION_TIMEOUT = new PID(243, "DEVICE_ACTIVATION_TIMEOUT", new Formatter(FORMAT.UINT48), 2);

		public static readonly PID LEVELER_UI_SUPPORTED_FEATURES = new PID(244, "LEVELER_UI_SUPPORTED_FEATURES", new Formatter(FORMAT.UINT32, "${0:X8}"), 0);

		public static readonly PID LEVELER_SENSOR_TOPOLOGY = new PID(245, "LEVELER_SENSOR_TOPOLOGY", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID LEVELER_DRIVE_TYPE = new PID(246, "LEVELER_DRIVE_TYPE", new Formatter(FORMAT.UINT48), 0);

		public static readonly PID LEVELER_AUTO_MODE_PROGRESS = new PID(247, "LEVELER_AUTO_MODE_PROGRESS", new Formatter(FORMAT.UINT32, "${0:X8}"), 0);

		public static readonly PID LEFT_FRONT_JACK_STROKE_INCHES = new PID(248, "LEFT_FRONT_JACK_STROKE_INCHES", new Formatter(FORMAT.INT32, "{0:0.###}\"", 1.52587890625E-05), 0);

		public static readonly PID RIGHT_FRONT_JACK_STROKE_INCHES = new PID(249, "RIGHT_FRONT_JACK_STROKE_INCHES", new Formatter(FORMAT.INT32, "{0:0.###}\"", 1.52587890625E-05), 0);

		public static readonly PID LEFT_MIDDLE_JACK_STROKE_INCHES = new PID(250, "LEFT_MIDDLE_JACK_STROKE_INCHES", new Formatter(FORMAT.INT32, "{0:0.###}\"", 1.52587890625E-05), 0);

		public static readonly PID RIGHT_MIDDLE_JACK_STROKE_INCHES = new PID(251, "RIGHT_MIDDLE_JACK_STROKE_INCHES", new Formatter(FORMAT.INT32, "{0:0.###}\"", 1.52587890625E-05), 0);

		public static readonly PID LEFT_REAR_JACK_STROKE_INCHES = new PID(252, "LEFT_REAR_JACK_STROKE_INCHES", new Formatter(FORMAT.INT32, "{0:0.###}\"", 1.52587890625E-05), 0);

		public static readonly PID RIGHT_REAR_JACK_STROKE_INCHES = new PID(253, "RIGHT_REAR_JACK_STROKE_INCHES", new Formatter(FORMAT.INT32, "{0:0.###}\"", 1.52587890625E-05), 0);

		public static readonly PID LEFT_FRONT_JACK_MAX_STROKE_INCHES = new PID(254, "LEFT_FRONT_JACK_MAX_STROKE_INCHES", new Formatter(FORMAT.UINT32, "{0:0.###}\"", 1.52587890625E-05), 0);

		public static readonly PID RIGHT_FRONT_JACK_MAX_STROKE_INCHES = new PID(255, "RIGHT_FRONT_JACK_MAX_STROKE_INCHES", new Formatter(FORMAT.UINT32, "{0:0.###}\"", 1.52587890625E-05), 0);

		public static readonly PID LEFT_MIDDLE_JACK_MAX_STROKE_INCHES = new PID(256, "LEFT_MIDDLE_JACK_MAX_STROKE_INCHES", new Formatter(FORMAT.UINT32, "{0:0.###}\"", 1.52587890625E-05), 0);

		public static readonly PID RIGHT_MIDDLE_JACK_MAX_STROKE_INCHES = new PID(257, "RIGHT_MIDDLE_JACK_MAX_STROKE_INCHES", new Formatter(FORMAT.UINT32, "{0:0.###}\"", 1.52587890625E-05), 0);

		public static readonly PID LEFT_REAR_JACK_MAX_STROKE_INCHES = new PID(258, "LEFT_REAR_JACK_MAX_STROKE_INCHES", new Formatter(FORMAT.UINT32, "{0:0.###}\"", 1.52587890625E-05), 0);

		public static readonly PID RIGHT_REAR_JACK_MAX_STROKE_INCHES = new PID(259, "RIGHT_REAR_JACK_MAX_STROKE_INCHES", new Formatter(FORMAT.UINT32, "{0:0.###}\"", 1.52587890625E-05), 0);

		public static readonly PID PARKBRAKE_BEHAVIOR = new PID(260, "PARKBRAKE_BEHAVIOR", new Formatter(FORMAT.UINT48), 2);

		public static readonly PID EXTENDED_DEVICE_CAPABILITIES = new PID(261, "EXTENDED_DEVICE_CAPABILITIES", new Formatter(FORMAT.UINT48, "${0:X12}"), 2);

		public static readonly PID CLOUD_CAPABILITIES = new PID(262, "CLOUD_CAPABILITIES", new Formatter(FORMAT.UINT48, "${0:X12}"), 2);

		public static readonly PID RV_MAKE_ID = new PID(263, "RV_MAKE_ID", new Formatter(FORMAT.UINT32, "${0:X8}"), 2);

		public static readonly PID RV_MODEL_ID = new PID(264, "RV_MODEL_ID", new Formatter(FORMAT.UINT32, "${0:X8}"), 2);

		public static readonly PID RV_YEAR = new PID(265, "RV_YEAR", new Formatter(FORMAT.UINT16, "${0:X4}"), 2);

		public static readonly PID RV_FLOORPLAN_ID = new PID(266, "RV_FLOORPLAN_ID", new Formatter(FORMAT.UINT32, "${0:X8}"), 2);

		public static readonly PID FLOORPLAN_PART_NUM = new PID(267, "FLOORPLAN_PART_NUM", new Formatter(FORMAT.CHAR6), 2);

		public static readonly PID FLOORPLAN_WRITTEN_BY = new PID(268, "FLOORPLAN_WRITTEN_BY", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID ASSEMBLY_PART_NUM = new PID(269, "ASSEMBLY_PART_NUM", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID ASSEMBLY_DATE_CODE = new PID(270, "ASSEMBLY_DATE_CODE", new Formatter(FORMAT.CHAR5), 1);

		public static readonly PID ASSEMBLY_SERIAL_NUM = new PID(271, "ASSEMBLY_SERIAL_NUM", new Formatter(FORMAT.CHAR5), 1);

		public static readonly PID LEVELER_AUTO_PROCESS_STEPS_1 = new PID(272, "LEVELER_AUTO_PROCESS_STEPS_1", new Formatter(FORMAT.MAC6), 0);

		public static readonly PID LEVELER_AUTO_PROCESS_STEPS_2 = new PID(273, "LEVELER_AUTO_PROCESS_STEPS_2", new Formatter(FORMAT.MAC6), 0);

		public static readonly PID LEVELER_AUTO_PROCESS_STEPS_3 = new PID(274, "LEVELER_AUTO_PROCESS_STEPS_3", new Formatter(FORMAT.MAC6), 0);

		public static readonly PID LEVELER_AUTO_PROCESS_STEPS_4 = new PID(275, "LEVELER_AUTO_PROCESS_STEPS_4", new Formatter(FORMAT.MAC6), 0);

		public static readonly PID LEVELER_AUTO_PROCESS_STEPS_5 = new PID(276, "LEVELER_AUTO_PROCESS_STEPS_5", new Formatter(FORMAT.MAC6), 0);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_01 = new PID(277, "MONITOR_PANEL_DEVICE_ID_01", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_02 = new PID(278, "MONITOR_PANEL_DEVICE_ID_02", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_03 = new PID(279, "MONITOR_PANEL_DEVICE_ID_03", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_04 = new PID(280, "MONITOR_PANEL_DEVICE_ID_04", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_05 = new PID(281, "MONITOR_PANEL_DEVICE_ID_05", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_06 = new PID(282, "MONITOR_PANEL_DEVICE_ID_06", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_07 = new PID(283, "MONITOR_PANEL_DEVICE_ID_07", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_08 = new PID(284, "MONITOR_PANEL_DEVICE_ID_08", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_09 = new PID(285, "MONITOR_PANEL_DEVICE_ID_09", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_10 = new PID(286, "MONITOR_PANEL_DEVICE_ID_10", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_11 = new PID(287, "MONITOR_PANEL_DEVICE_ID_11", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_12 = new PID(288, "MONITOR_PANEL_DEVICE_ID_12", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_13 = new PID(289, "MONITOR_PANEL_DEVICE_ID_13", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_14 = new PID(290, "MONITOR_PANEL_DEVICE_ID_14", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_15 = new PID(291, "MONITOR_PANEL_DEVICE_ID_15", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_16 = new PID(292, "MONITOR_PANEL_DEVICE_ID_16", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_17 = new PID(293, "MONITOR_PANEL_DEVICE_ID_17", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_18 = new PID(294, "MONITOR_PANEL_DEVICE_ID_18", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_19 = new PID(295, "MONITOR_PANEL_DEVICE_ID_19", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_20 = new PID(296, "MONITOR_PANEL_DEVICE_ID_20", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_21 = new PID(297, "MONITOR_PANEL_DEVICE_ID_21", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_22 = new PID(298, "MONITOR_PANEL_DEVICE_ID_22", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_23 = new PID(299, "MONITOR_PANEL_DEVICE_ID_23", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_24 = new PID(300, "MONITOR_PANEL_DEVICE_ID_24", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_25 = new PID(301, "MONITOR_PANEL_DEVICE_ID_25", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_26 = new PID(302, "MONITOR_PANEL_DEVICE_ID_26", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_27 = new PID(303, "MONITOR_PANEL_DEVICE_ID_27", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_28 = new PID(304, "MONITOR_PANEL_DEVICE_ID_28", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_29 = new PID(305, "MONITOR_PANEL_DEVICE_ID_29", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_30 = new PID(306, "MONITOR_PANEL_DEVICE_ID_30", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_31 = new PID(307, "MONITOR_PANEL_DEVICE_ID_31", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_32 = new PID(308, "MONITOR_PANEL_DEVICE_ID_32", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_33 = new PID(309, "MONITOR_PANEL_DEVICE_ID_33", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_34 = new PID(310, "MONITOR_PANEL_DEVICE_ID_34", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_35 = new PID(311, "MONITOR_PANEL_DEVICE_ID_35", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_36 = new PID(312, "MONITOR_PANEL_DEVICE_ID_36", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_37 = new PID(313, "MONITOR_PANEL_DEVICE_ID_37", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_38 = new PID(314, "MONITOR_PANEL_DEVICE_ID_38", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_39 = new PID(315, "MONITOR_PANEL_DEVICE_ID_39", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_40 = new PID(316, "MONITOR_PANEL_DEVICE_ID_40", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_41 = new PID(317, "MONITOR_PANEL_DEVICE_ID_41", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_42 = new PID(318, "MONITOR_PANEL_DEVICE_ID_42", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_43 = new PID(319, "MONITOR_PANEL_DEVICE_ID_43", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_44 = new PID(320, "MONITOR_PANEL_DEVICE_ID_44", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_45 = new PID(321, "MONITOR_PANEL_DEVICE_ID_45", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_46 = new PID(322, "MONITOR_PANEL_DEVICE_ID_46", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_47 = new PID(323, "MONITOR_PANEL_DEVICE_ID_47", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_DEVICE_ID_48 = new PID(324, "MONITOR_PANEL_DEVICE_ID_48", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID MONITOR_PANEL_CONTROL_TYPE_MOMENTARY_SWITCH = new PID(325, "MONITOR_PANEL_CONTROL_TYPE_MOMENTARY_SWITCH", new Formatter(FORMAT.UINT8, "${0:X2}"), 0);

		public static readonly PID MONITOR_PANEL_CONTROL_TYPE_LATCHING_SWITCH = new PID(326, "MONITOR_PANEL_CONTROL_TYPE_LATCHING_SWITCH", new Formatter(FORMAT.UINT8, "${0:X2}"), 0);

		public static readonly PID MONITOR_PANEL_CONTROL_TYPE_SUPPLY_TANK = new PID(327, "MONITOR_PANEL_CONTROL_TYPE_SUPPLY_TANK", new Formatter(FORMAT.UINT8, "${0:X2}"), 0);

		public static readonly PID MONITOR_PANEL_CONTROL_TYPE_WASTE_TANK = new PID(328, "MONITOR_PANEL_CONTROL_TYPE_WASTE_TANK", new Formatter(FORMAT.UINT8, "${0:X2}"), 0);

		public static readonly PID MONITOR_PANEL_CONFIGURATION = new PID(329, "MONITOR_PANEL_CONFIGURATION", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID BLE_PAIRING_MODE = new PID(330, "BLE_PAIRING_MODE", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID MONITOR_PANEL_CALIBRATION_PART_NBR = new PID(331, "MONITOR_PANEL_CALIBRATION_PART_NBR", new Formatter(FORMAT.CHAR6), 0);

		public static readonly PID READ_ADDRESS16BITS_DATA32BITS = new PID(332, "READ_ADDRESS16BITS_DATA32BITS", new Formatter(FORMAT.ADDRESS16_DATA32), 0);

		public static readonly PID WRITE_ADDRESS16BITS_DATA32BITS = new PID(333, "WRITE_ADDRESS16BITS_DATA32BITS", new Formatter(FORMAT.ADDRESS16_DATA32), 3);

		public static readonly PID TEMP_SENSOR_HIGH_TEMP_ALERT = new PID(334, "TEMP_SENSOR_HIGH_TEMP_ALERT ", new Formatter(FORMAT.INT16, "{0:0.#}°", 1.0 / 256.0), 2);

		public static readonly PID TEMP_SENSOR_LOW_TEMP_ALERT = new PID(335, "TEMP_SENSOR_LOW_TEMP_ALERT ", new Formatter(FORMAT.INT16, "{0:0.#}°", 1.0 / 256.0), 2);

		public static readonly PID ACC_GW_ADD_DEVICE_MAC = new PID(336, "ACC_GW_ADD_DEVICE_MAC", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID ACC_GW_WRITE_DEVICE_SW_NUM = new PID(337, "ACC_GW_WRITE_DEVICE_SW_NUM ", new Formatter(FORMAT.CHAR6), 2);

		public static readonly PID VEHICLE_CONFIGURATION = new PID(338, "VEHICLE_CONFIGURATION", new Formatter(FORMAT.UINT32, "${0:X8}"), 2);

		public static readonly PID TPMS_SENSOR_POSITION = new PID(339, "TPMS_SENSOR_POSITION", new Formatter(FORMAT.ADDRESS16_DATA32), 3);

		public static readonly PID TPMS_SENSOR_PRESURE_FAULT_LIMITS = new PID(340, "TPMS_SENSOR_PRESURE_FAULT_LIMITS", new Formatter(FORMAT.ADDRESS16_DATA32), 3);

		public static readonly PID TPMS_SENSOR_TEMPERATURE_FAULT_LIMITS = new PID(341, "TPMS_SENSOR_TEMPERATURE_FAULT_LIMITS", new Formatter(FORMAT.ADDRESS16_DATA32), 3);

		public static readonly PID TPMS_SENSOR_ID = new PID(342, "TPMS_SENSOR_ID", new Formatter(FORMAT.ADDRESS16_DATA32), 0);

		public static readonly PID SMART_ARM_WIND_EVENT_SETTING = new PID(343, "SMART_ARM_WIND_EVENT_SETTING", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID ACC_REQUEST_MODE = new PID(344, "ACC_REQUEST_MODE", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID ACCESSORY_SETTING_01 = new PID(345, "ACCESSORY_SETTING_01", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_02 = new PID(346, "ACCESSORY_SETTING_02", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_03 = new PID(347, "ACCESSORY_SETTING_03", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_04 = new PID(348, "ACCESSORY_SETTING_04", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_05 = new PID(349, "ACCESSORY_SETTING_05", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_06 = new PID(350, "ACCESSORY_SETTING_06", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_07 = new PID(351, "ACCESSORY_SETTING_07", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_08 = new PID(352, "ACCESSORY_SETTING_08", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_09 = new PID(353, "ACCESSORY_SETTING_09", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_10 = new PID(354, "ACCESSORY_SETTING_10", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_11 = new PID(355, "ACCESSORY_SETTING_11", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_12 = new PID(356, "ACCESSORY_SETTING_12", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_13 = new PID(357, "ACCESSORY_SETTING_13", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_14 = new PID(358, "ACCESSORY_SETTING_14", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_15 = new PID(359, "ACCESSORY_SETTING_15", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ACCESSORY_SETTING_16 = new PID(360, "ACCESSORY_SETTING_16", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID TIRE_TRACK_WIDTH = new PID(361, "TIRE_TRACK_WIDTH", new Formatter(FORMAT.UINT32, "{0:0.###}\"", 1.52587890625E-05), 1);

		public static readonly PID TIRE_DIAMETER = new PID(362, "TIRE_DIAMETER", new Formatter(FORMAT.UINT32, "{0:0.###}\"", 1.52587890625E-05), 1);

		public static readonly PID ABS_RIM_TEETH_COUNT = new PID(363, "ABS_RIM_TEETH_COUNT", new Formatter(FORMAT.UINT8, "${0:X2}"), 1);

		public static readonly PID ABS_MAINTENANCE_PERIOD = new PID(364, "ABS_MAINTENANCE_PERIOD", new Formatter(FORMAT.UINT32), 1);

		public static readonly PID ILLUMINATION_SYNC = new PID(365, "ILLUMINATION_SYNC", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID RV_C_INSTANCE = new PID(366, "RV_C_INSTANCE", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID HVAC_CONTROL_TYPE_SETTING = new PID(367, "HVAC_CONTROL_TYPE_SETTING", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID ACTIVE_HVAC_CONTROL_TYPE = new PID(368, "ACTIVE_HVAC_CONTROL_TYPE", new Formatter(FORMAT.UINT8, "${0:X2}"), 0);

		public static readonly PID MONITOR_PANEL_CONTROL_TYPE_CONFIG_TANK = new PID(369, "MONITOR_PANEL_CONTROL_TYPE_CONFIG_TANK", new Formatter(FORMAT.UINT8, "${0:X2}"), 0);

		public static readonly PID NUMBER_OF_AXLES = new PID(370, "NUMBER_OF_AXLES", new Formatter(FORMAT.UINT8, "${0:X2}"), 1);

		public static readonly PID LAST_MAINTENANCE_ODOMETER = new PID(371, "LAST_MAINTENANCE_ODOMETER", new Formatter(FORMAT.UINT32), 2);

		public static readonly PID ACC_GW_NUM_DEVICES = new PID(372, "ACC_GW_NUM_DEVICES", new Formatter(FORMAT.UINT8), 0);

		public static readonly PID ACC_GW_MAC_HIGH = new PID(373, "ACC_GW_MAC_HIGH", new Formatter(FORMAT.ADDRESS16_DATA32), 0);

		public static readonly PID ACC_GW_MAC_LOW = new PID(374, "ACC_GW_MAC_LOW", new Formatter(FORMAT.ADDRESS16_DATA32), 0);

		public static readonly PID DEVICE_TYPE_AT_INDEX = new PID(375, "DEVICE_TYPE_AT_INDEX", new Formatter(FORMAT.ADDRESS16_DATA32), 0);

		public static readonly PID BRAKE_MODULE_ORIENTATION = new PID(376, "BRAKE_MODULE_ORIENTATION", new Formatter(FORMAT.UINT8), 1);

		public static readonly PID CORE_MICROCONTOLLER_RESET = new PID(377, "CORE_MICROCONTOLLER_RESET", new Formatter(FORMAT.CHAR6), 3);

		public static readonly PID PRODUCT_FW_PART_NUM = new PID(378, "PRODUCT_FW_PART_NUM", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID CORE_VERSION_INFO = new PID(379, "CORE_VERSION_INFO", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID PRODUCT_ID_NUM = new PID(380, "PRODUCT_ID_NUM", new Formatter(FORMAT.UINT16, "${0:X4}"), 1);

		public static readonly PID PRODUCT_ID_IN_CONFIG_BLOCK = new PID(381, "PRODUCT_ID_IN_CONFIG_BLOCK", new Formatter(FORMAT.UINT16, "${0:X4}"), 1);

		public static readonly PID LOCAP_VERSION_INFO = new PID(382, "LOCAP_VERSION_INFO", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID PRODUCT_FW_PART_NUM1 = new PID(383, "PRODUCT_FW_PART_NUM1", new Formatter(FORMAT.CHAR6), 1);

		public static readonly PID PRODUCT_FW_PART_NUM2 = new PID(384, "PRODUCT_FW_PART_NUM2", new Formatter(FORMAT.CHAR2), 1);

		public static readonly PID HBRIDGE_SAFETY_ALERT_CONFIG = new PID(385, "HBRIDGE_SAFETY_ALERT_CONFIG", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID AWNING_AUTO_PROTECTION_COUNT = new PID(386, "AWNING_AUTO_PROTECTION_COUNT", new Formatter(FORMAT.UINT16), 2);

		public static readonly PID MOMENTARY_HBRIDGE_CIRCUIT_ROLE = new PID(387, "MOMENTARY_HBRIDGE_CIRCUIT_ROLE", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID SOUNDS_HIGHEST_CAPABLE = new PID(388, "SOUNDS_HIGHEST_CAPABLE", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID SMART_ARM_VALANCE_CORRECTION = new PID(389, "SMART_ARM_VALANCE_CORRECTION", new Formatter(FORMAT.INT8), 2);

		public static readonly PID JUMP_TO_BOOT = new PID(390, "JUMP_TO_BOOT", new Formatter(FORMAT.BYTE6), 3);

		public static readonly PID OPTIONAL_CAPABILITIES_SUPPORTED = new PID(391, "OPTIONAL_CAPABILITIES_SUPPORTED", new Formatter(FORMAT.UINT8, "${0:X2}"), 0);

		public static readonly PID OPTIONAL_CAPABILITIES_ENABLED = new PID(392, "OPTIONAL_CAPABILITIES_ENABLED", new Formatter(FORMAT.UINT8, "${0:X2}"), 3);

		public static readonly PID OPTIONAL_CAPABILITIES_MANDATORY = new PID(393, "OPTIONAL_CAPABILITIES_MANDATORY", new Formatter(FORMAT.UINT8, "${0:X2}"), 0);

		public static readonly PID ABS_MODEL_VERSION = new PID(394, "ABS_MODEL_VERSION", new Formatter(FORMAT.UINT16), 0);

		public static readonly PID LOCKOUT_DISABLES_SWITCH_INPUT = new PID(395, "LOCKOUT_DISABLES_SWITCH_INPUT", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID TANK_SENSOR_TYPE = new PID(396, "TANK_SENSOR_TYPE", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID TANK_SENSOR_CALIBRATION_MULTIPLIER = new PID(397, "TANK_SENSOR_CALIBRATION_MULTIPLIER", new Formatter(FORMAT.UINT8, "{0:###0}"), 2);

		public static readonly PID TANK_SENSOR_CALIBRATION_1 = new PID(398, "TANK_SENSOR_CALIBRATION_1", new Formatter(FORMAT.UINT40, "${0:X10}"), 2);

		public static readonly PID TANK_SENSOR_CALIBRATION_2 = new PID(399, "TANK_SENSOR_CALIBRATION_2", new Formatter(FORMAT.UINT40, "${0:X10}"), 2);

		public static readonly PID TANK_SENSOR_CALIBRATION_3 = new PID(400, "TANK_SENSOR_CALIBRATION_3", new Formatter(FORMAT.UINT40, "${0:X10}"), 2);

		public static readonly PID TANK_SENSOR_CALIBRATION_4 = new PID(401, "TANK_SENSOR_CALIBRATION_4", new Formatter(FORMAT.UINT40, "${0:X10}"), 2);

		public static readonly PID ABS_AUTO_CONFIG_STATUS = new PID(402, "ABS_AUTO_CONFIG_STATUS", new Formatter(FORMAT.UINT8, "${0:X2}"), 0);

		public static readonly PID OPTIONAL_CAPABILITIES_USER_DISABLED = new PID(403, "OPTIONAL_CAPABILITIES_USER_DISABLED", new Formatter(FORMAT.UINT8, "${0:X2}"), 2);

		public static readonly PID GENERATOR_TYPE = new PID(404, "GENERATOR_TYPE", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID CONFIG_BUILD_DATE_TIME = new PID(405, "CONFIG_BUILD_DATE_TIME", new Formatter(FORMAT.DATE_TIME), 0);

		public static readonly PID TANK_CAPACITY_GALLONS = new PID(406, "TANK_CAPACITY_GALLONS", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID LEVELER_SET_POINT_NAMES = new PID(407, "LEVELER_SET_POINT_NAMES", new Formatter(FORMAT.MAC6), 2);

		public static readonly PID CURRENT_STALL_THRESHOLD = new PID(408, "CURRENT_STALL_THRESHOLD", new Formatter(FORMAT.UINT48, "{0:0.###} A", 1.52587890625E-05), 2);

		public static readonly PID CURRENT_STALL_DEBOUNCE = new PID(409, "CURRENT_STALL_DEBOUNCE", new Formatter(FORMAT.UINT48, "{0:#,###0} ms"), 2);

		public static readonly PID SPEED_STALL_THRESHOLD = new PID(410, "SPEED_STALL_THRESHOLD", new Formatter(FORMAT.UINT48, "{0:#,###0} Hz"), 2);

		public static readonly PID SPEED_STALL_DEBOUNCE = new PID(411, "SPEED_STALL_DEBOUNCE", new Formatter(FORMAT.UINT48, "{0:#,###0} ms"), 2);

		public static readonly PID MOTOR_1_POSITION = new PID(412, "MOTOR_1_POSITION", new Formatter(FORMAT.INT32), 2);

		public static readonly PID MOTOR_2_POSITION = new PID(413, "MOTOR_2_POSITION", new Formatter(FORMAT.INT32), 2);

		public static readonly PID MOTOR_1_DISTANCE_TRAVELLED = new PID(414, "MOTOR_1_DISTANCE_TRAVELLED", new Formatter(FORMAT.INT32), 2);

		public static readonly PID MOTOR_2_DISTANCE_TRAVELLED = new PID(415, "MOTOR_2_DISTANCE_TRAVELLED", new Formatter(FORMAT.INT32), 2);

		public static readonly PID MOTOR_2_DISTANCE_TRAVELLED_UNCORRECTED = new PID(416, "MOTOR_2_DISTANCE_TRAVELLED_UNCORRECTED", new Formatter(FORMAT.INT32), 2);

		public static readonly PID MOTOR_1_RESISTANCE = new PID(417, "MOTOR_1_RESISTANCE", new Formatter(FORMAT.UINT48, "{0:0.###} Ohms", 1.52587890625E-05), 2);

		public static readonly PID MOTOR_2_RESISTANCE = new PID(418, "MOTOR_2_RESISTANCE", new Formatter(FORMAT.UINT48, "{0:0.###} Ohms", 1.52587890625E-05), 2);

		public static readonly PID POSITION_FEEDBACK_TYPE = new PID(419, "POSITION_FEEDBACK_TYPE", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID GENERIC_INDEXABLE_1 = new PID(420, "GENERIC_INDEXABLE_1", new Formatter(FORMAT.ADDRESS16_DATA32), 2);

		public static readonly PID GENERIC_INDEXABLE_2 = new PID(421, "GENERIC_INDEXABLE_2", new Formatter(FORMAT.ADDRESS16_DATA32), 2);

		public static readonly PID GENERIC_INDEXABLE_3 = new PID(422, "GENERIC_INDEXABLE_3", new Formatter(FORMAT.ADDRESS16_DATA32), 2);

		public static readonly PID GENERIC_INDEXABLE_4 = new PID(423, "GENERIC_INDEXABLE_4", new Formatter(FORMAT.ADDRESS16_DATA32), 2);

		public static readonly PID IDSCAN_VERSION_INFO = new PID(424, "IDSCAN_VERSION_INFO", new Formatter(FORMAT.BYTE6), 2);

		public static readonly PID ODOMETER = new PID(425, "ODOMETER", new Formatter(FORMAT.UINT32), 2);

		public static readonly PID ODOMETER_RESET_COUNT = new PID(426, "ODOMETER_RESET_COUNT", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID SOFTWARE_FUSE_MAX_RATING_MILLIAMPS = new PID(427, "SOFTWARE_FUSE_MAX_RATING_MILLIAMPS", new Formatter(FORMAT.UINT32), 2);

		public static readonly PID SOFTWARE_FUSE_RATING_MILLIAMPS = new PID(428, "SOFTWARE_FUSE_RATING_MILLIAMPS", new Formatter(FORMAT.UINT32), 2);

		public static readonly PID INVERT_HBRIDGE_BEHAVIOR = new PID(429, "INVERT_HBRIDGE_BEHAVIOR", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID MAINTAIN_STATE_THROUGH_POWER_CYCLE = new PID(430, "MAINTAIN_STATE_THROUGH_POWER_CYCLE", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID SET_ALL_LIGHTS_GROUP_BEHAVIOR = new PID(431, "SET_ALL_LIGHTS_GROUP_BEHAVIOR", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID CRITICAL_TANK_STATE = new PID(432, "CRITICAL_TANK_STATE", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID TANK_TYPE = new PID(433, "TANK_TYPE", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID BRANDING = new PID(434, "BRANDING", new Formatter(FORMAT.UINT16), 2);

		public static readonly PID SWAY_MODEL_VERSION = new PID(435, "SWAY_MODEL_VERSION", new Formatter(FORMAT.UINT16), 0);

		public static readonly PID INPUT_DOES_NOT_CLEAR_LOCKOUT = new PID(436, "INPUT_DOES_NOT_CLEAR_LOCKOUT", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID PAIRING_PRIORITY = new PID(437, "PAIRING_PRIORITY", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID RGB_LIGHT_TYPE = new PID(438, "RGB_LIGHT_TYPE", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID RV_PREPPED_FOR_TPMS = new PID(439, "RV_PREPPED_FOR_TPMS", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID TANK_SENSOR_CALIBRATION_UID = new PID(440, "TANK_SENSOR_CALIBRATION_UID", new Formatter(FORMAT.UINT16), 2);

		public static readonly PID GROUP_ID_ADD = new PID(441, "GROUP_ID_ADD", new Formatter(FORMAT.UINT16), 2);

		public static readonly PID GROUP_ID_REMOVE = new PID(442, "GROUP_ID_REMOVE", new Formatter(FORMAT.UINT16), 2);

		public static readonly PID GROUP_ID_CAPACITY = new PID(443, "GROUP_ID_CAPACITY", new Formatter(FORMAT.UINT16), 0);

		public static readonly PID TEMPERATURE_SETPOINT_MINIMUM = new PID(444, "TEMPERATURE_SETPOINT_MINIMUM", new Formatter(FORMAT.INT8), 2);

		public static readonly PID TEMPERATURE_SETPOINT_MAXIMUM = new PID(445, "TEMPERATURE_SETPOINT_MAXIMUM", new Formatter(FORMAT.INT8), 2);

		public static readonly PID GENERATOR_AUTO_START_LOW_VOLTAGE_ENABLED = new PID(446, "GENERATOR_AUTO_START_LOW_VOLTAGE_ENABLED", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID QUIET_HOURS_DISABLED_DAYS = new PID(447, "QUIET_HOURS_DISABLED_DAYS", new Formatter(FORMAT.UINT8), 2);

		public static readonly PID MAX_ADAPTIVE_CURRENT_STALL_THRESHOLD = new PID(448, "MAX_ADAPTIVE_CURRENT_STALL_THRESHOLD", new Formatter(FORMAT.UINT32, "{0:0.###} A", 1.52587890625E-05), 2);

		public static readonly PID ADAPTIVE_CURRENT_STALL_MARGIN = new PID(449, "ADAPTIVE_CURRENT_STALL_MARGIN", new Formatter(FORMAT.UINT32, "{0:0.###} A", 1.52587890625E-05), 2);

		public static readonly PID MAX_RELAXATION_DISTANCE = new PID(450, "MAX_RELAXATION_DISTANCE", new Formatter(FORMAT.UINT32, "{0:0.###}\"", 1.52587890625E-05), 2);

		public readonly ushort Value;

		public readonly string Name;

		private readonly Formatter Print;

		public readonly ushort Write_SessionId;

		public bool IsValid => this?.Value > 0;

		public static IEnumerable<PID> GetEnumerator()
		{
			return List;
		}

		private PID(ushort value)
		{
			Value = value;
			Name = "UNKNOWN_" + value.ToString("X4");
			Print = null;
			if (value > 0 && !Lookup.ContainsKey(value))
			{
				Lookup.Add(value, this);
			}
			Write_SessionId = 0;
		}

		private PID(ushort value, string name, Formatter print, ushort write_SessionId = 0)
		{
			Value = value;
			Name = name.Trim();
			Print = print;
			if (value > 0)
			{
				List.Add(this);
				Lookup.Add(value, this);
			}
			Write_SessionId = write_SessionId;
		}

		public string FormatValue(ulong value)
		{
			if (Print == null)
			{
				return value.ToString();
			}
			return Print.ToString(value);
		}

		public static implicit operator ushort(PID msg)
		{
			return msg?.Value ?? 0;
		}

		public static implicit operator PID(ushort value)
		{
			if (value == 0)
			{
				return UNKNOWN;
			}
			if (!Lookup.TryGetValue(value, out var value2))
			{
				return new PID(value);
			}
			return value2;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
