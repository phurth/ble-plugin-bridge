using System;
using System.Text;

namespace IDS.Core.IDS_CAN.Devices
{
	public class LEVELER_TYPE_1 : LocalDevice
	{
		private LEVELER_TYPE_1_LED_STATE _indicator_FrontLED;

		private LEVELER_TYPE_1_LED_STATE _indicator_RearLED;

		private LEVELER_TYPE_1_LED_STATE _indicator_LeftLED;

		private LEVELER_TYPE_1_LED_STATE _indicator_RightLED;

		private LEVELER_TYPE_1_LED_STATE _indicator_LevelLED;

		private LEVELER_TYPE_1_LED_STATE _indicator_PowerLED;

		private LEVELER_TYPE_1_LED_STATE _indicator_RetractLED;

		private LEVELER_TYPE_1_LED_STATE _indicator_Buzzer;

		private static byte StateLines;

		public LEVELER_TYPE_1_LED_STATE Indicator_FrontLED
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

		public LEVELER_TYPE_1_LED_STATE Indicator_RearLED
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

		public LEVELER_TYPE_1_LED_STATE Indicator_LeftLED
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

		public LEVELER_TYPE_1_LED_STATE Indicator_RightLED
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

		public LEVELER_TYPE_1_LED_STATE Indicator_LevelLED
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

		public LEVELER_TYPE_1_LED_STATE Indicator_PowerLED
		{
			get
			{
				return _indicator_PowerLED;
			}
			set
			{
				if (_indicator_PowerLED != value)
				{
					_indicator_PowerLED = value;
					UpdateStatus();
				}
			}
		}

		public LEVELER_TYPE_1_LED_STATE Indicator_RetractLED
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

		public LEVELER_TYPE_1_LED_STATE Indicator_Buzzer
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

		public string Command { get; set; }

		public string ConsoleTextLine1 { get; set; }

		public string ConsoleTextLine2 { get; set; }

		public LEVELER_TYPE_1(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(new LocalProduct(adapter, mac, product_id, version, software_part_number), (byte)7, 0, (ushort)109, 1, 0, options)
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
			CAN.PAYLOAD deviceStatus = new CAN.PAYLOAD(3);
			uint num = (uint)(Convert.ToByte(Indicator_Buzzer) << 21);
			num |= (uint)(Convert.ToByte(Indicator_RetractLED) << 18);
			num |= (uint)(Convert.ToByte(Indicator_PowerLED) << 15);
			num |= (uint)(Convert.ToByte(Indicator_LeftLED) << 12);
			num |= (uint)(Convert.ToByte(Indicator_LevelLED) << 9);
			num |= (uint)(Convert.ToByte(Indicator_RearLED) << 6);
			num |= (uint)(Convert.ToByte(Indicator_RightLED) << 3);
			num |= Convert.ToByte(Indicator_FrontLED);
			deviceStatus[0] = (byte)(num >> 16);
			deviceStatus[1] = (byte)(num >> 8);
			deviceStatus[2] = (byte)num;
			base.DeviceStatus = deviceStatus;
		}

		public void TransmitTextMessage()
		{
			int num = 0;
			byte ext_data = 0;
			byte[] array = null;
			string text = ((StateLines <= 1) ? ConsoleTextLine1 : ConsoleTextLine2);
			switch (StateLines)
			{
			case 0:
			case 2:
				ext_data = StateLines;
				if (text != null)
				{
					array = Encoding.ASCII.GetBytes(text);
					if (array != null)
					{
						if (array.Length != 0)
						{
							num = ((array.Length >= 8) ? 8 : array.Length);
						}
						else
						{
							num = 1;
							array = Encoding.ASCII.GetBytes(" ");
						}
					}
				}
				StateLines++;
				break;
			case 1:
			case 3:
				ext_data = StateLines;
				if (text != null)
				{
					array = Encoding.ASCII.GetBytes(text);
					if (array != null)
					{
						if (array.Length != 0)
						{
							int num2 = 0;
							for (int i = 8; i < array.Length; i++)
							{
								array[num2++] = array[i];
							}
							num = ((num2 >= 8) ? 8 : num2);
						}
						else
						{
							num = 1;
							array = Encoding.ASCII.GetBytes(" ");
						}
					}
				}
				StateLines++;
				if (StateLines > 3)
				{
					StateLines = 0;
				}
				break;
			default:
				StateLines = 0;
				break;
			}
			if (num == 0)
			{
				return;
			}
			CAN.PAYLOAD payload = new CAN.PAYLOAD(num);
			for (int j = 0; j < payload.Length; j++)
			{
				if (array != null)
				{
					payload[j] = array[j];
				}
			}
			Transmit29((byte)131, ext_data, ADDRESS.BROADCAST, payload);
		}

		protected override void OnLocalDeviceRxEvent(AdapterRxEvent rx)
		{
			base.OnLocalDeviceRxEvent(rx);
			if ((byte)rx.MessageType != 130 || rx.TargetAddress != base.Address)
			{
				return;
			}
			TransmitTextMessage();
			if (rx.SourceAddress == GetLocalSessionClientAddress((ushort)4) && rx.Count == 2)
			{
				string text = string.Empty;
				ushort num = (ushort)((rx[0] << 8) | rx[1]);
				if (((uint)num & (true ? 1u : 0u)) != 0)
				{
					text += " RIGHT ";
				}
				if ((num & 2u) != 0)
				{
					text += " LEFT ";
				}
				if ((num & 4u) != 0)
				{
					text += " REAR ";
				}
				if ((num & 8u) != 0)
				{
					text += " FRONT ";
				}
				if ((num & 0x10u) != 0)
				{
					text += " AUTO_LEVEL ";
				}
				if ((num & 0x20u) != 0)
				{
					text += " RETRACT ";
				}
				if ((num & 0x40u) != 0)
				{
					text += " ENTER ";
				}
				if ((num & 0x80u) != 0)
				{
					text += " MENU_DOWN ";
				}
				if ((num & 0x100u) != 0)
				{
					text += " RESERVED ";
				}
				if ((num & 0x200u) != 0)
				{
					text += " ON_OFF ";
				}
				if ((num & 0x400u) != 0)
				{
					text += " MENU_UP ";
				}
				Command = text;
			}
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}
}
