using System;
using IDS.Core.Types;

namespace IDS.Core.IDS_CAN.Devices
{
	public class GENERATOR_GENIE : LocalDevice
	{
		public enum COMMAND_MODE : byte
		{
			OFF = 1,
			ON,
			PRIME
		}

		private struct COMMAND
		{
			private int Value;

			public COMMAND_MODE Option
			{
				get
				{
					return (COMMAND_MODE)GetBits(3, 6);
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

		private GENERATOR_GENIE_STATE _state;

		private bool _hasTemperatureSensor;

		private bool _isTemperatureSensorNotSupported;

		private float _batteryVoltage;

		private float _temperature_C;

		private COMMAND _command = 0;

		public ushort GENERATOR_QUIET_HOURS_START_TIME { get; set; }

		public ushort GENERATOR_QUIET_HOURS_END_TIME { get; set; }

		public byte GENERATOR_QUIET_HOURS_ENABLED { get; set; }

		public int GENERATOR_AUTO_START_LOW_VOLTAGE { get; set; }

		public int GENERATOR_AUTO_START_HI_TEMP_C { get; set; }

		public ushort GENERATOR_AUTO_RUN_DURATION_MINUTES { get; set; }

		public ushort GENERATOR_AUTO_RUN_MIN_OFF_TIME_MINUTES { get; set; }

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

		public GENERATOR_GENIE_STATE State
		{
			get
			{
				return _state;
			}
			set
			{
				if (_state != value)
				{
					_state = value;
					UpdateStatus();
				}
			}
		}

		public bool HasTemperatureSensor
		{
			get
			{
				return _hasTemperatureSensor;
			}
			set
			{
				if (_hasTemperatureSensor != value)
				{
					_hasTemperatureSensor = value;
					UpdateStatus();
				}
			}
		}

		public bool IsTemperatureSensorNotSupported
		{
			get
			{
				return _isTemperatureSensorNotSupported;
			}
			set
			{
				if (_isTemperatureSensorNotSupported != value)
				{
					_isTemperatureSensorNotSupported = value;
					UpdateStatus();
				}
			}
		}

		public float BatteryVoltage
		{
			get
			{
				return _batteryVoltage;
			}
			set
			{
				if (_batteryVoltage != value)
				{
					_batteryVoltage = value;
					UpdateStatus();
				}
			}
		}

		public float Temperature_C
		{
			get
			{
				return _temperature_C;
			}
			set
			{
				if (_temperature_C != value)
				{
					_temperature_C = value;
					UpdateStatus();
				}
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
					case 1:
						State = GENERATOR_GENIE_STATE.OFF;
						break;
					case 2:
						State = GENERATOR_GENIE_STATE.STARTING;
						break;
					case 3:
						State = GENERATOR_GENIE_STATE.PRIMING;
						break;
					}
					UpdateStatus();
				}
			}
		}

		public GENERATOR_GENIE(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(new LocalProduct(adapter, mac, product_id, version, software_part_number), (byte)24, 0, (ushort)95, 0, 0, options)
		{
			Init();
		}

		private void Init()
		{
			BatteryVoltage = 0f;
			AddPID(PID.GENERATOR_QUIET_HOURS_START_TIME, () => GENERATOR_QUIET_HOURS_START_TIME, delegate(UInt48 arg)
			{
				GENERATOR_QUIET_HOURS_START_TIME = (ushort)arg;
			});
			AddPID(PID.GENERATOR_QUIET_HOURS_END_TIME, () => GENERATOR_QUIET_HOURS_END_TIME, delegate(UInt48 arg)
			{
				GENERATOR_QUIET_HOURS_END_TIME = (ushort)arg;
			});
			AddPID(PID.GENERATOR_QUIET_HOURS_ENABLED, () => GENERATOR_QUIET_HOURS_ENABLED, delegate(UInt48 arg)
			{
				GENERATOR_QUIET_HOURS_ENABLED = (byte)arg;
			});
			AddPID(PID.GENERATOR_AUTO_START_LOW_VOLTAGE, () => (uint)GENERATOR_AUTO_START_LOW_VOLTAGE, delegate(UInt48 arg)
			{
				GENERATOR_AUTO_START_LOW_VOLTAGE = (int)arg;
			});
			AddPID(PID.GENERATOR_AUTO_START_HI_TEMP_C, () => (uint)GENERATOR_AUTO_START_HI_TEMP_C, delegate(UInt48 arg)
			{
				GENERATOR_AUTO_START_HI_TEMP_C = (int)arg;
			});
			AddPID(PID.GENERATOR_AUTO_RUN_DURATION_MINUTES, () => GENERATOR_AUTO_RUN_DURATION_MINUTES, delegate(UInt48 arg)
			{
				GENERATOR_AUTO_RUN_DURATION_MINUTES = (ushort)arg;
			});
			AddPID(PID.GENERATOR_AUTO_RUN_MIN_OFF_TIME_MINUTES, () => GENERATOR_AUTO_RUN_MIN_OFF_TIME_MINUTES, delegate(UInt48 arg)
			{
				GENERATOR_AUTO_RUN_MIN_OFF_TIME_MINUTES = (ushort)arg;
			});
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			int num = (int)Math.Round(BatteryVoltage * 256f);
			if (num > 32767)
			{
				num = 32767;
			}
			if (num < -32768)
			{
				num = -32768;
			}
			int num2 = (IsTemperatureSensorNotSupported ? 32768 : ((!HasTemperatureSensor) ? 32767 : ((int)Math.Round(Temperature_C * 256f))));
			base.DeviceStatus = CAN.PAYLOAD.FromArgs((byte)State, (short)num, (short)num2);
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
