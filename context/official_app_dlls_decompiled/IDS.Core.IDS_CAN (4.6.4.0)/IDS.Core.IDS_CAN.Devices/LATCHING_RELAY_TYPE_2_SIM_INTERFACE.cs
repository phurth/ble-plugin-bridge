using System;

namespace IDS.Core.IDS_CAN.Devices
{
	public class LATCHING_RELAY_TYPE_2_SIM_INTERFACE : RELAY_TYPE_2_SIM_INTERFACE
	{
		public enum COMMAND_MODE : byte
		{
			OFF = 0,
			ON = 1,
			CLEAR_OUTPUT_DISABLED_LATCH = 3
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

		public RELAY_TYPE_2_STATUS_PARAMS StatusParams;

		private COMMAND _command = 0;

		private COMMAND Command
		{
			get
			{
				return _command;
			}
			set
			{
				_command = value;
				switch ((byte)_command)
				{
				case 0:
					if (!base.OutputDisabledLatch && base.OutputState == RELAY_TYPE_2_OUTPUT_STATE.ON)
					{
						base.OutputState = RELAY_TYPE_2_OUTPUT_STATE.OFF_STOP;
					}
					break;
				case 1:
					if (!base.OutputDisabledLatch && base.OutputState == RELAY_TYPE_2_OUTPUT_STATE.OFF_STOP && base.OnCommandAllowed)
					{
						base.OutputState = RELAY_TYPE_2_OUTPUT_STATE.ON;
					}
					break;
				case 3:
					if (base.OutputDisabledLatch)
					{
						base.OutputDisabledLatch = false;
					}
					break;
				}
				UpdateStatus();
			}
		}

		public LATCHING_RELAY_TYPE_2_SIM_INTERFACE(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(adapter, software_part_number, product_id, version, new DEVICE_ID(product_id, 0, (byte)30, 0, (ushort)7, 0, 0), options)
		{
			StatusParams = new RELAY_TYPE_2_STATUS_PARAMS();
			Init();
		}

		protected override void Init()
		{
			UpdateStatus();
		}

		protected override void UpdateDeviceCapabilities()
		{
			byte b = 0;
			byte b2 = Convert.ToByte(base.Supports_SoftwareConfigurableFuse);
			b = (byte)(b | b2);
			b2 = Convert.ToByte(base.Supports_CoarsePosition);
			b = (byte)(b | (byte)(b2 << 1));
			b2 = Convert.ToByte(base.Supports_FinePosition);
			b = (byte)(b | (byte)(b2 << 2));
			b2 = Convert.ToByte(base.PhysicalSwitchType);
			b = (byte)(b | (byte)(b2 << 3));
			base.DeviceCapabilities = b;
		}

		protected override void UpdateStatus()
		{
			StatusParams._OutputState = (byte)base.OutputState;
			StatusParams.OutputPositionPct = base.OutputPositionPct;
			StatusParams.CurrentDraw = (ushort)Math.Round(base.CurrentDraw * 256f);
			StatusParams.UserMessage = base.UserMessage;
			base.DeviceStatus = StatusParams.GetPayload();
		}

		protected void UpdateFromPayload(CAN.PAYLOAD pl)
		{
			StatusParams.SetPayload(pl);
		}

		protected override void OnLocalDeviceRxEvent(AdapterRxEvent rx)
		{
			base.OnLocalDeviceRxEvent(rx);
			if ((byte)rx.MessageType == 130 && rx.TargetAddress == base.Address && rx.SourceAddress == GetLocalSessionClientAddress((ushort)4) && rx.Count == 0)
			{
				Command = rx.MessageData;
			}
		}
	}
}
