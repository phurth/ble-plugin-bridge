using System;
using System.ComponentModel;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLightDimmableStatus : LogicalDeviceStatusPacketMutable, ILogicalDeviceStatus<LogicalDeviceLightDimmableStatusSerializable>, ILogicalDeviceStatus, IDeviceDataPacketMutable, IDeviceDataPacket, INotifyPropertyChanged
	{
		private const int MinimumStatusPacketSize = 8;

		public const uint LightModeByteIndex = 0u;

		public const uint MaxBrightnessByteIndex = 1u;

		public const uint DurationByteIndex = 2u;

		public const uint BrightnessByteIndex = 3u;

		public const uint CycleTime1MsbIndex = 4u;

		public const uint CycleTime1LsbIndex = 5u;

		public const uint CycleTime2MsbIndex = 6u;

		public const uint CycleTime2LsbIndex = 7u;

		public static readonly byte MinimumBrightnessValue = 1;

		public static readonly byte MaximumBrightnessValue = byte.MaxValue;

		public static readonly byte InfiniteTime = 0;

		public bool On
		{
			get
			{
				if (base.HasData)
				{
					return base.Data[0] > 0;
				}
				return false;
			}
		}

		public bool Off
		{
			get
			{
				if (base.HasData)
				{
					return base.Data[0] == 0;
				}
				return false;
			}
		}

		public DimmableLightMode Mode
		{
			get
			{
				try
				{
					return DimmableLightModeFromByte(base.Data[0]);
				}
				catch
				{
					return DimmableLightMode.Off;
				}
			}
		}

		public byte MaxBrightness => base.Data[1];

		public byte Duration => base.Data[2];

		public byte Brightness => base.Data[3];

		public int CycleTime1 => (base.Data[4] << 8) | base.Data[5];

		public int CycleTime2 => (base.Data[6] << 8) | base.Data[7];

		public LogicalDeviceLightDimmableStatus()
			: base(8u)
		{
		}

		public LogicalDeviceLightDimmableStatus(LogicalDeviceLightDimmableStatus originalStatus)
		{
			byte[] data = originalStatus.Data;
			Update(data, data.Length);
		}

		public LogicalDeviceLightDimmableStatusSerializable CopyAsSerializable()
		{
			return new LogicalDeviceLightDimmableStatusSerializable(this);
		}

		public override string ToString()
		{
			return $"Mode: {Mode}, MaxBrightness: {MaxBrightness}, Duration: {Duration}, Brightness: {Brightness}, CycleTime1: {CycleTime1}, CycleTime2: {CycleTime2}";
		}

		public static DimmableLightMode DimmableLightModeFromByte(byte rawValue)
		{
			return rawValue switch
			{
				0 => DimmableLightMode.Off, 
				1 => DimmableLightMode.On, 
				2 => DimmableLightMode.Blink, 
				3 => DimmableLightMode.Swell, 
				_ => throw new ArgumentOutOfRangeException($"Unknown DimmableLightMode value 0x{rawValue:x2}"), 
			};
		}

		public void SetLightMode(DimmableLightMode mode)
		{
			SetByte(byte.MaxValue, (byte)mode, 0);
		}

		public void SetMaxBrightness(byte maxBrightness)
		{
			SetByte(byte.MaxValue, maxBrightness, 1);
		}

		public void SetDuration(byte duration)
		{
			SetByte(byte.MaxValue, duration, 2);
		}

		public void SetBrightness(byte brightness)
		{
			SetByte(byte.MaxValue, brightness, 3);
		}

		public void SetCycleTime1(int cycleTime)
		{
			byte value = (byte)((cycleTime & 0xFF00) >> 8);
			byte value2 = (byte)((uint)cycleTime & 0xFFu);
			SetByte(byte.MaxValue, value, 4);
			SetByte(byte.MaxValue, value2, 5);
		}

		public void SetCycleTime2(int cycleTime)
		{
			byte value = (byte)((cycleTime & 0xFF00) >> 8);
			byte value2 = (byte)((uint)cycleTime & 0xFFu);
			SetByte(byte.MaxValue, value, 6);
			SetByte(byte.MaxValue, value2, 7);
		}
	}
}
