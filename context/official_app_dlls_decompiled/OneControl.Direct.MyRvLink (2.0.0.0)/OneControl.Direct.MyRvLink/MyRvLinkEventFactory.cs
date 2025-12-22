using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using OneControl.Direct.MyRvLink.Events;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkEventFactory : Singleton<MyRvLinkEventFactory>
	{
		public const string LogTag = "MyRvLinkEventFactory";

		public const int EventTypeIndex = 0;

		private MyRvLinkEventFactory()
		{
		}

		public IMyRvLinkEvent? TryDecodeEvent(IReadOnlyList<byte> eventBytes, bool showErrors, Func<int, IMyRvLinkCommand?> getPendingCommand)
		{
			if (eventBytes == null || eventBytes.Count <= 0)
			{
				return null;
			}
			try
			{
				MyRvLinkEventType myRvLinkEventType = (MyRvLinkEventType)eventBytes[0];
				switch (myRvLinkEventType)
				{
				case MyRvLinkEventType.GatewayInformation:
					return MyRvLinkGatewayInformation.Decode(eventBytes);
				case MyRvLinkEventType.DeviceCommand:
					return MyRvLinkCommandEvent.DecodeCommandEvent(eventBytes, getPendingCommand);
				case MyRvLinkEventType.DeviceOnlineStatus:
					return MyRvLinkDeviceOnlineStatus.Decode(eventBytes);
				case MyRvLinkEventType.DeviceLockStatus:
					return MyRvLinkDeviceLockStatus.Decode(eventBytes);
				case MyRvLinkEventType.RelayBasicLatchingStatusType1:
					return MyRvLinkRelayBasicLatchingStatusType1.Decode(eventBytes);
				case MyRvLinkEventType.RelayBasicLatchingStatusType2:
					return MyRvLinkRelayBasicLatchingStatusType2.Decode(eventBytes);
				case MyRvLinkEventType.RvStatus:
					return MyRvLinkRvStatus.Decode(eventBytes);
				case MyRvLinkEventType.DimmableLightStatus:
					return MyRvLinkDimmableLightStatus.Decode(eventBytes);
				case MyRvLinkEventType.RgbLightStatus:
					return MyRvLinkRgbLightStatus.Decode(eventBytes);
				case MyRvLinkEventType.GeneratorGenieStatus:
					return MyRvLinkGeneratorGenieStatus.Decode(eventBytes);
				case MyRvLinkEventType.HvacStatus:
					return MyRvLinkHvacStatus.Decode(eventBytes);
				case MyRvLinkEventType.TankSensorStatus:
					return MyRvLinkTankSensorStatus.Decode(eventBytes);
				case MyRvLinkEventType.TankSensorStatusV2:
					return MyRvLinkTankSensorStatusV2.Decode(eventBytes);
				case MyRvLinkEventType.RelayHBridgeMomentaryStatusType1:
					return MyRvLinkRelayHBridgeMomentaryStatusType1.Decode(eventBytes);
				case MyRvLinkEventType.RelayHBridgeMomentaryStatusType2:
					return MyRvLinkRelayHBridgeMomentaryStatusType2.Decode(eventBytes);
				case MyRvLinkEventType.HourMeterStatus:
					return MyRvLinkHourMeterStatus.Decode(eventBytes);
				case MyRvLinkEventType.Leveler4DeviceStatus:
					return MyRvLinkLeveler4Status.Decode(eventBytes);
				case MyRvLinkEventType.Leveler5DeviceStatus:
					return MyRvLinkLeveler5Status.Decode(eventBytes);
				case MyRvLinkEventType.AutoOperationProgressStatus:
					return MyRvLinkLevelerType5ExtendedStatus.Decode(eventBytes);
				case MyRvLinkEventType.LevelerType5ExtendedStatus:
					return MyRvLinkLevelerType5ExtendedStatus.Decode(eventBytes);
				case MyRvLinkEventType.LevelerConsoleText:
					return MyRvLinkLevelerConsoleText.Decode(eventBytes);
				case MyRvLinkEventType.Leveler1DeviceStatus:
					return MyRvLinkLeveler1Status.Decode(eventBytes);
				case MyRvLinkEventType.Leveler3DeviceStatus:
					return MyRvLinkLeveler3Status.Decode(eventBytes);
				case MyRvLinkEventType.RealTimeClock:
					return MyRvLinkRealTimeClock.Decode(eventBytes);
				case MyRvLinkEventType.MonitorPanelStatus:
					return MyRvLinkMonitorPanelStatus.Decode(eventBytes);
				case MyRvLinkEventType.AccessoryGatewayStatus:
					return MyRvLinkAccessoryGatewayStatus.Decode(eventBytes);
				case MyRvLinkEventType.HostDebug:
					return MyRvLinkHostDebug.Decode(eventBytes);
				case MyRvLinkEventType.DeviceSessionStatus:
					return MyRvLinkDeviceSessionStatus.Decode(eventBytes);
				case MyRvLinkEventType.TemperatureSensorStatus:
					return MyRvLinkTemperatureSensorStatus.Decode(eventBytes);
				case MyRvLinkEventType.JaycoTbbStatus:
					return MyRvLinkJaycoTbbStatus.Decode(eventBytes);
				case MyRvLinkEventType.AwningSensorStatus:
					return MyRvLinkAwningSensorStatus.Decode(eventBytes);
				case MyRvLinkEventType.CloudGatewayStatus:
					return MyRvLinkCloudGatewayStatus.Decode(eventBytes);
				case MyRvLinkEventType.BrakingSystemStatus:
					return MyRvLinkBrakingSystemStatus.Decode(eventBytes);
				case MyRvLinkEventType.BatteryMonitorStatus:
					return MyRvLinkBatteryMonitorStatus.Decode(eventBytes);
				case MyRvLinkEventType.ReFlashBootloader:
					return MyRvLinkBootLoaderStatus.Decode(eventBytes);
				case MyRvLinkEventType.DoorLockStatus:
					return MyRvLinkDoorLockStatus.Decode(eventBytes);
				case MyRvLinkEventType.DimmableLightExtendedStatus:
					return MyRvLinkDimmableLightStatusExtended.Decode(eventBytes);
				default:
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Unknown Event ");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkEventType);
					defaultInterpolatedStringHandler.AppendLiteral("(0x");
					defaultInterpolatedStringHandler.AppendFormatted((byte)myRvLinkEventType, "X2");
					defaultInterpolatedStringHandler.AppendLiteral(")");
					throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				}
			}
			catch (Exception ex)
			{
				if (showErrors)
				{
					TaggedLog.Error("MyRvLinkEventFactory", "Error processing event " + eventBytes.DebugDump(0, eventBytes.Count) + ": " + ex.Message);
				}
				return null;
			}
		}
	}
}
