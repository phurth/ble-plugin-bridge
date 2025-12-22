using System;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLightDimmableStatusExtended : LogicalDeviceStatusPacketMutableExtended
	{
		public const int MinimumStatusPacketSize = 8;

		public const int SavedSleepTimerBitMask = 255;

		public const int SavedSleepTimerInvalidOrUnknown = 255;

		public const int SavedSleepTimerIndex = 0;

		public const int SavedSleepTimerMin = 0;

		public const int SavedSleepTimerMax = 254;

		public const byte RawBufferFill = byte.MaxValue;

		public TimeSpan? SavedSleepTimer
		{
			get
			{
				if (!base.HasData)
				{
					return null;
				}
				byte @byte = GetByte(byte.MaxValue, 0);
				if (@byte == byte.MaxValue)
				{
					return null;
				}
				return TimeSpan.FromMinutes((int)@byte);
			}
		}

		public LogicalDeviceLightDimmableStatusExtended()
			: base(8u, 8u, byte.MaxValue)
		{
		}

		public LogicalDeviceLightDimmableStatusExtended(LogicalDeviceLightDimmableStatusExtended originalStatus)
		{
			byte[] data = originalStatus.Data;
			Update(data, (uint)data.Length, originalStatus.ExtendedByte);
		}

		public void SetMinutesRemainingUntilDischarged(TimeSpan timeSpan)
		{
			byte value = (byte)MathCommon.Clamp((int)timeSpan.TotalMinutes, 0, 254);
			SetByte(byte.MaxValue, value, 0);
		}
	}
}
