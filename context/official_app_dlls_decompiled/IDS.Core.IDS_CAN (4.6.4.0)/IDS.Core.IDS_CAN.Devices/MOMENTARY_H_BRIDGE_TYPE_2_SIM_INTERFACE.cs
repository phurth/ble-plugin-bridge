using System;
using IDS.Core.Types;

namespace IDS.Core.IDS_CAN.Devices
{
	public class MOMENTARY_H_BRIDGE_TYPE_2_SIM_INTERFACE : RELAY_TYPE_2_SIM_INTERFACE
	{
		public enum COMMAND_MODE : byte
		{
			STOP,
			FORWARD,
			REVERSE,
			CLEAR_OUTPUT_DISABLED_LATCH,
			HOME_RESET,
			AUTO_FORWARD,
			AUTO_REVERSE
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
					if (!base.OutputDisabledLatch && (base.OutputState == RELAY_TYPE_2_OUTPUT_STATE.FORWARD_EXTEND_CLOCKWISE_OUT_UP || base.OutputState == RELAY_TYPE_2_OUTPUT_STATE.REVERSE_RETRACT_COUNTERCLOCKWISE_IN_DOWN))
					{
						base.OutputState = RELAY_TYPE_2_OUTPUT_STATE.OFF_STOP;
					}
					break;
				case 1:
					if (!base.OutputDisabledLatch && (base.OutputState == RELAY_TYPE_2_OUTPUT_STATE.OFF_STOP || base.OutputState == RELAY_TYPE_2_OUTPUT_STATE.REVERSE_RETRACT_COUNTERCLOCKWISE_IN_DOWN) && base.ForwardCommandAllowed)
					{
						base.OutputState = RELAY_TYPE_2_OUTPUT_STATE.FORWARD_EXTEND_CLOCKWISE_OUT_UP;
					}
					break;
				case 2:
					if (!base.OutputDisabledLatch && (base.OutputState == RELAY_TYPE_2_OUTPUT_STATE.OFF_STOP || base.OutputState == RELAY_TYPE_2_OUTPUT_STATE.FORWARD_EXTEND_CLOCKWISE_OUT_UP) && base.ReverseCommandAllowed)
					{
						base.OutputState = RELAY_TYPE_2_OUTPUT_STATE.REVERSE_RETRACT_COUNTERCLOCKWISE_IN_DOWN;
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

		public MOMENTARY_H_BRIDGE_TYPE_2_SIM_INTERFACE(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(adapter, software_part_number, product_id, version, new DEVICE_ID(product_id, 0, (byte)33, 0, (ushort)105, 0, 0), options)
		{
			Init();
			AddPID(PID.EXTENDED_DEVICE_CAPABILITIES, () => base.EXTENDEDDEVICECAPABILITIES, delegate(UInt48 arg)
			{
				base.EXTENDEDDEVICECAPABILITIES = (byte)arg;
			});
			AddPID(PID.CLOUD_CAPABILITIES, () => base.CLOUDCAPABILITIES, delegate(UInt48 arg)
			{
				base.CLOUDCAPABILITIES = (byte)arg;
			});
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
			b2 = Convert.ToByte(base.Supports_Homing);
			b = (byte)(b | (byte)(b2 << 5));
			base.DeviceCapabilities = b;
		}

		protected override void UpdateStatus()
		{
			CAN.PAYLOAD deviceStatus = new CAN.PAYLOAD(6);
			byte b = 0;
			byte b2 = Convert.ToByte(base.OutputState);
			b = (byte)(b | b2);
			b2 = Convert.ToByte(base.OutputDisabledLatch);
			b = (byte)(b | (byte)(b2 << 5));
			b2 = Convert.ToByte(base.ReverseCommandAllowed);
			b = (byte)(b | (byte)(b2 << 6));
			b2 = Convert.ToByte(base.ForwardCommandAllowed);
			b = (deviceStatus[0] = (byte)(b | (byte)(b2 << 7)));
			deviceStatus[1] = base.OutputPositionPct;
			uint num = (uint)Math.Round(base.CurrentDraw * 256f);
			if (num > 65535)
			{
				num = 65535u;
			}
			if (num < 0)
			{
				num = 0u;
			}
			deviceStatus[2] = (byte)(num >> 8);
			deviceStatus[3] = (byte)num;
			if (base.OutputDisabledLatch)
			{
				deviceStatus[4] = (byte)(base.UserMessage >> 8);
				deviceStatus[5] = (byte)base.UserMessage;
			}
			base.DeviceStatus = deviceStatus;
		}

		protected override void OnLocalDeviceRxEvent(AdapterRxEvent rx)
		{
			base.OnLocalDeviceRxEvent(rx);
			if (rx.TargetAddress != base.Address)
			{
				return;
			}
			if ((byte)rx.MessageType == 130)
			{
				if (rx.SourceAddress == GetLocalSessionClientAddress((ushort)4) && rx.Count == 0)
				{
					Command = rx.MessageData;
				}
			}
			else if ((byte)rx.MessageType == 128 && (byte)(REQUEST)rx.MessageData == 69 && GetLocalSession((ushort)4)?.Client?.Address == rx.SourceAddress && (base.OutputState == RELAY_TYPE_2_OUTPUT_STATE.FORWARD_EXTEND_CLOCKWISE_OUT_UP || base.OutputState == RELAY_TYPE_2_OUTPUT_STATE.REVERSE_RETRACT_COUNTERCLOCKWISE_IN_DOWN))
			{
				base.OutputState = RELAY_TYPE_2_OUTPUT_STATE.OFF_STOP;
			}
		}
	}
}
