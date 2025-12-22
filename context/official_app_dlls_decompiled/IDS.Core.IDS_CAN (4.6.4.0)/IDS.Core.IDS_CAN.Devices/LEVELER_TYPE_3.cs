using System;
using System.Runtime.CompilerServices;

namespace IDS.Core.IDS_CAN.Devices
{
	public class LEVELER_TYPE_3 : LocalDevice
	{
		private LEVELER_TYPE_3_SCREEN _screen;

		private string[] Lines = new string[6];

		private bool _isDisabledButton_ReservedBit15;

		private bool _isDisabledButton_ReservedBit14;

		private bool _isDisabledButton_ReservedBit13;

		private bool _isDisabledButton_EnterSetup;

		private bool _isDisabledButton_AutoHitch;

		private bool _isDisabledButton_MenuUp;

		private bool _isDisabledButton_Back;

		private bool _isDisabledButton_Extend;

		private bool _isDisabledButton_MenuDown;

		private bool _isDisabledButton_Enter;

		private bool _isDisabledButton_Retract;

		private bool _isDisabledButton_AutoLevel;

		private bool _isDisabledButton_Front;

		private bool _isDisabledButton_Rear;

		private bool _isDisabledButton_Left;

		private bool _isDisabledButton_Right;

		private short _blinkRateOnCount;

		private short _blinkRateOffCount;

		private LEVELER_TYPE_3_LED_STATE _indicator_FrontLED;

		private LEVELER_TYPE_3_LED_STATE _indicator_RearLED;

		private LEVELER_TYPE_3_LED_STATE _indicator_LeftLED;

		private LEVELER_TYPE_3_LED_STATE _indicator_RightLED;

		private LEVELER_TYPE_3_LED_STATE _indicator_LevelLED;

		private LEVELER_TYPE_3_LED_STATE _indicator_ExtendLED;

		private LEVELER_TYPE_3_LED_STATE _indicator_RetractLED;

		private LEVELER_TYPE_3_LED_STATE _indicator_Buzzer;

		private LEVELER_TYPE_3_SCREEN _command_Screen;

		private bool _command_Button_EnterSetup;

		private bool _command_Button_AutoHitch;

		private bool _command_Button_MenuUp;

		private bool _command_Button_Back;

		private bool _command_Button_Extend;

		private bool _command_Button_MenuDown;

		private bool _command_Button_Enter;

		private bool _command_Button_Retract;

		private bool _command_Button_AutoLevel;

		private bool _command_Button_Front;

		private bool _command_Button_Rear;

		private bool _command_Button_Left;

		private bool _command_Button_Right;

		public LEVELER_TYPE_3_SCREEN Screen
		{
			get
			{
				return _screen;
			}
			set
			{
				if (_screen != value)
				{
					_screen = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_ReservedBit15
		{
			get
			{
				return _isDisabledButton_ReservedBit15;
			}
			set
			{
				if (_isDisabledButton_ReservedBit15 != value)
				{
					_isDisabledButton_ReservedBit15 = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_ReservedBit14
		{
			get
			{
				return _isDisabledButton_ReservedBit14;
			}
			set
			{
				if (_isDisabledButton_ReservedBit14 != value)
				{
					_isDisabledButton_ReservedBit14 = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_ReservedBit13
		{
			get
			{
				return _isDisabledButton_ReservedBit13;
			}
			set
			{
				if (_isDisabledButton_ReservedBit13 != value)
				{
					_isDisabledButton_ReservedBit13 = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_EnterSetup
		{
			get
			{
				return _isDisabledButton_EnterSetup;
			}
			set
			{
				if (_isDisabledButton_EnterSetup != value)
				{
					_isDisabledButton_EnterSetup = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_AutoHitch
		{
			get
			{
				return _isDisabledButton_AutoHitch;
			}
			set
			{
				if (_isDisabledButton_AutoHitch != value)
				{
					_isDisabledButton_AutoHitch = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_MenuUp
		{
			get
			{
				return _isDisabledButton_MenuUp;
			}
			set
			{
				if (_isDisabledButton_MenuUp != value)
				{
					_isDisabledButton_MenuUp = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_Back
		{
			get
			{
				return _isDisabledButton_Back;
			}
			set
			{
				if (_isDisabledButton_Back != value)
				{
					_isDisabledButton_Back = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_Extend
		{
			get
			{
				return _isDisabledButton_Extend;
			}
			set
			{
				if (_isDisabledButton_Extend != value)
				{
					_isDisabledButton_Extend = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_MenuDown
		{
			get
			{
				return _isDisabledButton_MenuDown;
			}
			set
			{
				if (_isDisabledButton_MenuDown != value)
				{
					_isDisabledButton_MenuDown = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_Enter
		{
			get
			{
				return _isDisabledButton_Enter;
			}
			set
			{
				if (_isDisabledButton_Enter != value)
				{
					_isDisabledButton_Enter = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_Retract
		{
			get
			{
				return _isDisabledButton_Retract;
			}
			set
			{
				if (_isDisabledButton_Retract != value)
				{
					_isDisabledButton_Retract = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_AutoLevel
		{
			get
			{
				return _isDisabledButton_AutoLevel;
			}
			set
			{
				if (_isDisabledButton_AutoLevel != value)
				{
					_isDisabledButton_AutoLevel = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_Front
		{
			get
			{
				return _isDisabledButton_Front;
			}
			set
			{
				if (_isDisabledButton_Front != value)
				{
					_isDisabledButton_Front = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_Rear
		{
			get
			{
				return _isDisabledButton_Rear;
			}
			set
			{
				if (_isDisabledButton_Rear != value)
				{
					_isDisabledButton_Rear = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_Left
		{
			get
			{
				return _isDisabledButton_Left;
			}
			set
			{
				if (_isDisabledButton_Left != value)
				{
					_isDisabledButton_Left = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDisabledButton_Right
		{
			get
			{
				return _isDisabledButton_Right;
			}
			set
			{
				if (_isDisabledButton_Right != value)
				{
					_isDisabledButton_Right = value;
					UpdateStatus();
				}
			}
		}

		public short BlinkRateOnCount
		{
			get
			{
				return _blinkRateOnCount;
			}
			set
			{
				if (_blinkRateOnCount != value)
				{
					_blinkRateOnCount = value;
					UpdateStatus();
				}
			}
		}

		public short BlinkRateOffCount
		{
			get
			{
				return _blinkRateOffCount;
			}
			set
			{
				if (_blinkRateOffCount != value)
				{
					_blinkRateOffCount = value;
					UpdateStatus();
				}
			}
		}

		public LEVELER_TYPE_3_LED_STATE Indicator_FrontLED
		{
			get
			{
				return _indicator_FrontLED;
			}
			set
			{
				if (_indicator_FrontLED != value)
				{
					_indicator_FrontLED = value;
					UpdateStatus();
				}
			}
		}

		public LEVELER_TYPE_3_LED_STATE Indicator_RearLED
		{
			get
			{
				return _indicator_RearLED;
			}
			set
			{
				if (_indicator_RearLED != value)
				{
					_indicator_RearLED = value;
					UpdateStatus();
				}
			}
		}

		public LEVELER_TYPE_3_LED_STATE Indicator_LeftLED
		{
			get
			{
				return _indicator_LeftLED;
			}
			set
			{
				if (_indicator_LeftLED != value)
				{
					_indicator_LeftLED = value;
					UpdateStatus();
				}
			}
		}

		public LEVELER_TYPE_3_LED_STATE Indicator_RightLED
		{
			get
			{
				return _indicator_RightLED;
			}
			set
			{
				if (_indicator_RightLED != value)
				{
					_indicator_RightLED = value;
					UpdateStatus();
				}
			}
		}

		public LEVELER_TYPE_3_LED_STATE Indicator_LevelLED
		{
			get
			{
				return _indicator_LevelLED;
			}
			set
			{
				if (_indicator_LevelLED != value)
				{
					_indicator_LevelLED = value;
					UpdateStatus();
				}
			}
		}

		public LEVELER_TYPE_3_LED_STATE Indicator_ExtendLED
		{
			get
			{
				return _indicator_ExtendLED;
			}
			set
			{
				if (_indicator_ExtendLED != value)
				{
					_indicator_ExtendLED = value;
					UpdateStatus();
				}
			}
		}

		public LEVELER_TYPE_3_LED_STATE Indicator_RetractLED
		{
			get
			{
				return _indicator_RetractLED;
			}
			set
			{
				if (_indicator_RetractLED != value)
				{
					_indicator_RetractLED = value;
					UpdateStatus();
				}
			}
		}

		public LEVELER_TYPE_3_LED_STATE Indicator_Buzzer
		{
			get
			{
				return _indicator_Buzzer;
			}
			set
			{
				if (_indicator_Buzzer != value)
				{
					_indicator_Buzzer = value;
					UpdateStatus();
				}
			}
		}

		public LEVELER_TYPE_3_SCREEN Command_Screen
		{
			get
			{
				return _command_Screen;
			}
			private set
			{
				if (_command_Screen != value)
				{
					_command_Screen = value;
				}
			}
		}

		public bool Command_Button_EnterSetup
		{
			get
			{
				return _command_Button_EnterSetup;
			}
			private set
			{
				if (_command_Button_EnterSetup != value)
				{
					_command_Button_EnterSetup = value;
				}
			}
		}

		public bool Command_Button_AutoHitch
		{
			get
			{
				return _command_Button_AutoHitch;
			}
			private set
			{
				if (_command_Button_AutoHitch != value)
				{
					_command_Button_AutoHitch = value;
				}
			}
		}

		public bool Command_Button_MenuUp
		{
			get
			{
				return _command_Button_MenuUp;
			}
			private set
			{
				if (_command_Button_MenuUp != value)
				{
					_command_Button_MenuUp = value;
				}
			}
		}

		public bool Command_Button_Back
		{
			get
			{
				return _command_Button_Back;
			}
			private set
			{
				if (_command_Button_Back != value)
				{
					_command_Button_Back = value;
				}
			}
		}

		public bool Command_Button_Extend
		{
			get
			{
				return _command_Button_Extend;
			}
			private set
			{
				if (_command_Button_Extend != value)
				{
					_command_Button_Extend = value;
				}
			}
		}

		public bool Command_Button_MenuDown
		{
			get
			{
				return _command_Button_MenuDown;
			}
			private set
			{
				if (_command_Button_MenuDown != value)
				{
					_command_Button_MenuDown = value;
				}
			}
		}

		public bool Command_Button_Enter
		{
			get
			{
				return _command_Button_Enter;
			}
			private set
			{
				if (_command_Button_Enter != value)
				{
					_command_Button_Enter = value;
				}
			}
		}

		public bool Command_Button_Retract
		{
			get
			{
				return _command_Button_Retract;
			}
			private set
			{
				if (_command_Button_Retract != value)
				{
					_command_Button_Retract = value;
				}
			}
		}

		public bool Command_Button_AutoLevel
		{
			get
			{
				return _command_Button_AutoLevel;
			}
			private set
			{
				if (_command_Button_AutoLevel != value)
				{
					_command_Button_AutoLevel = value;
				}
			}
		}

		public bool Command_Button_Front
		{
			get
			{
				return _command_Button_Front;
			}
			private set
			{
				if (_command_Button_Front != value)
				{
					_command_Button_Front = value;
				}
			}
		}

		public bool Command_Button_Rear
		{
			get
			{
				return _command_Button_Rear;
			}
			private set
			{
				if (_command_Button_Rear != value)
				{
					_command_Button_Rear = value;
				}
			}
		}

		public bool Command_Button_Left
		{
			get
			{
				return _command_Button_Left;
			}
			private set
			{
				if (_command_Button_Left != value)
				{
					_command_Button_Left = value;
				}
			}
		}

		public bool Command_Button_Right
		{
			get
			{
				return _command_Button_Right;
			}
			private set
			{
				if (_command_Button_Right != value)
				{
					_command_Button_Right = value;
				}
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

		public LEVELER_TYPE_3(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(new LocalProduct(adapter, mac, product_id, version, software_part_number), (byte)17, 0, (ushort)109, 3, 0, options)
		{
			Init();
			AddPID(PID.BATTERY_VOLTAGE, () => 819187u);
		}

		private void Init()
		{
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			CAN.PAYLOAD deviceStatus = new CAN.PAYLOAD(6);
			deviceStatus[0] = (byte)Screen;
			byte b = 0;
			byte b2 = Convert.ToByte(IsDisabledButton_EnterSetup);
			b = (byte)(b | (byte)(b2 << 4));
			b2 = Convert.ToByte(IsDisabledButton_AutoHitch);
			b = (byte)(b | (byte)(b2 << 3));
			b2 = Convert.ToByte(IsDisabledButton_MenuUp);
			b = (byte)(b | (byte)(b2 << 2));
			b2 = Convert.ToByte(IsDisabledButton_Back);
			b = (byte)(b | (byte)(b2 << 1));
			b2 = Convert.ToByte(IsDisabledButton_Extend);
			b = (deviceStatus[1] = (byte)(b | b2));
			b = 0;
			b2 = Convert.ToByte(IsDisabledButton_MenuDown);
			b = (byte)(b | (byte)(b2 << 7));
			b2 = Convert.ToByte(IsDisabledButton_Enter);
			b = (byte)(b | (byte)(b2 << 6));
			b2 = Convert.ToByte(IsDisabledButton_Retract);
			b = (byte)(b | (byte)(b2 << 5));
			b2 = Convert.ToByte(IsDisabledButton_AutoLevel);
			b = (byte)(b | (byte)(b2 << 4));
			b2 = Convert.ToByte(IsDisabledButton_Front);
			b = (byte)(b | (byte)(b2 << 3));
			b2 = Convert.ToByte(IsDisabledButton_Rear);
			b = (byte)(b | (byte)(b2 << 2));
			b2 = Convert.ToByte(IsDisabledButton_Left);
			b = (byte)(b | (byte)(b2 << 1));
			b2 = Convert.ToByte(IsDisabledButton_Right);
			b = (deviceStatus[2] = (byte)(b | b2));
			b = 0;
			b2 = Convert.ToByte(BlinkRateOnCount);
			b = (byte)(b | (byte)(b2 << 4));
			b2 = Convert.ToByte(BlinkRateOffCount);
			b = (deviceStatus[3] = (byte)(b | b2));
			b = 0;
			b2 = Convert.ToByte(Indicator_FrontLED);
			b = (byte)(b | (byte)(b2 << 6));
			b2 = Convert.ToByte(Indicator_RearLED);
			b = (byte)(b | (byte)(b2 << 4));
			b2 = Convert.ToByte(Indicator_LeftLED);
			b = (byte)(b | (byte)(b2 << 2));
			b2 = Convert.ToByte(Indicator_RightLED);
			b = (deviceStatus[4] = (byte)(b | b2));
			b = 0;
			b2 = Convert.ToByte(Indicator_LevelLED);
			b = (byte)(b | (byte)(b2 << 6));
			b2 = Convert.ToByte(Indicator_ExtendLED);
			b = (byte)(b | (byte)(b2 << 4));
			b2 = Convert.ToByte(Indicator_RetractLED);
			b = (byte)(b | (byte)(b2 << 2));
			b2 = Convert.ToByte(Indicator_Buzzer);
			b = (deviceStatus[5] = (byte)(b | b2));
			base.DeviceStatus = deviceStatus;
		}

		protected override void OnLocalDeviceRxEvent(AdapterRxEvent rx)
		{
			base.OnLocalDeviceRxEvent(rx);
			if ((byte)rx.MessageType == 130 && rx.TargetAddress == base.Address && rx.SourceAddress == GetLocalSessionClientAddress((ushort)4) && rx.Count == 3)
			{
				Command_Screen = (LEVELER_TYPE_3_SCREEN)rx[0];
				Command_Button_EnterSetup = (rx[1] & 0x10) != 0;
				Command_Button_AutoHitch = (rx[1] & 8) != 0;
				Command_Button_MenuUp = (rx[1] & 4) != 0;
				Command_Button_Back = (rx[1] & 2) != 0;
				Command_Button_Extend = (rx[1] & 1) != 0;
				Command_Button_MenuDown = (rx[2] & 0x80) != 0;
				Command_Button_Enter = (rx[2] & 0x40) != 0;
				Command_Button_Retract = (rx[2] & 0x20) != 0;
				Command_Button_AutoLevel = (rx[2] & 0x10) != 0;
				Command_Button_Front = (rx[2] & 8) != 0;
				Command_Button_Rear = (rx[2] & 4) != 0;
				Command_Button_Left = (rx[2] & 2) != 0;
				Command_Button_Right = (rx[2] & 1) != 0;
			}
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}
}
