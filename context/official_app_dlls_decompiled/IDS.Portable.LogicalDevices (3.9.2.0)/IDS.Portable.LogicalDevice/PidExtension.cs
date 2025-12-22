using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public static class PidExtension
	{
		private const string LogTag = "PidExtension";

		public const int MaxPidBytes = 6;

		public static readonly DateTime Epoch2000 = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private const int MonitorPanelDeviceIdPidCount = 48;

		private static readonly List<Pid> _monitorPanelDeviceIdPidList = new List<Pid>(48);

		public static IReadOnlyList<Pid> MonitorPanelDeviceIdPidList
		{
			get
			{
				lock (_monitorPanelDeviceIdPidList)
				{
					if (_monitorPanelDeviceIdPidList.Count > 0)
					{
						return _monitorPanelDeviceIdPidList;
					}
					for (short num = 277; num < 324; num = (short)(num + 1))
					{
						_monitorPanelDeviceIdPidList.Add((Pid)num);
					}
					return _monitorPanelDeviceIdPidList;
				}
			}
		}

		public static Pid ConvertToPid(this PID canPid)
		{
			return Enum<Pid>.TryConvert(canPid.Value);
		}

		public static PID ConvertToPid(this Pid pid)
		{
			return (ushort)pid;
		}

		private static ulong PidValueToUlong(ulong rawPidValue)
		{
			return rawPidValue;
		}

		private static byte PidValueToByte(ulong rawPidValue)
		{
			return (byte)rawPidValue;
		}

		private static uint PidValueToUInt32(ulong rawPidValue)
		{
			return (uint)rawPidValue;
		}

		private static ushort PidValueToUInt16(ulong rawPidValue)
		{
			return (ushort)rawPidValue;
		}

		private static MAC PidValueToMac(ulong rawPidValue)
		{
			return new MAC((UInt48)rawPidValue);
		}

		private static float PidValueToFixedPointUnsignedBigEndian16X16(ulong rawPidValue)
		{
			return FixedPointUnsignedBigEndian16X16.ToFloat((uint)rawPidValue);
		}

		private static float PidValueToFixedPointSignedBigEndian16X16(ulong rawPidValue)
		{
			return FixedPointSignedBigEndian16X16.ToFloat((uint)rawPidValue);
		}

		public static DateTime PidEpoch2000ValueToDatetime(ulong rawPidValue)
		{
			return Epoch2000.AddSeconds(rawPidValue);
		}

		public static ulong PidDateTimeToSecondsSinceEpoch2000(DateTime dateTime)
		{
			return (ulong)(dateTime - Epoch2000).TotalSeconds;
		}

		private static DateTime PidBuildDateTimeValueToDatetime(ulong rawPidValue)
		{
			return new DateTime((int)(rawPidValue >> 40) & 0xFF, (int)(rawPidValue >> 32) & 0xFF, (int)(rawPidValue >> 24) & 0xFF, (int)(rawPidValue >> 16) & 0xFF, (int)(rawPidValue >> 8) & 0xFF, (int)(rawPidValue & 0xFF));
		}

		private static string PidValueToString(ulong rawPidValue)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < 6; i++)
			{
				int num = 5 - i % 6;
				ulong num2 = (rawPidValue >> num * 8) & 0xFF;
				if (num2 == 0L)
				{
					break;
				}
				stringBuilder.Append((char)num2);
			}
			return stringBuilder.ToString();
		}

		private static byte[] PidValueToBytes(ulong rawPidValue)
		{
			byte[] array = new byte[6];
			new StringBuilder();
			for (int i = 0; i < 6; i++)
			{
				int num = 5 - i % 6;
				ulong num2 = (rawPidValue >> num * 8) & 0xFF;
				if (num2 == 0L)
				{
					break;
				}
				array[i] = (byte)num2;
			}
			return array;
		}

		private static IPAddress PidValueToIpAddress(ulong rawPidValue)
		{
			return new IPAddress((long)rawPidValue);
		}

		private static TEnumValue PidValueToEnum<TEnumValue>(ulong rawPidValue) where TEnumValue : struct, IConvertible
		{
			return Enum<TEnumValue>.TryConvert(rawPidValue);
		}

		private static bool PidValueToBoolean(ulong rawPidValue)
		{
			return rawPidValue != 0;
		}

		private static PidMonitorPanelDeviceId PidValueToPidDeviceId(ulong rawPidValue)
		{
			return new PidMonitorPanelDeviceId(rawPidValue);
		}

		private static PidValueCheck PidCheckValueUndefined0xFFFFFFFF(ulong value, IDevicePID? devicePid = null)
		{
			PidValueCheck pidValueCheck = PidValueCheckExtension.PidCheckValueDefault(value, devicePid);
			if (pidValueCheck != PidValueCheck.HasValue)
			{
				return pidValueCheck;
			}
			if ((value & 0xFFFFFFFFu) != uint.MaxValue)
			{
				return pidValueCheck;
			}
			return PidValueCheck.Undefined;
		}

		private static PidValueCheck PidCheckValueUndefined0x80000000(ulong value, IDevicePID? devicePid = null)
		{
			PidValueCheck pidValueCheck = PidValueCheckExtension.PidCheckValueDefault(value, devicePid);
			if (pidValueCheck != PidValueCheck.HasValue)
			{
				return pidValueCheck;
			}
			if ((value & 0x80000000u) != 2147483648u)
			{
				return pidValueCheck;
			}
			return PidValueCheck.Undefined;
		}

		private static PidValueCheck PidCheckValueFeatureDisabled0x00000000(ulong value, IDevicePID? devicePid = null)
		{
			PidValueCheck pidValueCheck = PidValueCheckExtension.PidCheckValueDefault(value, devicePid);
			if (pidValueCheck != PidValueCheck.HasValue)
			{
				return pidValueCheck;
			}
			if ((value & 0xFFFFFFFFu) != uint.MaxValue)
			{
				return pidValueCheck;
			}
			return PidValueCheck.FeatureDisabled;
		}

		private static PidValueCheck PidCheckValueFeatureDisabled0x80000000(ulong value, IDevicePID? devicePid = null)
		{
			PidValueCheck pidValueCheck = PidValueCheckExtension.PidCheckValueDefault(value, devicePid);
			if (pidValueCheck != PidValueCheck.HasValue)
			{
				return pidValueCheck;
			}
			if ((value & 0x80000000u) != 2147483648u)
			{
				return pidValueCheck;
			}
			return PidValueCheck.FeatureDisabled;
		}

		public static bool IsAutoCacheingPid(this Pid pid)
		{
			if (((int)pid >= 345 && (int)pid <= 360) || pid == Pid.LevelerSetPointNames)
			{
				return true;
			}
			return false;
		}

		public static IPidDetail GetPidDetailDefault(this Pid pid)
		{
			switch (pid)
			{
			case Pid.Unknown:
				return new PidDetail<ulong>(pid, PidCategory.Unknown, PidUnits.None, PidValueToUlong, null, 0);
			case Pid.ProductionBytes:
				return new PidDetail<ulong>(pid, PidCategory.Manufacturing, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.CanAdapterMac:
				return new PidDetail<MAC>(pid, PidCategory.Device, PidUnits.Mac, PidValueToMac, null, 6, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.IdsCanCircuitId:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.None, PidValueToUlong, null, 4, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.IdsCanFunctionName:
				return new PidDetail<FunctionName>(pid, PidCategory.Device, PidUnits.None, PidValueToEnum<FunctionName>, null, 2, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.IdsCanFunctionInstance:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.IdsCanNumDevicesOnNetwork:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.Quantity, PidValueToUlong);
			case Pid.IdsCanMaxNetworkHeartbeatTime:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.Milliseconds, PidValueToUlong, null, 2);
			case Pid.SerialNumber:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.CanBytesTx:
			case Pid.CanBytesRx:
			case Pid.CanMessagesTx:
			case Pid.CanMessagesRx:
			case Pid.CanTxBufferOverflowCount:
			case Pid.CanRxBufferOverflowCount:
			case Pid.CanTxMaxBytesQueued:
			case Pid.CanRxMaxBytesQueued:
				return new PidDetail<ulong>(pid, PidCategory.StatisticsCan, PidUnits.Quantity, PidValueToUlong);
			case Pid.UartBytesTx:
			case Pid.UartBytesRx:
			case Pid.UartMessagesTx:
			case Pid.UartMessagesRx:
			case Pid.UartTxBufferOverflowCount:
			case Pid.UartRxBufferOverflowCount:
			case Pid.UartTxMaxBytesQueued:
			case Pid.UartRxMaxBytesQueued:
				return new PidDetail<ulong>(pid, PidCategory.StatisticsSerial, PidUnits.Quantity, PidValueToUlong);
			case Pid.WifiBytesTx:
			case Pid.WifiBytesRx:
			case Pid.WifiMessagesTx:
			case Pid.WifiMessagesRx:
			case Pid.WifiTxBufferOverflowCount:
			case Pid.WifiRxBufferOverflowCount:
			case Pid.WifiTxMaxBytesQueued:
			case Pid.WifiRxMaxBytesQueued:
				return new PidDetail<float>(pid, PidCategory.StatisticsWifi, PidUnits.Quantity, PidValueToFixedPointUnsignedBigEndian16X16);
			case Pid.WifiRssi:
				return new PidDetail<float>(pid, PidCategory.StatisticsWifi, PidUnits.Rssi, PidValueToFixedPointUnsignedBigEndian16X16, null, 4);
			case Pid.RfBytesTx:
			case Pid.RfBytesRx:
			case Pid.RfMessagesTx:
			case Pid.RfMessagesRx:
			case Pid.RfTxBufferOverflowCount:
			case Pid.RfRxBufferOverflowCount:
			case Pid.RfTxMaxBytesQueued:
			case Pid.RfRxMaxBytesQueued:
				return new PidDetail<ulong>(pid, PidCategory.StatisticsWifi, PidUnits.Quantity, PidValueToUlong);
			case Pid.RfRssi:
				return new PidDetail<float>(pid, PidCategory.StatisticsWifi, PidUnits.Rssi, PidValueToFixedPointUnsignedBigEndian16X16, null, 4);
			case Pid.BatteryVoltage:
			case Pid.RegulatorVoltage:
				return new PidDetail<float>(pid, PidCategory.Battery, PidUnits.Volts, PidValueToFixedPointUnsignedBigEndian16X16, null, 4);
			case Pid.NumTiltSensorAxes:
				return new PidDetail<ulong>(pid, PidCategory.Leveler, PidUnits.Quantity, PidValueToUlong);
			case Pid.TiltAxis1Angle:
			case Pid.TiltAxis2Angle:
			case Pid.TiltAxis3Angle:
			case Pid.TiltAxis4Angle:
			case Pid.TiltAxis5Angle:
			case Pid.TiltAxis6Angle:
			case Pid.TiltAxis7Angle:
			case Pid.TiltAxis8Angle:
				return new PidDetail<float>(pid, PidCategory.Leveler, PidUnits.Degrees, PidValueToFixedPointSignedBigEndian16X16, null, 4);
			case Pid.IdsCanFixedAddress:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.FuseSetting1:
			case Pid.FuseSetting2:
			case Pid.FuseSetting3:
			case Pid.FuseSetting4:
			case Pid.FuseSetting5:
			case Pid.FuseSetting6:
			case Pid.FuseSetting7:
			case Pid.FuseSetting8:
			case Pid.FuseSetting9:
			case Pid.FuseSetting10:
			case Pid.FuseSetting11:
			case Pid.FuseSetting12:
			case Pid.FuseSetting13:
			case Pid.FuseSetting14:
			case Pid.FuseSetting15:
			case Pid.FuseSetting16:
				return new PidDetail<float>(pid, PidCategory.Fuse, PidUnits.Amps, PidValueToFixedPointSignedBigEndian16X16, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.ManufacturingPid1:
			case Pid.ManufacturingPid2:
			case Pid.ManufacturingPid3:
			case Pid.ManufacturingPid4:
			case Pid.ManufacturingPid5:
			case Pid.ManufacturingPid6:
			case Pid.ManufacturingPid7:
			case Pid.ManufacturingPid8:
			case Pid.ManufacturingPid9:
			case Pid.ManufacturingPid10:
			case Pid.ManufacturingPid11:
			case Pid.ManufacturingPid12:
			case Pid.ManufacturingPid13:
			case Pid.ManufacturingPid14:
			case Pid.ManufacturingPid15:
			case Pid.ManufacturingPid16:
			case Pid.ManufacturingPid17:
			case Pid.ManufacturingPid18:
			case Pid.ManufacturingPid19:
			case Pid.ManufacturingPid20:
			case Pid.ManufacturingPid21:
			case Pid.ManufacturingPid22:
			case Pid.ManufacturingPid23:
			case Pid.ManufacturingPid24:
			case Pid.ManufacturingPid25:
			case Pid.ManufacturingPid26:
			case Pid.ManufacturingPid27:
			case Pid.ManufacturingPid28:
			case Pid.ManufacturingPid29:
			case Pid.ManufacturingPid30:
			case Pid.ManufacturingPid31:
			case Pid.ManufacturingPid32:
				return new PidDetail<ulong>(pid, PidCategory.Manufacturing, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.MeteredTimeSec:
			case Pid.MaintenancePeriodSec:
			case Pid.LastMaintenanceTimeSec:
				return new PidDetail<ulong>(pid, PidCategory.Maintenance, PidUnits.Seconds, PidValueToUlong, null, 4, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.TimeZone:
				return new PidDetail<ulong>(pid, PidCategory.Rtc, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.RtcTimeSec:
				return new PidDetail<ulong>(pid, PidCategory.Rtc, PidUnits.Seconds, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.RtcTimeMin:
				return new PidDetail<ulong>(pid, PidCategory.Rtc, PidUnits.Minutes, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.RtcTimeHour:
				return new PidDetail<ulong>(pid, PidCategory.Rtc, PidUnits.Hours, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.RtcTimeDay:
				return new PidDetail<ulong>(pid, PidCategory.Rtc, PidUnits.Days, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.RtcTimeMonth:
				return new PidDetail<ulong>(pid, PidCategory.Rtc, PidUnits.Months, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.RtcTimeYear:
				return new PidDetail<ulong>(pid, PidCategory.Rtc, PidUnits.Years, PidValueToUlong, null, 2, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.RtcEpochSec:
			case Pid.RtcSetTimeSec:
				return new PidDetail<DateTime>(pid, PidCategory.Rtc, PidUnits.DateTime, PidEpoch2000ValueToDatetime, null, 4, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.BleDeviceName1:
			case Pid.BleDeviceName2:
			case Pid.BleDeviceName3:
				return new PidDetail<string>(pid, PidCategory.Ble, PidUnits.None, PidValueToString, null, 6, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.BlePin:
				return new PidDetail<string>(pid, PidCategory.Ble, PidUnits.None, PidValueToString, null, 6, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.SystemUptimeMs:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.Milliseconds, PidValueToUlong);
			case Pid.EthAdapterMac:
				return new PidDetail<MAC>(pid, PidCategory.StatisticsEthernet, PidUnits.Mac, PidValueToMac, null, 6, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.EthBytesTx:
			case Pid.EthBytesRx:
			case Pid.EthMessagesTx:
			case Pid.EthMessagesRx:
			case Pid.EthTxBufferOverflowCount:
			case Pid.EthRxBufferOverflowCount:
			case Pid.EthPacketsTxDiscarded:
			case Pid.EthPacketsRxDiscarded:
			case Pid.EthPacketsTxError:
			case Pid.EthPacketsRxError:
			case Pid.EthPacketsTxOverflow:
			case Pid.EthPacketsTxLateCollision:
			case Pid.EthPacketsTxExcessCollision:
			case Pid.EthPacketsTxUnderflow:
			case Pid.EthPacketsRxAlignErr:
			case Pid.EthPacketsRxCrcErr:
			case Pid.EthPacketsRxTruncErr:
			case Pid.EthPacketsRxLenErr:
			case Pid.EthPacketsRxCollision:
				return new PidDetail<ulong>(pid, PidCategory.StatisticsEthernet, PidUnits.Quantity, PidValueToUlong);
			case Pid.IpAddress:
			case Pid.IpSubnetmask:
			case Pid.IpGateway:
				return new PidDetail<IPAddress>(pid, PidCategory.TcpIp, PidUnits.IpAddress, PidValueToIpAddress, null, 4, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.TcpNumConnections:
				return new PidDetail<ulong>(pid, PidCategory.TcpIp, PidUnits.Quantity, PidValueToUlong);
			case Pid.AuxBatteryVoltage:
				return new PidDetail<float>(pid, PidCategory.Battery, PidUnits.Volts, PidValueToFixedPointUnsignedBigEndian16X16, null, 4);
			case Pid.RgbLightingGangEnable:
				return new PidDetail<ulong>(pid, PidCategory.Light, PidUnits.Unknown, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.InputSwitchType:
				return new PidDetail<PidInputSwitchType>(pid, PidCategory.Misc, PidUnits.None, PidValueToEnum<PidInputSwitchType>, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.DoorLockState:
				return new PidDetail<ulong>(pid, PidCategory.Misc, PidUnits.Unknown, PidValueToUlong);
			case Pid.GeneratorQuietHoursStartTime:
			case Pid.GeneratorQuietHoursEndTime:
				return new PidDetail<ulong>(pid, PidCategory.Generator, PidUnits.MinutesAfterMidnight, PidValueToUlong, null, 2, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.GeneratorAutoStartLowVoltage:
				return new PidDetail<float>(pid, PidCategory.Generator, PidUnits.Volts, PidValueToFixedPointUnsignedBigEndian16X16, PidCheckValueFeatureDisabled0x00000000, 4, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.GeneratorAutoStartHiTempC:
				return new PidDetail<ulong>(pid, PidCategory.Generator, PidUnits.Celsius, PidValueToUlong, PidCheckValueFeatureDisabled0x80000000, 4, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.GeneratorAutoRunDurationMinutes:
			case Pid.GeneratorAutoRunMinOffTimeMinutes:
				return new PidDetail<ulong>(pid, PidCategory.Generator, PidUnits.Minutes, PidValueToUlong, null, 2, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.SoftwareBuildDateTime:
				return new PidDetail<DateTime>(pid, PidCategory.Device, PidUnits.DateTime, PidBuildDateTimeValueToDatetime, null, 6, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.GeneratorQuietHoursEnabled:
				return new PidDetail<bool>(pid, PidCategory.Generator, PidUnits.None, PidValueToBoolean, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.ShorePowerAmpRating:
				return new PidDetail<float>(pid, PidCategory.Battery, PidUnits.Amps, PidValueToFixedPointUnsignedBigEndian16X16, PidCheckValueUndefined0xFFFFFFFF, 4, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.BatteryCapacityAmpHours:
				return new PidDetail<float>(pid, PidCategory.Battery, PidUnits.AmpHours, PidValueToFixedPointUnsignedBigEndian16X16, PidCheckValueUndefined0xFFFFFFFF, 4, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.PcbAssemblyPartNumber:
				return new PidDetail<byte[]>(pid, PidCategory.Device, PidUnits.None, PidValueToBytes, null, 6, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.UnlockPin:
				return new PidDetail<ulong>(pid, PidCategory.Misc, PidUnits.None, PidValueToUlong, null, 2, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.UnlockPinMode:
				return new PidDetail<PidUnlockPinModeType>(pid, PidCategory.Misc, PidUnits.None, PidValueToEnum<PidUnlockPinModeType>, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.SimulateOnOffStyleLight:
				return new PidDetail<bool>(pid, PidCategory.Light, PidUnits.None, PidValueToBoolean, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.FanSpeedControlType:
				return new PidDetail<PidFanSpeedControlType>(pid, PidCategory.Misc, PidUnits.None, PidValueToEnum<PidFanSpeedControlType>, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.HvacControlType:
				return new PidDetail<PidHvacControlType>(pid, PidCategory.Misc, PidUnits.None, PidValueToEnum<PidHvacControlType>, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.SoftwareFuseRatingAmps:
			case Pid.SoftwareFuseMaxRatingAmps:
				return new PidDetail<float>(pid, PidCategory.Fuse, PidUnits.Amps, PidValueToFixedPointUnsignedBigEndian16X16, null, 4, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.CumminsOnanGeneratorFaultCode:
				return new PidDetail<ulong>(pid, PidCategory.Generator, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.Motor1CurrentAmps:
			case Pid.Motor2CurrentAmps:
			case Pid.Motor3CurrentAmps:
			case Pid.Motor4CurrentAmps:
			case Pid.Motor5CurrentAmps:
			case Pid.Motor6CurrentAmps:
			case Pid.Motor7CurrentAmps:
			case Pid.Motor8CurrentAmps:
			case Pid.Motor9CurrentAmps:
			case Pid.Motor10CurrentAmps:
			case Pid.Motor11CurrentAmps:
			case Pid.Motor12CurrentAmps:
			case Pid.Motor13CurrentAmps:
			case Pid.Motor14CurrentAmps:
			case Pid.Motor15CurrentAmps:
			case Pid.Motor16CurrentAmps:
				return new PidDetail<float>(pid, PidCategory.Motor, PidUnits.Amps, PidValueToFixedPointSignedBigEndian16X16, null, 4);
			case Pid.DeviceType:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.InMotionLockoutBehavior:
				return new PidDetail<PidLockoutType>(pid, PidCategory.Device, PidUnits.None, PidValueToEnum<PidLockoutType>, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.RvcDetectedNodes:
			case Pid.RvcLostNodes:
			case Pid.RvcBytesTx:
			case Pid.RvcBytesRx:
			case Pid.RvcMessagesTx:
			case Pid.RvcMessagesRx:
			case Pid.RvcTxBuffersFree:
			case Pid.RvcTxBuffersUsed:
			case Pid.RvcRxBuffersFree:
			case Pid.RvcRxBuffersUsed:
			case Pid.RvcTxOutOfBuffersCount:
			case Pid.RvcRxOutOfBuffersCount:
			case Pid.RvcTxFailureCount:
				return new PidDetail<ulong>(pid, PidCategory.StatisticsRvc, PidUnits.Quantity, PidValueToUlong);
			case Pid.RvcDefaultSrcAddr:
			case Pid.RvcDynamicAddr:
				return new PidDetail<ulong>(pid, PidCategory.Rvc, PidUnits.None, PidValueToUlong);
			case Pid.RvcMake:
			case Pid.RvcModel1:
			case Pid.RvcModel2:
			case Pid.RvcModel3:
			case Pid.RvcSerial:
			case Pid.RvcIdNumber:
				return new PidDetail<string>(pid, PidCategory.Rvc, PidUnits.None, PidValueToString, null, 6, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.CloudGatewayAssetIdPart1:
			case Pid.CloudGatewayAssetIdPart2:
			case Pid.CloudGatewayAssetIdPart3:
				return new PidDetail<string>(pid, PidCategory.Cloud, PidUnits.None, PidValueToString, null, 6, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.HvacZoneCapabilities:
				return new PidDetail<ulong>(pid, PidCategory.Misc, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.IgnitionBehavior:
				return new PidDetail<PidIgnitionBehaviorType>(pid, PidCategory.Device, PidUnits.None, PidValueToEnum<PidIgnitionBehaviorType>, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.BleNumberOfForwardedCanDevices:
			case Pid.BleNumberOfConnects:
			case Pid.BleNumberOfDisconnects:
			case Pid.BleTotalTraffic:
			case Pid.BleWritesFromPhone:
			case Pid.BleNotificationsToPhoneSuccessful:
			case Pid.BleNotificationsToPhoneFailure:
				return new PidDetail<ulong>(pid, PidCategory.StatisticsBle, PidUnits.Quantity, PidValueToUlong);
			case Pid.BleMtuSizeCentral:
			case Pid.BleMtuSizePeripheral:
			case Pid.BleDataLengthTime:
				return new PidDetail<ulong>(pid, PidCategory.StatisticsBle, PidUnits.None, PidValueToUlong);
			case Pid.BleSecurityUnlocked:
			case Pid.BleClientConnected:
				return new PidDetail<bool>(pid, PidCategory.StatisticsBle, PidUnits.None, PidValueToBoolean);
			case Pid.BleCccdWritten:
			case Pid.BleNumBuffersFree:
			case Pid.BleLastTxError:
			case Pid.BleConnectedDeviceRssi:
			case Pid.BleDeadClientCounter:
			case Pid.BleLastDisconnectReason:
				return new PidDetail<ulong>(pid, PidCategory.StatisticsBle, PidUnits.None, PidValueToUlong);
			case Pid.BleSpiRxMsgsDropped:
			case Pid.BleSpiTxMsgsDropped:
				return new PidDetail<ulong>(pid, PidCategory.StatisticsBle, PidUnits.Quantity, PidValueToUlong);
			case Pid.LowVoltageBehavior:
				return new PidDetail<PidLowVoltageBehaviorType>(pid, PidCategory.Battery, PidUnits.None, PidValueToEnum<PidLowVoltageBehaviorType>, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.DhcpEnabled:
				return new PidDetail<bool>(pid, PidCategory.TcpIp, PidUnits.None, PidValueToBoolean, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.UdpDeviceName1:
			case Pid.UdpDeviceName2:
			case Pid.UdpDeviceName3:
				return new PidDetail<string>(pid, PidCategory.TcpIp, PidUnits.None, PidValueToString, null, 6, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.TcpBatchSize:
				return new PidDetail<ulong>(pid, PidCategory.TcpIp, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.TcpBatchTime:
				return new PidDetail<ulong>(pid, PidCategory.TcpIp, PidUnits.Milliseconds, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.OnOffInputPin:
			case Pid.ExtendInputPin:
			case Pid.RetractInputPin:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.InputPinCount:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.Quantity, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.DsiFaultInputPin:
			case Pid.DeviceActivationTimeout:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.LevelerUiSupportedFeatures:
				return new PidDetail<PidLevelUiSupportedFeaturesType>(pid, PidCategory.Leveler, PidUnits.None, PidValueToEnum<PidLevelUiSupportedFeaturesType>);
			case Pid.LevelerSensorTopology:
				return new PidDetail<PidLevelSensorTopologyType>(pid, PidCategory.Leveler, PidUnits.None, PidValueToEnum<PidLevelSensorTopologyType>);
			case Pid.LevelerDriveType:
				return new PidDetail<PidLevelDriveType>(pid, PidCategory.Leveler, PidUnits.None, PidValueToEnum<PidLevelDriveType>);
			case Pid.LevelerAutoModeProgress:
				return new PidDetail<ulong>(pid, PidCategory.Leveler, PidUnits.None, PidValueToUlong, null, 3);
			case Pid.LeftFrontJackStrokeInches:
			case Pid.RightFrontJackStrokeInches:
			case Pid.LeftMiddleJackStrokeInches:
			case Pid.RightMiddleJackStrokeInches:
			case Pid.LeftRearJackStrokeInches:
			case Pid.RightRearJackStrokeInches:
				return new PidDetail<float>(pid, PidCategory.Leveler, PidUnits.Inches, PidValueToFixedPointSignedBigEndian16X16, PidCheckValueUndefined0x80000000, 4);
			case Pid.LeftFrontJackMaxStrokeInches:
			case Pid.RightFrontJackMaxStrokeInches:
			case Pid.LeftMiddleJackMaxStrokeInches:
			case Pid.RightMiddleJackMaxStrokeInches:
			case Pid.LeftRearJackMaxStrokeInches:
			case Pid.RightRearJackMaxStrokeInches:
				return new PidDetail<float>(pid, PidCategory.Leveler, PidUnits.Inches, PidValueToFixedPointUnsignedBigEndian16X16, PidCheckValueUndefined0xFFFFFFFF, 4);
			case Pid.ParkbrakeBehavior:
				return new PidDetail<PidParkBrakeBehaviorType>(pid, PidCategory.Device, PidUnits.None, PidValueToEnum<PidParkBrakeBehaviorType>, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.ExtendedDeviceCapabilities:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.Dynamic, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.CloudCapabilities:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.Dynamic, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.RvMakeId:
			case Pid.RvModelId:
				return new PidDetail<ulong>(pid, PidCategory.MakeModelYearFloorplan, PidUnits.None, PidValueToUlong, null, 4, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.RvYear:
				return new PidDetail<ulong>(pid, PidCategory.MakeModelYearFloorplan, PidUnits.None, PidValueToUlong, null, 2, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.RvFloorplanId:
				return new PidDetail<ulong>(pid, PidCategory.MakeModelYearFloorplan, PidUnits.None, PidValueToUlong, null, 4, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.FloorplanPartNum:
				return new PidDetail<string>(pid, PidCategory.MakeModelYearFloorplan, PidUnits.None, PidValueToString, null, 6, 2, deprecated: true, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.FloorplanWrittenBy:
				return new PidDetail<ulong>(pid, PidCategory.MakeModelYearFloorplan, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: true, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.AssemblyPartNum:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.None, PidValueToUlong, null, 6, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.AssemblyDateCode:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.None, PidValueToUlong, null, 5, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.AssemblySerialNum:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.None, PidValueToUlong, null, 5, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.LevelerAutoProcessSteps1:
			case Pid.LevelerAutoProcessSteps2:
			case Pid.LevelerAutoProcessSteps3:
			case Pid.LevelerAutoProcessSteps4:
			case Pid.LevelerAutoProcessSteps5:
				return new PidDetail<byte[]>(pid, PidCategory.Leveler, PidUnits.None, PidValueToBytes, null, 6);
			case Pid.MonitorPanelDeviceId1:
			case Pid.MonitorPanelDeviceId2:
			case Pid.MonitorPanelDeviceId3:
			case Pid.MonitorPanelDeviceId4:
			case Pid.MonitorPanelDeviceId5:
			case Pid.MonitorPanelDeviceId6:
			case Pid.MonitorPanelDeviceId7:
			case Pid.MonitorPanelDeviceId8:
			case Pid.MonitorPanelDeviceId9:
			case Pid.MonitorPanelDeviceId10:
			case Pid.MonitorPanelDeviceId11:
			case Pid.MonitorPanelDeviceId12:
			case Pid.MonitorPanelDeviceId13:
			case Pid.MonitorPanelDeviceId14:
			case Pid.MonitorPanelDeviceId15:
			case Pid.MonitorPanelDeviceId16:
			case Pid.MonitorPanelDeviceId17:
			case Pid.MonitorPanelDeviceId18:
			case Pid.MonitorPanelDeviceId19:
			case Pid.MonitorPanelDeviceId20:
			case Pid.MonitorPanelDeviceId21:
			case Pid.MonitorPanelDeviceId22:
			case Pid.MonitorPanelDeviceId23:
			case Pid.MonitorPanelDeviceId24:
			case Pid.MonitorPanelDeviceId25:
			case Pid.MonitorPanelDeviceId26:
			case Pid.MonitorPanelDeviceId27:
			case Pid.MonitorPanelDeviceId28:
			case Pid.MonitorPanelDeviceId29:
			case Pid.MonitorPanelDeviceId30:
			case Pid.MonitorPanelDeviceId31:
			case Pid.MonitorPanelDeviceId32:
			case Pid.MonitorPanelDeviceId33:
			case Pid.MonitorPanelDeviceId34:
			case Pid.MonitorPanelDeviceId35:
			case Pid.MonitorPanelDeviceId36:
			case Pid.MonitorPanelDeviceId37:
			case Pid.MonitorPanelDeviceId38:
			case Pid.MonitorPanelDeviceId39:
			case Pid.MonitorPanelDeviceId40:
			case Pid.MonitorPanelDeviceId41:
			case Pid.MonitorPanelDeviceId42:
			case Pid.MonitorPanelDeviceId43:
			case Pid.MonitorPanelDeviceId44:
			case Pid.MonitorPanelDeviceId45:
			case Pid.MonitorPanelDeviceId46:
			case Pid.MonitorPanelDeviceId47:
			case Pid.MonitorPanelDeviceId48:
				return new PidDetail<PidMonitorPanelDeviceId>(pid, PidCategory.MonitorPanel, PidUnits.MonitorPanelDeviceId, PidValueToPidDeviceId, null, 6, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.MonitorPanelControlTypeMomentarySwitch:
			case Pid.MonitorPanelControlTypeLatchingSwitch:
			case Pid.MonitorPanelControlTypeSupplyTank:
			case Pid.MonitorPanelControlTypeWasteTank:
				return new PidDetail<ulong>(pid, PidCategory.MonitorPanel, PidUnits.Quantity, PidValueToUlong);
			case Pid.MonitorPanelConfiguration:
			case Pid.BlePairingMode:
				return new PidDetail<ulong>(pid, PidCategory.MonitorPanel, PidUnits.Quantity, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.MonitorPanelCalibrationPartNumber:
				return new PidDetail<ulong>(pid, PidCategory.MonitorPanel, PidUnits.Quantity, PidValueToUlong);
			case Pid.ReadAddress16BitsData32Bits:
				return new PidDetail<uint>(pid, PidCategory.WithAddress, PidUnits.Dynamic, PidValueToUInt32);
			case Pid.WriteAddress16BitsData32Bits:
				return new PidDetail<uint>(pid, PidCategory.WithAddress, PidUnits.Dynamic, PidValueToUInt32, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Reprogramming);
			case Pid.TempSensorHighAlert:
			case Pid.TempSensorLowAlert:
				return new PidDetail<ulong>(pid, PidCategory.Accessory, PidUnits.Mixed, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.AccGatewayAddDeviceMac:
				return new PidDetail<MAC>(pid, PidCategory.Accessory, PidUnits.Mac, PidValueToMac, null, 6, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.AccGatewayWriteDeviceSoftwarePartNum:
				return new PidDetail<string>(pid, PidCategory.Accessory, PidUnits.None, PidValueToString, null, 6, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.VehicleConfiguration:
				return new PidDetail<ulong>(pid, PidCategory.Accessory, PidUnits.Mixed, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.TpmsSensorPosition:
				return new PidDetail<ulong>(pid, PidCategory.WithAddress, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Reprogramming);
			case Pid.TpmsSensorPressureFaultLimits:
				return new PidDetail<ulong>(pid, PidCategory.WithAddress, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Reprogramming);
			case Pid.TpmsSensorTemperatureFaultLimits:
				return new PidDetail<ulong>(pid, PidCategory.WithAddress, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Reprogramming);
			case Pid.TpmsSensorId:
				return new PidDetail<ulong>(pid, PidCategory.WithAddress, PidUnits.None, PidValueToUlong);
			case Pid.SmartArmWindEventSetting:
				return new PidDetail<byte>(pid, PidCategory.AwningSensor, PidUnits.None, PidValueToByte);
			case Pid.AccRequestMode:
				return new PidDetail<byte>(pid, PidCategory.Accessory, PidUnits.None, PidValueToByte);
			case Pid.AccessorySetting1:
			case Pid.AccessorySetting2:
			case Pid.AccessorySetting3:
			case Pid.AccessorySetting4:
			case Pid.AccessorySetting5:
			case Pid.AccessorySetting6:
			case Pid.AccessorySetting7:
			case Pid.AccessorySetting8:
			case Pid.AccessorySetting9:
			case Pid.AccessorySetting10:
			case Pid.AccessorySetting11:
			case Pid.AccessorySetting12:
			case Pid.AccessorySetting13:
			case Pid.AccessorySetting14:
			case Pid.AccessorySetting15:
			case Pid.AccessorySetting16:
				return new PidDetail<ulong>(pid, PidCategory.Accessory, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.TireTrackWidth:
			case Pid.TireDiameter:
			case Pid.AbsRimTeethCount:
				return new PidDetail<byte>(pid, PidCategory.ElectronicBrakeControl, PidUnits.None, PidValueToByte, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.AbsMaintenancePeriod:
				return new PidDetail<ulong>(pid, PidCategory.ElectronicBrakeControl, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.IlluminationSync:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.RvCInstance:
				return new PidDetail<ulong>(pid, PidCategory.Device, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.HvacControlTypeSetting:
				return new PidDetail<ulong>(pid, PidCategory.Misc, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.ActiveHvacControlType:
				return new PidDetail<ulong>(pid, PidCategory.Misc, PidUnits.None, PidValueToUlong);
			case Pid.MonitorPanelControlTypeConfigTank:
				return new PidDetail<ulong>(pid, PidCategory.MonitorPanel, PidUnits.Quantity, PidValueToUlong);
			case Pid.NumberOfAxles:
				return new PidDetail<byte>(pid, PidCategory.ElectronicBrakeControl, PidUnits.Quantity, PidValueToByte, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Manufacturing);
			case Pid.LastMaintenanceOdometer:
				return new PidDetail<ulong>(pid, PidCategory.ElectronicBrakeControl, PidUnits.Miles, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: false, LogicalDeviceSessionType.Diagnostic);
			case Pid.AccGwNumDevice:
				return new PidDetail<byte>(pid, PidCategory.AccessoryGateway, PidUnits.Quantity, PidValueToByte);
			case Pid.AccGwMacHigh:
				return new PidDetail<uint>(pid, PidCategory.WithAddress, PidUnits.MacHigh, PidValueToUInt32);
			case Pid.AccGwMacLow:
				return new PidDetail<uint>(pid, PidCategory.WithAddress, PidUnits.MacLow, PidValueToUInt32);
			case Pid.DeviceTypeAtIndex:
				return new PidDetail<uint>(pid, PidCategory.WithAddress, PidUnits.None, PidValueToUInt32);
			case Pid.BrakeModuleOrientation:
				return new PidDetail<byte>(pid, PidCategory.ElectronicBrakeControl, PidUnits.None, PidValueToByte);
			case Pid.CoreMicrocontrollerReset:
				return new PidDetail<byte[]>(pid, PidCategory.Misc, PidUnits.None, PidValueToBytes, null, 6);
			case Pid.ProductFwPartNumber:
				return new PidDetail<byte[]>(pid, PidCategory.Device, PidUnits.None, PidValueToBytes, null, 6);
			case Pid.CoreVersionInfo:
				return new PidDetail<byte[]>(pid, PidCategory.Misc, PidUnits.None, PidValueToBytes);
			case Pid.ProductIdNum:
				return new PidDetail<uint>(pid, PidCategory.Device, PidUnits.None, PidValueToUInt32, null, 2);
			case Pid.ProductIdInConfigBlock:
				return new PidDetail<uint>(pid, PidCategory.Misc, PidUnits.None, PidValueToUInt32, null, 2);
			case Pid.LocapVersionInfo:
				return new PidDetail<byte[]>(pid, PidCategory.Misc, PidUnits.None, PidValueToBytes, null, 2);
			case Pid.ProductFwPartNum1:
				return new PidDetail<string>(pid, PidCategory.Misc, PidUnits.None, PidValueToString, null, 6);
			case Pid.ProductFwPartNum2:
				return new PidDetail<string>(pid, PidCategory.Misc, PidUnits.None, PidValueToString, null, 2);
			case Pid.HBridgeSafetyAlertConfig:
				return new PidDetail<byte>(pid, PidCategory.Misc, PidUnits.None, PidValueToByte);
			case Pid.AwningAutoProtectionCount:
				return new PidDetail<ushort>(pid, PidCategory.Misc, PidUnits.Quantity, PidValueToUInt16, null, 2);
			case Pid.MomentaryHBridgeCircuitRole:
				return new PidDetail<byte>(pid, PidCategory.Device, PidUnits.None, PidValueToByte);
			case Pid.SoundsHighestCapable:
				return new PidDetail<byte>(pid, PidCategory.Device, PidUnits.None, PidValueToByte);
			case Pid.SmartArmValanceCorrection:
				return new PidDetail<byte>(pid, PidCategory.Device, PidUnits.None, PidValueToByte);
			case Pid.JumpToBoot:
				return new PidDetail<byte[]>(pid, PidCategory.Misc, PidUnits.None, PidValueToBytes, null, 6);
			case Pid.OptionalCapabilitiesSupported:
				return new PidDetail<byte>(pid, PidCategory.Device, PidUnits.None, PidValueToByte);
			case Pid.OptionalCapabilitiesEnabled:
				return new PidDetail<byte>(pid, PidCategory.Device, PidUnits.None, PidValueToByte);
			case Pid.OptionalCapabilitiesMandatory:
				return new PidDetail<byte>(pid, PidCategory.Device, PidUnits.None, PidValueToByte);
			case Pid.AbsModelVersion:
				return new PidDetail<ushort>(pid, PidCategory.Device, PidUnits.None, PidValueToUInt16, null, 2);
			case Pid.LockoutDisablesSwitchInput:
				return new PidDetail<byte>(pid, PidCategory.Device, PidUnits.None, PidValueToByte);
			case Pid.TankSensorType:
				return new PidDetail<byte>(pid, PidCategory.Device, PidUnits.None, PidValueToByte);
			case Pid.TankSensorCalibrationMultiplier:
				return new PidDetail<byte>(pid, PidCategory.Device, PidUnits.None, PidValueToByte);
			case Pid.TankSensorCalibration1:
			case Pid.TankSensorCalibration2:
			case Pid.TankSensorCalibration3:
			case Pid.TankSensorCalibration4:
				return new PidDetail<byte[]>(pid, PidCategory.Device, PidUnits.None, PidValueToBytes, null, 5);
			case Pid.AbsAutoConfigStatus:
				return new PidDetail<byte>(pid, PidCategory.Device, PidUnits.None, PidValueToByte);
			case Pid.OptionalCapabilitiesUserDisabled:
				return new PidDetail<byte>(pid, PidCategory.Device, PidUnits.None, PidValueToByte);
			case Pid.GeneratorType:
				return new PidDetail<byte>(pid, PidCategory.Device, PidUnits.None, PidValueToByte);
			case Pid.ConfigBuildDateTime:
				return new PidDetail<DateTime>(pid, PidCategory.Device, PidUnits.DateTime, PidBuildDateTimeValueToDatetime, null, 6);
			default:
				TaggedLog.Warning("PidExtension", $"Creating PidDetail for unknown pid {pid}");
				return new PidDetail<ulong>(pid, PidCategory.Unknown, PidUnits.None, PidValueToUlong, null, 1, 2, deprecated: false, undefinedPid: true);
			}
		}

		public static bool ValidatePidsHavePidDetails(bool verbose)
		{
			List<string> list = new List<string>();
			foreach (PID item in PID.GetEnumerator())
			{
				if (item == null)
				{
					break;
				}
				if (((Pid)item.Value).GetPidDetailDefault().IsUndefinedPid)
				{
					list.Add(string.Format("    {0}/0x{1:X4} doesn't have a known {2} or missing {3}", item.Value, item.Value, "Pid", "IPidDetail"));
				}
			}
			if (verbose && list.Count > 0)
			{
				TaggedLog.Error("FunctionNameExtension", "\n**** MISSING Pid/PidDetail DEFINITION START ****");
				foreach (string item2 in list)
				{
					TaggedLog.Error("FunctionNameExtension", item2);
				}
				TaggedLog.Error("FunctionNameExtension", "\n**** MISSING Pid/PidDetail DEFINITION END ****\n");
			}
			return list.Count == 0;
		}
	}
}
