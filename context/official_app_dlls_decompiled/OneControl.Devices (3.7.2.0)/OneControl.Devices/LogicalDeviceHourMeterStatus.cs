using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceHourMeterStatus : LogicalDeviceStatusPacketMutable, IReadOnlyLogicalDeviceHourMeterStatus
	{
		private const int MinimumStatusPacketSize = 5;

		public const BasicBitMask RunningStateBit = BasicBitMask.BitMask0X01;

		public const BasicBitMask MaintenanceDueStateBit = BasicBitMask.BitMask0X02;

		public const BasicBitMask MaintenancePastDueStateBit = BasicBitMask.BitMask0X04;

		public const BasicBitMask StoppingStateBit = BasicBitMask.BitMask0X08;

		public const BasicBitMask StartingStateBit = BasicBitMask.BitMask0X10;

		public const BasicBitMask ErrorStateBit = BasicBitMask.BitMask0X20;

		public const BasicBitMask HourMeterInvalidBit = BasicBitMask.BitMask0X80;

		public const int OperatingSecondsIndex = 0;

		public const int StatusByteIndex = 4;

		public bool Running => GetBit(BasicBitMask.BitMask0X01, 4);

		public bool MaintenanceDue => GetBit(BasicBitMask.BitMask0X02, 4);

		public bool MaintenancePastDue => GetBit(BasicBitMask.BitMask0X04, 4);

		public bool Stopping => GetBit(BasicBitMask.BitMask0X08, 4);

		public bool Starting => GetBit(BasicBitMask.BitMask0X10, 4);

		public bool Error => GetBit(BasicBitMask.BitMask0X20, 4);

		public bool IsHourMeterValid => !GetBit(BasicBitMask.BitMask0X80, 4);

		public uint OperatingSeconds
		{
			get
			{
				byte[] data = base.Data;
				if (data == null || data.Length < 5)
				{
					return 0u;
				}
				return GetUInt32(0u);
			}
		}

		public LogicalDeviceHourMeterStatus()
			: base(5u)
		{
		}

		public void SetRunning(bool isRunning)
		{
			SetBit(BasicBitMask.BitMask0X01, isRunning);
		}

		public void SetMaintenanceDue(bool isMaintenanceDue)
		{
			SetBit(BasicBitMask.BitMask0X02, isMaintenanceDue);
		}

		public void SetMaintenancePastDue(bool isMaintenancePastDue)
		{
			SetBit(BasicBitMask.BitMask0X04, isMaintenancePastDue);
		}

		public void SetStopping(bool isStopping)
		{
			SetBit(BasicBitMask.BitMask0X08, isStopping);
		}

		public void SetStarting(bool isStarting)
		{
			SetBit(BasicBitMask.BitMask0X10, isStarting);
		}

		public void SetError(bool isError)
		{
			SetBit(BasicBitMask.BitMask0X20, isError);
		}

		public void SetHourMeterValid(bool isValid)
		{
			SetBit(BasicBitMask.BitMask0X80, !isValid);
		}

		public void SetOperatingSeconds(uint cycleTime)
		{
			SetUInt32(cycleTime, 0);
		}
	}
}
