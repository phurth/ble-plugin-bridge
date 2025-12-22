using System;
using IDS.Core.Types;

namespace IDS.Core.IDS_CAN.Devices
{
	public class DC_POWER_MONITOR : LocalDevice
	{
		private float _measuredVoltage;

		private float _measuredCurrent;

		private byte _chargingCapacity;

		private bool _isDischarging;

		private ushort _estimatedTimeToDischarge;

		public uint BATTERY_CAPACITY_AMP_HOURS { get; set; }

		public float MeasuredVoltage
		{
			get
			{
				return _measuredVoltage;
			}
			set
			{
				if (_measuredVoltage != value)
				{
					_measuredVoltage = value;
					UpdateStatus();
				}
			}
		}

		public float MeasuredCurrent
		{
			get
			{
				return _measuredCurrent;
			}
			set
			{
				if (_measuredCurrent != value)
				{
					_measuredCurrent = value;
					UpdateStatus();
				}
			}
		}

		public byte ChargingCapacity
		{
			get
			{
				return _chargingCapacity;
			}
			set
			{
				if (_chargingCapacity != value)
				{
					_chargingCapacity = value;
					UpdateStatus();
				}
			}
		}

		public bool IsDischarging
		{
			get
			{
				return _isDischarging;
			}
			set
			{
				if (_isDischarging != value)
				{
					_isDischarging = value;
					UpdateStatus();
				}
			}
		}

		public ushort EstimatedTimeToDischarge
		{
			get
			{
				return _estimatedTimeToDischarge;
			}
			set
			{
				if (_estimatedTimeToDischarge != value)
				{
					_estimatedTimeToDischarge = value;
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

		public DC_POWER_MONITOR(IAdapter adapter, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(new LocalProduct(adapter, mac, product_id, version, software_part_number), (byte)27, 0, (ushort)255, 0, 0, options)
		{
			Init();
		}

		private void Init()
		{
			AddPID(PID.BATTERY_CAPACITY_AMP_HOURS, () => BATTERY_CAPACITY_AMP_HOURS, delegate(UInt48 arg)
			{
				BATTERY_CAPACITY_AMP_HOURS = (uint)arg;
			});
			IsDischarging = true;
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			int num = (int)Math.Round(MeasuredVoltage * 256f);
			if (num > int.MaxValue)
			{
				num = 32767;
			}
			if (num < int.MinValue)
			{
				num = -32768;
			}
			int num2 = (int)Math.Round(MeasuredCurrent * 256f);
			if (num2 > int.MaxValue)
			{
				num2 = int.MaxValue;
			}
			if (num2 < int.MinValue)
			{
				num2 = int.MinValue;
			}
			byte b = ChargingCapacity;
			if (!IsDischarging)
			{
				b = (byte)(b | 0x80u);
			}
			base.DeviceStatus = CAN.PAYLOAD.FromArgs((ushort)num, (ushort)num2, b, EstimatedTimeToDischarge);
		}
	}
}
