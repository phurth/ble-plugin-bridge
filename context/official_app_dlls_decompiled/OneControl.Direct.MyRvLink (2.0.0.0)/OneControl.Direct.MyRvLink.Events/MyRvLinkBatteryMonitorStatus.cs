using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using OneControl.Devices.BatteryMonitor;

namespace OneControl.Direct.MyRvLink.Events
{
	internal class MyRvLinkBatteryMonitorStatus : MyRvLinkEvent<MyRvLinkBatteryMonitorStatus>
	{
		private const int PayloadVersion1Length = 13;

		private const int PayloadVersion2Length = 19;

		private const int MaxPayloadLength = 19;

		private const int DeviceTableIdIndex = 1;

		private const int DeviceIdIndex = 2;

		private const int StatusIndex = 3;

		private const int ExtendedStatusIndex = 11;

		private const int BatteryMonitorStatusSize = 8;

		private const int BatteryMonitorExtendedStatusVersion1Size = 2;

		private const int BatteryMonitorExtendedStatusVersion2Size = 8;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.BatteryMonitorStatus;

		protected override byte[] _rawData { get; }

		protected override int MinPayloadLength => 2;

		public int DeviceId => _rawData[2];

		public byte DeviceTableId => _rawData[1];

		protected MyRvLinkBatteryMonitorStatus(IReadOnlyList<byte> rawData)
		{
			ValidateEventRawDataBasic(rawData);
			if (rawData.Count > 19)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkBatteryMonitorStatus));
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(19);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkBatteryMonitorStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkBatteryMonitorStatus(rawData);
		}

		public (LogicalDeviceBatteryMonitorStatus status, LogicalDeviceBatteryMonitorStatusExtended statusExtended) GetStatusAndExtendedStatus()
		{
			LogicalDeviceBatteryMonitorStatus logicalDeviceBatteryMonitorStatus = new LogicalDeviceBatteryMonitorStatus();
			logicalDeviceBatteryMonitorStatus.Update(new ArraySegment<byte>(_rawData, 3, 8), 8);
			LogicalDeviceBatteryMonitorStatusExtended logicalDeviceBatteryMonitorStatusExtended = new LogicalDeviceBatteryMonitorStatusExtended();
			switch (_rawData.Length)
			{
			case 13:
				logicalDeviceBatteryMonitorStatusExtended.Update(new ArraySegment<byte>(_rawData, 11, 2), 2);
				break;
			case 19:
				logicalDeviceBatteryMonitorStatusExtended.Update(new ArraySegment<byte>(_rawData, 11, 8), 8);
				break;
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkBatteryMonitorStatus));
				defaultInterpolatedStringHandler.AppendLiteral(" unexpected payload size ");
				defaultInterpolatedStringHandler.AppendFormatted(_rawData.Length);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			}
			return (logicalDeviceBatteryMonitorStatus, logicalDeviceBatteryMonitorStatusExtended);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(37, 3);
			defaultInterpolatedStringHandler.AppendLiteral("DeviceId: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId);
			defaultInterpolatedStringHandler.AppendLiteral(" DeviceTableId: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId);
			defaultInterpolatedStringHandler.AppendLiteral(" Raw data: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
