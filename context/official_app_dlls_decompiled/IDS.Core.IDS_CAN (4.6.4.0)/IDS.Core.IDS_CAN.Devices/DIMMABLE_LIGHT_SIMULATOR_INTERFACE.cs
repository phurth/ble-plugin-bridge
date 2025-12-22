using System;

namespace IDS.Core.IDS_CAN.Devices
{
	public class DIMMABLE_LIGHT_SIMULATOR_INTERFACE : LocalDevice
	{
		private DIMMABLE_LIGHT_STATUS_PARAMS mStatus;

		private Timer DimTimer = new Timer();

		private Timer SleepTimer = new Timer();

		private DIMMABLE_MODE LastActiveMode = DIMMABLE_MODE.ON_01;

		private TimeSpan AutoOffTimeThreshold = TimeSpan.FromMinutes(255.0);

		private DIMMABLE_MODE Simulator_Mode
		{
			get
			{
				return (DIMMABLE_MODE)mStatus.Mode;
			}
			set
			{
				mStatus.Mode = (byte)value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		private byte Simulator_MaxBrightness
		{
			get
			{
				return mStatus.MaxBrightness;
			}
			set
			{
				mStatus.MaxBrightness = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		private byte Simulator_AutoOffTime
		{
			get
			{
				return mStatus.AutoOffTimeStatus;
			}
			set
			{
				mStatus.AutoOffTimeStatus = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		private byte Simulator_CurrentBrightness
		{
			get
			{
				return mStatus.CurrentBrightness;
			}
			set
			{
				mStatus.CurrentBrightness = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		private ushort Simulator_T1
		{
			get
			{
				return mStatus.T1;
			}
			set
			{
				mStatus.T1 = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		private ushort Simulator_T2
		{
			get
			{
				return mStatus.T2;
			}
			set
			{
				mStatus.T2 = value;
				base.DeviceStatus = mStatus.GetPayload();
			}
		}

		public DIMMABLE_LIGHT_SIMULATOR_INTERFACE(IAdapter adapter, FUNCTION_NAME name, string software_part_number, PRODUCT_ID product_id, IDS_CAN_VERSION_NUMBER version, LOCAL_DEVICE_OPTIONS options, MAC mac = null)
			: base(new LocalProduct(adapter, mac, product_id, version, software_part_number), (byte)20, 0, name, 0, 0, options)
		{
			mStatus = new DIMMABLE_LIGHT_STATUS_PARAMS();
			base.DeviceStatus = mStatus.GetPayload();
		}

		protected override void OnLocalDeviceRxEvent(AdapterRxEvent rx)
		{
			base.OnLocalDeviceRxEvent(rx);
			if (!base.IsDisposed && base.Address.IsValidDeviceAddress && rx.TargetAddress == base.Address && (byte)rx.MessageType == 130 && rx.Count == 8 && rx.SourceAddress == GetLocalSessionClientAddress((ushort)4))
			{
				Simulator_Mode = UnloadCommandMessage(rx);
				if (Simulator_Mode != 0 && Simulator_Mode != DIMMABLE_MODE.RESTORE_7F)
				{
					LastActiveMode = Simulator_Mode;
				}
			}
		}

		private DIMMABLE_MODE UnloadCommandMessage(AdapterRxEvent rx)
		{
			DIMMABLE_MODE dIMMABLE_MODE = (DIMMABLE_MODE)rx[0];
			switch (dIMMABLE_MODE)
			{
			case DIMMABLE_MODE.OFF_00:
				return DIMMABLE_MODE.OFF_00;
			case DIMMABLE_MODE.RESTORE_7F:
				if (Simulator_Mode == DIMMABLE_MODE.OFF_00)
				{
					SleepTimer.Reset();
				}
				return LastActiveMode;
			default:
				if (rx[1] == 0)
				{
					return DIMMABLE_MODE.OFF_00;
				}
				if (Simulator_MaxBrightness != rx[1])
				{
					Simulator_MaxBrightness = rx[1];
				}
				if (rx[2] != 0)
				{
					AutoOffTimeThreshold = TimeSpan.FromMinutes((int)rx[2]);
					SleepTimer.Reset();
				}
				if (dIMMABLE_MODE == DIMMABLE_MODE.BLINK_02 || dIMMABLE_MODE == DIMMABLE_MODE.SWELL_TRIANGLE_03)
				{
					Simulator_T1 = (ushort)((rx[3] << 8) | rx[4]);
					Simulator_T2 = (ushort)((rx[5] << 8) | rx[6]);
				}
				return dIMMABLE_MODE;
			}
		}

		protected override void OnBackgroundTask()
		{
			base.OnBackgroundTask();
			if (Simulator_Mode != 0 && AutoOffTimeThreshold.Ticks > 0 && Simulator_AutoOffTime > 0 && SleepTimer.ElapsedTime >= AutoOffTimeThreshold)
			{
				Simulator_Mode = DIMMABLE_MODE.OFF_00;
			}
			double num = DimTimer.ElapsedTime.TotalSeconds;
			if (num >= (double)(Simulator_T1 + Simulator_T2))
			{
				DimTimer.Reset();
				num = 0.0;
			}
			switch (Simulator_Mode)
			{
			case DIMMABLE_MODE.OFF_00:
				Simulator_CurrentBrightness = 0;
				break;
			case DIMMABLE_MODE.ON_01:
				Simulator_CurrentBrightness = Simulator_MaxBrightness;
				break;
			case DIMMABLE_MODE.BLINK_02:
				Simulator_CurrentBrightness = (byte)((num <= (double)(int)Simulator_T1) ? Simulator_MaxBrightness : 0);
				break;
			case DIMMABLE_MODE.SWELL_TRIANGLE_03:
				if (num < (double)(int)Simulator_T1)
				{
					Simulator_CurrentBrightness = (byte)((double)(int)Simulator_MaxBrightness * num / (double)(int)Simulator_T1);
					break;
				}
				num -= (double)(int)Simulator_T1;
				Simulator_CurrentBrightness = (byte)((double)(int)Simulator_MaxBrightness * ((double)(int)Simulator_T2 - num) / (double)(int)Simulator_T2);
				break;
			}
			if (Simulator_Mode == DIMMABLE_MODE.OFF_00)
			{
				Simulator_AutoOffTime = 0;
				return;
			}
			if (AutoOffTimeThreshold.Ticks == 0L)
			{
				Simulator_AutoOffTime = 0;
				return;
			}
			int num2 = (int)(AutoOffTimeThreshold - SleepTimer.ElapsedTime).TotalMinutes;
			if (num2 <= 0)
			{
				num2 = 0;
			}
			if (num2 >= 254)
			{
				num2 = 254;
			}
			Simulator_AutoOffTime = (byte)num2;
		}
	}
}
