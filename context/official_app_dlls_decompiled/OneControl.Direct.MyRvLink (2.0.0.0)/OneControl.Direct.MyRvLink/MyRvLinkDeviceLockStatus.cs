using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common.Extensions;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceLockStatus : MyRvLinkEventDevicesSubByte<MyRvLinkDeviceLockStatus>
	{
		[Flags]
		private enum MyRvLinkDeviceChassisStatus : byte
		{
			None = 0,
			ParkBreakReleased = 1,
			ParkBreakEngaged = 2,
			IgnitionOn = 4,
			IgnitionOff = 8,
			ParkingBreakMask = 3,
			IgnitionPowerMask = 0xC
		}

		private const int SystemLockoutLevelIndex = 1;

		private const int ChassisInfoIndex = 2;

		private const int TowableInfoIndex = 3;

		private const int TowableBatteryVoltageIndex = 4;

		private const int TowableBrakeVoltageIndex = 5;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.DeviceLockStatus;

		protected override MyRvLinkEventDevicesSubByte<MyRvLinkDeviceLockStatus>.AllowedDevicesPerByte DevicesPerByte => MyRvLinkEventDevicesSubByte<MyRvLinkDeviceLockStatus>.AllowedDevicesPerByte.Eight;

		protected override int MinPayloadLength => 8;

		public byte StartDeviceId { get; }

		public LogicalDeviceChassisInfoStatus ChassisInfoStatus { get; }

		protected override int DeviceTableIdIndex => 6;

		protected override int DeviceCountIndex => 7;

		protected override int DeviceStatusStartIndex => 8;

		public byte SystemLockoutLevel => _rawData[1];

		public MyRvLinkIgnitionStatus IgnitionStatus => ChassisInfoStatus.IgnitionPowerSignal switch
		{
			IgnitionPowerSignal.On => MyRvLinkIgnitionStatus.On, 
			IgnitionPowerSignal.Off => MyRvLinkIgnitionStatus.Off, 
			IgnitionPowerSignal.Unknown => MyRvLinkIgnitionStatus.Unknown, 
			IgnitionPowerSignal.Reserved => MyRvLinkIgnitionStatus.Unknown, 
			_ => MyRvLinkIgnitionStatus.Unknown, 
		};

		public MyRvLinkParkingBreakStatus ParkingBreakStatus => ChassisInfoStatus.ParkBreak switch
		{
			ParkBrake.Engaged => MyRvLinkParkingBreakStatus.Engaged, 
			ParkBrake.Released => MyRvLinkParkingBreakStatus.Released, 
			ParkBrake.Unknown => MyRvLinkParkingBreakStatus.Unknown, 
			ParkBrake.Reserved => MyRvLinkParkingBreakStatus.Unknown, 
			_ => MyRvLinkParkingBreakStatus.Unknown, 
		};

		public MyRvLinkDeviceLockStatus(byte deviceTableId, byte deviceCount, byte lockoutLevel, MyRvLinkParkingBreakStatus parkingBreakStatus, MyRvLinkIgnitionStatus ignitionStatus)
			: base(deviceTableId, (int)deviceCount)
		{
			_rawData[1] = lockoutLevel;
			MyRvLinkDeviceChassisStatus myRvLinkDeviceChassisStatus = MyRvLinkDeviceChassisStatus.None;
			switch (parkingBreakStatus)
			{
			case MyRvLinkParkingBreakStatus.Engaged:
				myRvLinkDeviceChassisStatus |= MyRvLinkDeviceChassisStatus.ParkBreakEngaged;
				break;
			case MyRvLinkParkingBreakStatus.Released:
				myRvLinkDeviceChassisStatus |= MyRvLinkDeviceChassisStatus.ParkBreakReleased;
				break;
			}
			switch (ignitionStatus)
			{
			case MyRvLinkIgnitionStatus.On:
				myRvLinkDeviceChassisStatus |= MyRvLinkDeviceChassisStatus.IgnitionOn;
				break;
			case MyRvLinkIgnitionStatus.Off:
				myRvLinkDeviceChassisStatus |= MyRvLinkDeviceChassisStatus.IgnitionOff;
				break;
			}
			_rawData[2] = (byte)myRvLinkDeviceChassisStatus;
			_rawData[3] = 0;
			_rawData[4] = byte.MaxValue;
			_rawData[5] = byte.MaxValue;
			ChassisInfoStatus = new LogicalDeviceChassisInfoStatus(_rawData[2], _rawData[3], _rawData[4], _rawData[5]);
		}

		protected MyRvLinkDeviceLockStatus(IReadOnlyList<byte> rawData)
			: base((IReadOnlyList<byte>)rawData.ToNewArray(0, rawData.Count))
		{
			ChassisInfoStatus = new LogicalDeviceChassisInfoStatus(rawData[2], rawData[3], rawData[4], rawData[5]);
		}

		public static MyRvLinkDeviceLockStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkDeviceLockStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, bool isLocked)> EnumerateIsDeviceLocked()
		{
			int endDeviceId = StartDeviceId + base.DeviceCount;
			for (byte deviceId = StartDeviceId; deviceId < endDeviceId; deviceId = (byte)(deviceId + 1))
			{
				yield return (deviceId, IsDeviceLocked(deviceId));
			}
		}

		public IEnumerable<(byte DeviceId, bool isLocked)> EnumerateIsDeviceLockedDiff(MyRvLinkDeviceLockStatus? otherDeviceStatus)
		{
			if (otherDeviceStatus != null && (base.DeviceTableId != otherDeviceStatus!.DeviceTableId || base.DeviceCount != otherDeviceStatus!.DeviceCount))
			{
				otherDeviceStatus = null;
			}
			int endDeviceId = StartDeviceId + base.DeviceCount;
			for (byte deviceId = StartDeviceId; deviceId < endDeviceId; deviceId = (byte)(deviceId + 1))
			{
				bool flag = IsDeviceLocked(deviceId);
				if (otherDeviceStatus == null || flag != otherDeviceStatus!.IsDeviceLocked(deviceId))
				{
					yield return (deviceId, IsDeviceLocked(deviceId));
				}
			}
		}

		public bool IsDeviceLocked(byte deviceId)
		{
			return GetDeviceStatus(deviceId, StartDeviceId) != 0;
		}

		public void SetDeviceLocked(byte deviceId, bool isLocked)
		{
			SetDeviceStatus(deviceId, isLocked ? ((byte)1) : ((byte)0), StartDeviceId);
		}

		public void SetAllDevicesLocked(bool isLocked)
		{
			int num = StartDeviceId + base.DeviceCount;
			for (byte b = StartDeviceId; b < num; b = (byte)(b + 1))
			{
				SetDeviceLocked(b, isLocked);
			}
		}

		protected override void DevicesToStringBuilder(StringBuilder stringBuilder)
		{
			foreach (var item in EnumerateIsDeviceLocked())
			{
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 3, stringBuilder);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    0x");
				handler.AppendFormatted(item.DeviceId, "X2");
				handler.AppendLiteral(": ");
				handler.AppendFormatted(item.isLocked ? "Locked" : "Not Locked");
				stringBuilder.Append(ref handler);
			}
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(89, 7);
			defaultInterpolatedStringHandler.AppendFormatted(EventType);
			defaultInterpolatedStringHandler.AppendLiteral(" Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(" SystemLockoutLevel: ");
			defaultInterpolatedStringHandler.AppendFormatted(SystemLockoutLevel);
			defaultInterpolatedStringHandler.AppendLiteral(" IgnitionStatus: ");
			defaultInterpolatedStringHandler.AppendFormatted(IgnitionStatus);
			defaultInterpolatedStringHandler.AppendLiteral(" ParkingBreakStatus: ");
			defaultInterpolatedStringHandler.AppendFormatted(ParkingBreakStatus);
			defaultInterpolatedStringHandler.AppendLiteral(" TotalDevices: ");
			defaultInterpolatedStringHandler.AppendFormatted(base.DeviceCount);
			defaultInterpolatedStringHandler.AppendLiteral(": ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			try
			{
				DevicesToStringBuilder(stringBuilder);
			}
			catch (Exception ex)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(31, 2, stringBuilder2);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    ERROR Trying to Get Device ");
				handler.AppendFormatted(ex.Message);
				stringBuilder2.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
}
