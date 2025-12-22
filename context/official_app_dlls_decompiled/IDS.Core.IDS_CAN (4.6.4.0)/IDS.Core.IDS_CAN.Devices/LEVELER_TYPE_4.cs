using System;
using System.Runtime.CompilerServices;
using IDS.Core.Types;

namespace IDS.Core.IDS_CAN.Devices
{
	public class LEVELER_TYPE_4 : LocalDevice
	{
		public enum CHASSIS_TYPE : byte
		{
			CLASS_A,
			CLASS_C,
			FIFTH_WHEEL,
			TRAVEL_TRAILER
		}

		public enum JACK_CONFIGURATION : byte
		{
			THREE_JACKS,
			FOUR_JACKS,
			FOUR_JACKS_PLUS_TONGUE_JACK,
			SIX_JACKS
		}

		public struct CAPABILITIES
		{
			public byte Value;

			public bool IsJackPositionSupported
			{
				get
				{
					return (Value & 1) != 0;
				}
				set
				{
					if (value)
					{
						Value |= 1;
					}
					else
					{
						Value &= 254;
					}
				}
			}

			public JACK_CONFIGURATION JackConfiguration
			{
				get
				{
					return (JACK_CONFIGURATION)((uint)(Value >> 1) & 3u);
				}
				set
				{
					Value = (byte)((Value & 0xFFFFFFF9u) | ((uint)(value & JACK_CONFIGURATION.SIX_JACKS) << 1));
				}
			}

			public CHASSIS_TYPE ChassisType
			{
				get
				{
					return (CHASSIS_TYPE)((uint)(Value >> 4) & 2u);
				}
				set
				{
					Value = (byte)((Value & 0xFFFFFFCFu) | ((uint)(value & CHASSIS_TYPE.TRAVEL_TRAILER) << 4));
				}
			}

			public CAPABILITIES(byte value)
			{
				Value = value;
			}

			public static CAPABILITIES Create(bool isJackPositionSupported, JACK_CONFIGURATION jackConfiguration, CHASSIS_TYPE chassisType)
			{
				CAPABILITIES result = default(CAPABILITIES);
				result.IsJackPositionSupported = isJackPositionSupported;
				result.JackConfiguration = jackConfiguration;
				result.ChassisType = chassisType;
				return result;
			}
		}

		public enum ENHANCED_MODE : byte
		{
			SCREEN_CONTEXT,
			ABORT_OPERATION,
			BACK,
			HOME
		}

		public enum UI_MODE : byte
		{
			IDLE,
			AUTO,
			MANUAL,
			MANUAL_CONSOLE,
			ZERO,
			INFO,
			YES_NO,
			FAULT_GENERIC,
			FAULT_JACK_MANUAL,
			FAULT_JACK_MANUAL_CONSOLE,
			MANUAL_AIR_BAG_CONTROL,
			AUTO_HITCH,
			AUTO_RETRACT,
			AUTO_RETRACT_FRONT,
			AUTO_RETRACT_REAR,
			HOME_JACKS,
			MODE_10,
			MODE_11,
			MODE_12,
			MODE_13,
			MODE_14,
			MODE_15,
			MODE_16,
			MODE_17,
			MODE_18,
			MODE_19,
			MODE_1A,
			MODE_1B,
			MODE_1C,
			MODE_1D,
			MODE_1E,
			MODE_1F,
			MODE_20,
			MODE_21,
			MODE_22,
			MODE_23,
			MODE_24,
			MODE_25,
			MODE_26,
			MODE_27,
			MODE_28,
			MODE_29,
			MODE_2A,
			MODE_2B,
			MODE_2C,
			MODE_2D,
			MODE_2E,
			MODE_2F,
			MODE_30,
			MODE_31,
			MODE_32,
			MODE_33,
			MODE_34,
			MODE_35,
			MODE_36,
			MODE_37,
			MODE_38,
			MODE_39,
			MODE_3A,
			MODE_3B,
			MODE_3C,
			MODE_3D,
			MODE_3E,
			MODE_3F,
			MODE_40,
			MODE_41,
			MODE_42,
			MODE_43,
			MODE_44,
			MODE_45,
			MODE_46,
			MODE_47,
			MODE_48,
			MODE_49,
			MODE_4A,
			MODE_4B,
			MODE_4C,
			MODE_4D,
			MODE_4E,
			MODE_4F,
			MODE_50,
			MODE_51,
			MODE_52,
			MODE_53,
			MODE_54,
			MODE_55,
			MODE_56,
			MODE_57,
			MODE_58,
			MODE_59,
			MODE_5A,
			MODE_5B,
			MODE_5C,
			MODE_5D,
			MODE_5E,
			MODE_5F,
			MODE_60,
			MODE_61,
			MODE_62,
			MODE_63,
			MODE_64,
			MODE_65,
			MODE_66,
			MODE_67,
			MODE_68,
			MODE_69,
			MODE_6A,
			MODE_6B,
			MODE_6C,
			MODE_6D,
			MODE_6E,
			MODE_6F,
			MODE_70,
			MODE_71,
			MODE_72,
			MODE_73,
			MODE_74,
			MODE_75,
			MODE_76,
			MODE_77,
			MODE_78,
			MODE_79,
			MODE_7A,
			MODE_7B,
			MODE_7C,
			MODE_7D,
			MODE_7E,
			MODE_7F,
			MODE_80,
			MODE_81,
			MODE_82,
			MODE_83,
			MODE_84,
			MODE_85,
			MODE_86,
			MODE_87,
			MODE_88,
			MODE_89,
			MODE_8A,
			MODE_8B,
			MODE_8C,
			MODE_8D,
			MODE_8E,
			MODE_8F,
			MODE_90,
			MODE_91,
			MODE_92,
			MODE_93,
			MODE_94,
			MODE_95,
			MODE_96,
			MODE_97,
			MODE_98,
			MODE_99,
			MODE_9A,
			MODE_9B,
			MODE_9C,
			MODE_9D,
			MODE_9E,
			MODE_9F,
			MODE_A0,
			MODE_A1,
			MODE_A2,
			MODE_A3,
			MODE_A4,
			MODE_A5,
			MODE_A6,
			MODE_A7,
			MODE_A8,
			MODE_A9,
			MODE_AA,
			MODE_AB,
			MODE_AC,
			MODE_AD,
			MODE_AE,
			MODE_AF,
			MODE_B0,
			MODE_B1,
			MODE_B2,
			MODE_B3,
			MODE_B4,
			MODE_B5,
			MODE_B6,
			MODE_B7,
			MODE_B8,
			MODE_B9,
			MODE_BA,
			MODE_BB,
			MODE_BC,
			MODE_BD,
			MODE_BE,
			MODE_BF,
			MODE_C0,
			MODE_C1,
			MODE_C2,
			MODE_C3,
			MODE_C4,
			MODE_C5,
			MODE_C6,
			MODE_C7,
			MODE_C8,
			MODE_C9,
			MODE_CA,
			MODE_CB,
			MODE_CC,
			MODE_CD,
			MODE_CE,
			MODE_CF,
			MODE_D0,
			MODE_D1,
			MODE_D2,
			MODE_D3,
			MODE_D4,
			MODE_D5,
			MODE_D6,
			MODE_D7,
			MODE_D8,
			MODE_D9,
			MODE_DA,
			MODE_DB,
			MODE_DC,
			MODE_DD,
			MODE_DE,
			MODE_DF,
			MODE_E0,
			MODE_E1,
			MODE_E2,
			MODE_E3,
			MODE_E4,
			MODE_E5,
			MODE_E6,
			MODE_E7,
			MODE_E8,
			MODE_E9,
			MODE_EA,
			MODE_EB,
			MODE_EC,
			MODE_ED,
			MODE_EE,
			MODE_EF,
			MODE_F0,
			MODE_F1,
			MODE_F2,
			MODE_F3,
			MODE_F4,
			MODE_F5,
			MODE_F6,
			MODE_F7,
			MODE_F8,
			MODE_F9,
			MODE_FA,
			MODE_FB,
			MODE_FC,
			MODE_FD,
			MODE_FE,
			MODE_FF
		}

		private const double MAX_JACK_STROKE_INCHES = 15.0;

		private const double GROUND_INCHES = 7.5;

		private const byte IS_LEVEL = 1;

		private const byte JACKS_ARE_FULLY_RETRACTED = 2;

		private const byte JACKS_ARE_GROUNDED = 4;

		private const byte JACKS_ARE_MOVING = 8;

		private const byte EXCESS_ANGLE_DETECTED = 16;

		private const byte EXCESS_TWIST_DETECTED = 32;

		private const double MAX_ANGLE_DEGREES = 15.96875;

		private const double MIN_ANGLE_DEGREES = -15.96875;

		private uint ButtonCommand;

		private uint? LastButtonCommand;

		private string[] Lines = new string[6];

		private Timer AngleTime = new Timer();

		private Timer UiModeTime = new Timer();

		private double LFJackStrokeInches;

		private double RFJackStrokeInches;

		private double LMJackStrokeInches;

		private double RMJackStrokeInches;

		private double LRJackStrokeInches;

		private double RRJackStrokeInches;

		private double TongueJackStrokeInches;

		private bool _IsManualChecked;

		public new CAPABILITIES DeviceCapabilities => new CAPABILITIES(base.DeviceCapabilities.GetValueOrDefault());

		public ENHANCED_MODE CommandEnhanced { get; set; }

		public UI_MODE CommandLEVELER_UI_MODE { get; set; }

		public uint CommandUI_Button_State { get; set; }

		public bool IsManualChecked
		{
			get
			{
				return _IsManualChecked;
			}
			set
			{
				SetDeviceStatusFlag(0, 1, value: false);
				SetDeviceStatusFlag(0, 2, value: false);
				SetDeviceStatusFlag(0, 4, value: false);
				SetDeviceStatusFlag(0, 8, value: false);
				SetDeviceStatusFlag(0, 16, value: false);
				SetDeviceStatusFlag(0, 32, value: false);
				_IsManualChecked = value;
			}
		}

		public bool IsLevel
		{
			get
			{
				return GetDeviceStatusFlag(0, 1);
			}
			set
			{
				SetDeviceStatusFlag(0, 1, value);
			}
		}

		public bool JacksAreFullyRetracted
		{
			get
			{
				return GetDeviceStatusFlag(0, 2);
			}
			set
			{
				SetDeviceStatusFlag(0, 2, value);
			}
		}

		public bool JacksAreGrounded
		{
			get
			{
				return GetDeviceStatusFlag(0, 4);
			}
			set
			{
				SetDeviceStatusFlag(0, 4, value);
			}
		}

		public bool JacksAreMoving
		{
			get
			{
				return GetDeviceStatusFlag(0, 8);
			}
			set
			{
				SetDeviceStatusFlag(0, 8, value);
			}
		}

		public bool ExcessAngleDetected
		{
			get
			{
				return GetDeviceStatusFlag(0, 16);
			}
			set
			{
				SetDeviceStatusFlag(0, 16, value);
			}
		}

		public bool ExcessTwistDetected
		{
			get
			{
				return GetDeviceStatusFlag(0, 32);
			}
			set
			{
				SetDeviceStatusFlag(0, 32, value);
			}
		}

		public UI_MODE LevelerUIMode
		{
			get
			{
				return (UI_MODE)base.DeviceStatus[1];
			}
			set
			{
				CAN.PAYLOAD deviceStatus = base.DeviceStatus;
				deviceStatus[1] = (byte)value;
				base.DeviceStatus = deviceStatus;
			}
		}

		public bool Button01Enabled
		{
			get
			{
				return GetDeviceStatusFlag(4, 1);
			}
			set
			{
				SetDeviceStatusFlag(4, 1, value);
			}
		}

		public bool Button02Enabled
		{
			get
			{
				return GetDeviceStatusFlag(4, 2);
			}
			set
			{
				SetDeviceStatusFlag(4, 2, value);
			}
		}

		public bool Button03Enabled
		{
			get
			{
				return GetDeviceStatusFlag(4, 4);
			}
			set
			{
				SetDeviceStatusFlag(4, 4, value);
			}
		}

		public bool Button04Enabled
		{
			get
			{
				return GetDeviceStatusFlag(4, 8);
			}
			set
			{
				SetDeviceStatusFlag(4, 8, value);
			}
		}

		public bool Button05Enabled
		{
			get
			{
				return GetDeviceStatusFlag(4, 16);
			}
			set
			{
				SetDeviceStatusFlag(4, 16, value);
			}
		}

		public bool Button06Enabled
		{
			get
			{
				return GetDeviceStatusFlag(4, 32);
			}
			set
			{
				SetDeviceStatusFlag(4, 32, value);
			}
		}

		public bool Button07Enabled
		{
			get
			{
				return GetDeviceStatusFlag(4, 64);
			}
			set
			{
				SetDeviceStatusFlag(4, 64, value);
			}
		}

		public bool Button08Enabled
		{
			get
			{
				return GetDeviceStatusFlag(4, 128);
			}
			set
			{
				SetDeviceStatusFlag(4, 128, value);
			}
		}

		public bool Button09Enabled
		{
			get
			{
				return GetDeviceStatusFlag(3, 1);
			}
			set
			{
				SetDeviceStatusFlag(3, 1, value);
			}
		}

		public bool Button10Enabled
		{
			get
			{
				return GetDeviceStatusFlag(3, 2);
			}
			set
			{
				SetDeviceStatusFlag(3, 2, value);
			}
		}

		public bool Button11Enabled
		{
			get
			{
				return GetDeviceStatusFlag(3, 4);
			}
			set
			{
				SetDeviceStatusFlag(3, 4, value);
			}
		}

		public bool Button12Enabled
		{
			get
			{
				return GetDeviceStatusFlag(3, 8);
			}
			set
			{
				SetDeviceStatusFlag(3, 8, value);
			}
		}

		public bool Button13Enabled
		{
			get
			{
				return GetDeviceStatusFlag(3, 16);
			}
			set
			{
				SetDeviceStatusFlag(3, 16, value);
			}
		}

		public bool Button14Enabled
		{
			get
			{
				return GetDeviceStatusFlag(3, 32);
			}
			set
			{
				SetDeviceStatusFlag(3, 32, value);
			}
		}

		public bool Button15Enabled
		{
			get
			{
				return GetDeviceStatusFlag(3, 64);
			}
			set
			{
				SetDeviceStatusFlag(3, 64, value);
			}
		}

		public bool Button16Enabled
		{
			get
			{
				return GetDeviceStatusFlag(3, 128);
			}
			set
			{
				SetDeviceStatusFlag(3, 128, value);
			}
		}

		public bool Button17Enabled
		{
			get
			{
				return GetDeviceStatusFlag(2, 1);
			}
			set
			{
				SetDeviceStatusFlag(2, 1, value);
			}
		}

		public bool Button18Enabled
		{
			get
			{
				return GetDeviceStatusFlag(2, 2);
			}
			set
			{
				SetDeviceStatusFlag(2, 2, value);
			}
		}

		public bool Button19Enabled
		{
			get
			{
				return GetDeviceStatusFlag(2, 4);
			}
			set
			{
				SetDeviceStatusFlag(2, 4, value);
			}
		}

		public bool Button20Enabled
		{
			get
			{
				return GetDeviceStatusFlag(2, 8);
			}
			set
			{
				SetDeviceStatusFlag(2, 8, value);
			}
		}

		public bool Button21Enabled
		{
			get
			{
				return GetDeviceStatusFlag(2, 16);
			}
			set
			{
				SetDeviceStatusFlag(2, 16, value);
			}
		}

		public bool Button22Enabled
		{
			get
			{
				return GetDeviceStatusFlag(2, 32);
			}
			set
			{
				SetDeviceStatusFlag(2, 32, value);
			}
		}

		public bool Button23Enabled
		{
			get
			{
				return GetDeviceStatusFlag(2, 64);
			}
			set
			{
				SetDeviceStatusFlag(2, 64, value);
			}
		}

		public bool Button24Enabled
		{
			get
			{
				return GetDeviceStatusFlag(2, 128);
			}
			set
			{
				SetDeviceStatusFlag(2, 128, value);
			}
		}

		public double XAngleDegrees
		{
			get
			{
				CAN.PAYLOAD deviceStatus = base.DeviceStatus;
				double num = deviceStatus[5] >> 4;
				num += (double)(deviceStatus[6] >> 3) / 32.0;
				if ((deviceStatus[6] & 4u) != 0)
				{
					num = 0.0 - num;
				}
				return num;
			}
			set
			{
				Tuple<int, int, bool> tuple = DecomposeAngle(value);
				CAN.PAYLOAD deviceStatus = base.DeviceStatus;
				deviceStatus[5] = (byte)((uint)(tuple.Item1 << 4) | (deviceStatus[5] & 0xFu));
				deviceStatus[6] = (byte)((uint)(tuple.Item2 << 3) | (tuple.Item3 ? 4u : 0u) | (deviceStatus[6] & 3u));
				base.DeviceStatus = deviceStatus;
			}
		}

		public double YAngleDegrees
		{
			get
			{
				CAN.PAYLOAD deviceStatus = base.DeviceStatus;
				double num = deviceStatus[5] & 0xF;
				num += (double)(deviceStatus[7] >> 3) / 32.0;
				if ((deviceStatus[7] & 4u) != 0)
				{
					num = 0.0 - num;
				}
				return num;
			}
			set
			{
				Tuple<int, int, bool> tuple = DecomposeAngle(value);
				CAN.PAYLOAD deviceStatus = base.DeviceStatus;
				deviceStatus[5] = (byte)((deviceStatus[5] & 0xF0u) | ((uint)tuple.Item1 & 0xFu));
				deviceStatus[7] = (byte)((uint)(tuple.Item2 << 3) | (tuple.Item3 ? 4u : 0u) | (deviceStatus[7] & 3u));
				base.DeviceStatus = deviceStatus;
			}
		}

		public byte Bubble
		{
			get
			{
				CAN.PAYLOAD deviceStatus = base.DeviceStatus;
				return (byte)((uint)((deviceStatus[6] & 3) << 2) | (deviceStatus[7] & 3u));
			}
			set
			{
				CAN.PAYLOAD deviceStatus = base.DeviceStatus;
				deviceStatus[6] = (byte)((deviceStatus[6] & 0xFCu) | ((uint)(value >> 2) & 3u));
				deviceStatus[7] = (byte)((deviceStatus[7] & 0xFCu) | (value & 3u));
				base.DeviceStatus = deviceStatus;
			}
		}

		public TEXT_CONSOLE_SIZE TextConsoleSize
		{
			get
			{
				if (base.TextConsole == null)
				{
					return default(TEXT_CONSOLE_SIZE);
				}
				return base.TextConsole.Size;
			}
			set
			{
				int num = Lines.Length;
				int w = 32;
				if (value.Height > 0 && value.Height <= Lines.Length)
				{
					num = value.Height;
					if (value.Width > 0 && value.Width <= 32)
					{
						w = value.Width;
					}
				}
				CreateTextConsole(new TEXT_CONSOLE_SIZE(w, num));
				for (int i = 0; i < num; i++)
				{
					string[] lines = Lines;
					int num2 = i;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Line #");
					defaultInterpolatedStringHandler.AppendFormatted(i);
					lines[num2] = defaultInterpolatedStringHandler.ToStringAndClear();
				}
				base.TextConsole.Lines = Lines;
			}
		}

		private UInt48 LEVELER_AUTO_MODE_PROGRESS
		{
			get
			{
				uint num = (uint)LevelerUIMode;
				if (LevelerUIMode == UI_MODE.AUTO)
				{
					uint num2 = 10u;
					uint num3 = Math.Min(num2, (uint)UiModeTime.ElapsedTime.TotalSeconds / 5u);
					num |= num2 << 8;
					num |= num3 << 16;
				}
				return num;
			}
		}

		public LEVELER_TYPE_4(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, CAPABILITIES capabilities, MAC mac = null)
			: base(new LocalProduct(adapter, mac, product_id, version, software_part_number), (byte)40, 0, (ushort)109, 4, capabilities.Value, options)
		{
			base.DeviceStatus = new CAN.PAYLOAD(8);
			AddPID(PID.BATTERY_VOLTAGE, () => 819187u);
			AddPID(PID.LEVELER_UI_SUPPORTED_FEATURES, () => uint.MaxValue);
			AddPID(PID.LEVELER_SENSOR_TOPOLOGY, () => (byte)1);
			AddPID(PID.LEVELER_DRIVE_TYPE, () => (byte)1);
			AddPID(PID.LEVELER_AUTO_MODE_PROGRESS, () => LEVELER_AUTO_MODE_PROGRESS);
			if (capabilities.IsJackPositionSupported)
			{
				switch (capabilities.JackConfiguration)
				{
				case JACK_CONFIGURATION.FOUR_JACKS:
				case JACK_CONFIGURATION.FOUR_JACKS_PLUS_TONGUE_JACK:
					AddPID(PID.LEFT_FRONT_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(LFJackStrokeInches * 65536.0));
					AddPID(PID.RIGHT_FRONT_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(RFJackStrokeInches * 65536.0));
					AddPID(PID.LEFT_REAR_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(LRJackStrokeInches * 65536.0));
					AddPID(PID.RIGHT_REAR_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(RRJackStrokeInches * 65536.0));
					AddPID(PID.LEFT_FRONT_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					AddPID(PID.RIGHT_FRONT_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					AddPID(PID.LEFT_REAR_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					AddPID(PID.RIGHT_REAR_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					break;
				case JACK_CONFIGURATION.SIX_JACKS:
					AddPID(PID.LEFT_FRONT_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(LFJackStrokeInches * 65536.0));
					AddPID(PID.RIGHT_FRONT_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(RFJackStrokeInches * 65536.0));
					AddPID(PID.LEFT_MIDDLE_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(LMJackStrokeInches * 65536.0));
					AddPID(PID.RIGHT_MIDDLE_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(RMJackStrokeInches * 65536.0));
					AddPID(PID.LEFT_REAR_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(LRJackStrokeInches * 65536.0));
					AddPID(PID.RIGHT_REAR_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(RRJackStrokeInches * 65536.0));
					AddPID(PID.LEFT_FRONT_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					AddPID(PID.RIGHT_FRONT_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					AddPID(PID.LEFT_MIDDLE_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					AddPID(PID.RIGHT_MIDDLE_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					AddPID(PID.LEFT_REAR_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					AddPID(PID.RIGHT_REAR_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					break;
				case JACK_CONFIGURATION.THREE_JACKS:
					AddPID(PID.LEFT_FRONT_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(LFJackStrokeInches * 65536.0));
					AddPID(PID.RIGHT_FRONT_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(LFJackStrokeInches * 65536.0));
					AddPID(PID.LEFT_REAR_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(LRJackStrokeInches * 65536.0));
					AddPID(PID.RIGHT_REAR_JACK_STROKE_INCHES, () => (UInt48)(int)Math.Round(RRJackStrokeInches * 65536.0));
					AddPID(PID.LEFT_FRONT_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					AddPID(PID.RIGHT_FRONT_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					AddPID(PID.LEFT_MIDDLE_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					AddPID(PID.RIGHT_MIDDLE_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					AddPID(PID.LEFT_REAR_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					AddPID(PID.RIGHT_REAR_JACK_MAX_STROKE_INCHES, () => (UInt48)(int)Math.Round(983040.0));
					break;
				}
			}
			SetUiMode(UI_MODE.IDLE, force_update: true);
		}

		private bool GetDeviceStatusFlag(byte index, byte flag)
		{
			return (base.DeviceStatus[index] & flag) != 0;
		}

		private void SetDeviceStatusFlag(byte index, byte flag, bool value)
		{
			CAN.PAYLOAD deviceStatus = base.DeviceStatus;
			if (value)
			{
				deviceStatus[index] |= flag;
			}
			else
			{
				deviceStatus[index] = (byte)(deviceStatus[index] & ~flag);
			}
			base.DeviceStatus = deviceStatus;
		}

		private Tuple<int, int, bool> DecomposeAngle(double angle)
		{
			bool flag = angle < 0.0;
			if (flag)
			{
				angle = 0.0 - angle;
			}
			if (angle > 15.96875)
			{
				angle = 15.96875;
			}
			int num = (int)Math.Round(32.0 * angle);
			int num2 = num & 0x1F;
			int num3 = num >> 5;
			if (num3 > 15)
			{
				return Tuple.Create(15, 31, flag);
			}
			return Tuple.Create(num3, num2, flag);
		}

		protected override void OnBackgroundTask()
		{
			base.OnBackgroundTask();
			if (!GetLocalSessionClientAddress((ushort)4).IsValidDeviceAddress)
			{
				if (!IsManualChecked)
				{
					SetUiMode(UI_MODE.IDLE);
				}
				ButtonCommand = 0u;
			}
			if (!IsManualChecked)
			{
				double num = AngleTime.ElapsedTime.TotalSeconds / 5.0;
				XAngleDegrees = 15.0 * Math.Cos(3.0 * num);
				YAngleDegrees = 15.0 * Math.Sin(4.0 * num);
				IsLevel = Math.Abs(XAngleDegrees) <= 1.0 && Math.Abs(YAngleDegrees) <= 1.0;
				ExcessAngleDetected = Math.Abs(XAngleDegrees) > 12.0 || Math.Abs(YAngleDegrees) > 12.0;
				ExcessTwistDetected = Math.Abs(XAngleDegrees) > 10.0 && Math.Abs(YAngleDegrees) > 10.0;
			}
			switch (LevelerUIMode)
			{
			case UI_MODE.AUTO:
				if (!IsManualChecked)
				{
					JacksAreMoving = true;
				}
				break;
			case UI_MODE.MANUAL:
			case UI_MODE.MANUAL_CONSOLE:
			case UI_MODE.ZERO:
			case UI_MODE.FAULT_JACK_MANUAL:
			case UI_MODE.FAULT_JACK_MANUAL_CONSOLE:
				if ((ButtonCommand & (true ? 1u : 0u)) != 0)
				{
					RFJackStrokeInches += 0.01;
				}
				if ((ButtonCommand & 2u) != 0)
				{
					RFJackStrokeInches -= 0.01;
				}
				if ((ButtonCommand & 4u) != 0)
				{
					LFJackStrokeInches += 0.01;
				}
				if ((ButtonCommand & 8u) != 0)
				{
					LFJackStrokeInches -= 0.01;
				}
				if ((ButtonCommand & 0x10u) != 0)
				{
					RRJackStrokeInches += 0.01;
				}
				if ((ButtonCommand & 0x20u) != 0)
				{
					RRJackStrokeInches -= 0.01;
				}
				if ((ButtonCommand & 0x40u) != 0)
				{
					LRJackStrokeInches += 0.01;
				}
				if ((ButtonCommand & 0x80u) != 0)
				{
					LRJackStrokeInches -= 0.01;
				}
				if ((ButtonCommand & 0x100u) != 0)
				{
					TongueJackStrokeInches += 0.01;
				}
				if ((ButtonCommand & 0x200u) != 0)
				{
					TongueJackStrokeInches -= 0.01;
				}
				if ((ButtonCommand & 0x400u) != 0)
				{
					RMJackStrokeInches += 0.01;
				}
				if ((ButtonCommand & 0x800u) != 0)
				{
					RMJackStrokeInches -= 0.01;
				}
				if ((ButtonCommand & 0x1000u) != 0)
				{
					LMJackStrokeInches += 0.01;
				}
				if ((ButtonCommand & 0x2000u) != 0)
				{
					LMJackStrokeInches -= 0.01;
				}
				break;
			}
			if (!IsManualChecked)
			{
				JacksAreFullyRetracted = LFJackStrokeInches <= 0.0 && RFJackStrokeInches <= 0.0 && LMJackStrokeInches <= 0.0 && RMJackStrokeInches <= 0.0 && LRJackStrokeInches <= 0.0 && RRJackStrokeInches <= 0.0;
			}
			if (!IsManualChecked)
			{
				JacksAreGrounded = LFJackStrokeInches >= 7.5 && RFJackStrokeInches >= 7.5 && LMJackStrokeInches >= 7.5 && RMJackStrokeInches >= 7.5 && LRJackStrokeInches >= 7.5 && RRJackStrokeInches >= 7.5;
			}
		}

		protected override void OnLocalDeviceRxEvent(AdapterRxEvent rx)
		{
			base.OnLocalDeviceRxEvent(rx);
			if ((byte)rx.MessageType != 130 || rx.TargetAddress != base.Address || rx.SourceAddress != GetLocalSessionClientAddress((ushort)4) || rx.SourceDevice == null)
			{
				return;
			}
			switch (rx.MessageData)
			{
			case 0:
				if (rx.Length != 4)
				{
					break;
				}
				CommandEnhanced = (ENHANCED_MODE)rx.MessageData;
				CommandLEVELER_UI_MODE = (UI_MODE)rx[0];
				CommandUI_Button_State = (uint)((rx[1] << 16) | (rx[2] << 8) | rx[3]);
				if (IsManualChecked)
				{
					break;
				}
				if (rx.SourceDevice.ProductID.Name == "Simulated Product" && (uint)rx[0] == (uint)LevelerUIMode)
				{
					uint buttonCommand = ButtonCommand;
					ButtonCommand = rx.GetUINT24(1);
					uint num = (ButtonCommand ^ buttonCommand) & ButtonCommand;
					switch (LevelerUIMode)
					{
					case UI_MODE.IDLE:
						if ((num & (true ? 1u : 0u)) != 0)
						{
							SetUiMode(UI_MODE.AUTO);
						}
						if ((num & 2u) != 0)
						{
							SetUiMode(UI_MODE.AUTO);
						}
						if ((num & 4u) != 0)
						{
							SetUiMode(UI_MODE.AUTO);
						}
						if ((num & 8u) != 0)
						{
							SetUiMode(UI_MODE.AUTO);
						}
						if ((num & 0x10u) != 0)
						{
							SetUiMode(UI_MODE.AUTO);
						}
						if ((num & 0x20u) != 0)
						{
							SetUiMode(UI_MODE.MANUAL);
						}
						if ((num & 0x40u) != 0)
						{
							SetUiMode(UI_MODE.MANUAL_AIR_BAG_CONTROL);
						}
						if ((num & 0x80u) != 0)
						{
							SetUiMode(UI_MODE.ZERO);
						}
						if ((num & 0x100u) != 0)
						{
							SetUiMode(UI_MODE.MANUAL);
						}
						if ((num & 0x200u) != 0)
						{
							SetUiMode(UI_MODE.YES_NO);
						}
						break;
					case UI_MODE.MANUAL:
					case UI_MODE.MANUAL_CONSOLE:
						if (!IsManualChecked)
						{
							JacksAreMoving = (ButtonCommand & 0x3FFFF) != 0;
						}
						break;
					case UI_MODE.ZERO:
						if (!IsManualChecked)
						{
							JacksAreMoving = (ButtonCommand & 0x3FFFF) != 0;
						}
						if ((num & 0x40000u) != 0)
						{
							SetUiMode(UI_MODE.IDLE);
						}
						break;
					case UI_MODE.INFO:
						if ((num & (true ? 1u : 0u)) != 0)
						{
							SetUiMode(UI_MODE.IDLE);
						}
						break;
					case UI_MODE.YES_NO:
						if ((num & (true ? 1u : 0u)) != 0)
						{
							SetUiMode(UI_MODE.IDLE);
						}
						if ((num & 2u) != 0)
						{
							SetUiMode(UI_MODE.IDLE);
						}
						break;
					case UI_MODE.FAULT_GENERIC:
						if ((num & (true ? 1u : 0u)) != 0)
						{
							SetUiMode(UI_MODE.IDLE);
						}
						break;
					case UI_MODE.FAULT_JACK_MANUAL:
					case UI_MODE.FAULT_JACK_MANUAL_CONSOLE:
						if (!IsManualChecked)
						{
							JacksAreMoving = (ButtonCommand & 0x3FFFFF) != 0;
						}
						if ((num & 0x400000u) != 0)
						{
							SetUiMode(UI_MODE.AUTO);
						}
						break;
					case UI_MODE.AUTO:
					case UI_MODE.MANUAL_AIR_BAG_CONTROL:
						break;
					}
				}
				else if (rx.SourceDevice.ProductID.Name != "Simulated Product")
				{
					ButtonCommand = rx.GetUINT24(1);
					SetUiMode((UI_MODE)rx[0]);
				}
				break;
			case 1:
				if (rx.Length == 0)
				{
					CommandEnhanced = (ENHANCED_MODE)rx.MessageData;
					SetUiMode(UI_MODE.IDLE);
				}
				break;
			case 2:
				if (rx.Length == 1 && (uint)rx[0] == (uint)LevelerUIMode)
				{
					CommandEnhanced = (ENHANCED_MODE)rx.MessageData;
					CommandLEVELER_UI_MODE = (UI_MODE)rx[0];
					if (LevelerUIMode != 0)
					{
						SetUiMode(LevelerUIMode - 1);
					}
				}
				break;
			case 3:
				if (rx.Length == 0)
				{
					CommandEnhanced = (ENHANCED_MODE)rx.MessageData;
					SetUiMode(UI_MODE.IDLE);
				}
				break;
			}
		}

		public void SetUiMode(UI_MODE mode, bool force_update = false)
		{
			if (force_update || LevelerUIMode != mode)
			{
				LevelerUIMode = mode;
				UiModeTime.Reset();
				ButtonCommand = 0u;
				if (!IsManualChecked)
				{
					JacksAreMoving = false;
				}
				switch (mode)
				{
				case UI_MODE.IDLE:
					LFJackStrokeInches = 0.0;
					RFJackStrokeInches = 0.0;
					LMJackStrokeInches = 0.0;
					RMJackStrokeInches = 0.0;
					LRJackStrokeInches = 0.0;
					RRJackStrokeInches = 0.0;
					TongueJackStrokeInches = 0.0;
					break;
				case UI_MODE.AUTO:
				case UI_MODE.MANUAL:
				case UI_MODE.MANUAL_CONSOLE:
				case UI_MODE.ZERO:
					LFJackStrokeInches = 8.5;
					RFJackStrokeInches = 8.5;
					LMJackStrokeInches = 8.5;
					RMJackStrokeInches = 8.5;
					LRJackStrokeInches = 8.5;
					RRJackStrokeInches = 8.5;
					TongueJackStrokeInches = 8.5;
					break;
				}
				int num = 0;
				num = mode switch
				{
					UI_MODE.IDLE => 10, 
					UI_MODE.AUTO => 0, 
					UI_MODE.MANUAL => 10, 
					UI_MODE.MANUAL_CONSOLE => 10, 
					UI_MODE.ZERO => 11, 
					UI_MODE.INFO => 1, 
					UI_MODE.YES_NO => 2, 
					UI_MODE.FAULT_GENERIC => 1, 
					UI_MODE.FAULT_JACK_MANUAL => 15, 
					UI_MODE.FAULT_JACK_MANUAL_CONSOLE => 15, 
					UI_MODE.MANUAL_AIR_BAG_CONTROL => 2, 
					_ => 1, 
				};
				Button01Enabled = num >= 1;
				Button02Enabled = num >= 2;
				Button03Enabled = num >= 3;
				Button04Enabled = num >= 4;
				Button05Enabled = num >= 5;
				Button06Enabled = num >= 6;
				Button07Enabled = num >= 7;
				Button08Enabled = num >= 8;
				Button09Enabled = num >= 9;
				Button10Enabled = num >= 10;
				Button11Enabled = num >= 11;
				Button12Enabled = num >= 12;
				Button13Enabled = num >= 13;
				Button14Enabled = num >= 14;
				Button15Enabled = num >= 15;
				Button16Enabled = num >= 16;
				Button17Enabled = num >= 17;
				Button18Enabled = num >= 18;
				Button19Enabled = num >= 19;
				Button20Enabled = num >= 20;
				Button21Enabled = num >= 21;
				Button22Enabled = num >= 22;
				Button23Enabled = num >= 23;
				Button24Enabled = num >= 24;
			}
		}
	}
}
