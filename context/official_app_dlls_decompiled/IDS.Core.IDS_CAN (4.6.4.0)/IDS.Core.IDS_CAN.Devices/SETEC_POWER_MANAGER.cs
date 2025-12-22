namespace IDS.Core.IDS_CAN.Devices
{
	public class SETEC_POWER_MANAGER : LocalDevice
	{
		public enum COMMAND_OPERATING_MODE : byte
		{
			OFF,
			ON
		}

		private struct COMMAND
		{
			private int Value;

			public COMMAND_OPERATING_MODE Option
			{
				get
				{
					return (COMMAND_OPERATING_MODE)GetBits(3, 6);
				}
				set
				{
					SetBits((int)value, 3, 6);
				}
			}

			private bool GetBit(byte bit)
			{
				return (Value & bit) != 0;
			}

			private void SetBit(bool val, byte bit)
			{
				if (val)
				{
					Value |= bit;
				}
				else
				{
					Value &= ~bit;
				}
			}

			private int GetBits(int bit, int shift)
			{
				return (Value >> shift) & bit;
			}

			private void SetBits(int val, int bit, int shift)
			{
				val <<= shift;
				bit <<= shift;
				Value = (Value & ~bit) | (val & bit);
			}

			public static implicit operator int(COMMAND s)
			{
				return s.Value;
			}

			public static implicit operator byte(COMMAND s)
			{
				return (byte)s.Value;
			}

			public static implicit operator COMMAND(byte b)
			{
				COMMAND result = default(COMMAND);
				result.Value = b;
				return result;
			}

			public static implicit operator COMMAND(int i)
			{
				COMMAND result = default(COMMAND);
				result.Value = i;
				return result;
			}
		}

		private SETEC_POWER_MANAGER_OPERATING_MODE _operatingMode;

		private COMMAND _command = 0;

		public SETEC_POWER_MANAGER_OPERATING_MODE OperatingMode
		{
			get
			{
				return _operatingMode;
			}
			set
			{
				if (_operatingMode != value)
				{
					_operatingMode = value;
					UpdateStatus();
				}
			}
		}

		public bool NotAcceptingCommands
		{
			get
			{
				return base.IsNotAcceptingCommands;
			}
			set
			{
				base.IsNotAcceptingCommands = value;
			}
		}

		private COMMAND Command
		{
			get
			{
				return _command;
			}
			set
			{
				if ((int)_command != (int)value)
				{
					_command = value;
					switch ((byte)_command)
					{
					case 0:
						OperatingMode = SETEC_POWER_MANAGER_OPERATING_MODE.OFF;
						break;
					case 1:
						OperatingMode = SETEC_POWER_MANAGER_OPERATING_MODE.ON;
						break;
					}
					UpdateStatus();
				}
			}
		}

		public SETEC_POWER_MANAGER(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(new LocalProduct(adapter, mac, product_id, version, software_part_number), (byte)28, 0, (ushort)262, 0, 0, options)
		{
			Init();
		}

		private void Init()
		{
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			base.DeviceStatus = CAN.PAYLOAD.FromArgs((byte)OperatingMode);
		}

		protected override void OnLocalDeviceRxEvent(AdapterRxEvent rx)
		{
			base.OnLocalDeviceRxEvent(rx);
			if ((byte)rx.MessageType == 130 && rx.TargetAddress == base.Address && rx.SourceAddress == GetLocalSessionClientAddress((ushort)4) && rx.Count == 1)
			{
				Command = rx[0];
			}
		}
	}
}
