using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using OneControl.Devices.BrakingSystem;

namespace OneControl.Direct.MyRvLink
{
	internal class MyRvLinkBrakingSystemStatus : MyRvLinkEvent<MyRvLinkBrakingSystemStatus>
	{
		public const string LogTag = "MyRvLinkBrakingSystemStatus";

		public const int DeviceTableIdIndex = 1;

		public const int DeviceIdIndex = 2;

		public const int StatusIndex = 3;

		public const int AbsStatusPayloadSizeV1 = 6;

		public const int AbsStatusPayloadSizeV2 = 8;

		public const int AbsStatusSizeV1 = 9;

		public const int AbsStatusSizeV2 = 11;

		private const int MaxPayloadLength = 11;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.BrakingSystemStatus;

		protected override int MinPayloadLength => 9;

		protected override byte[] _rawData { get; }

		public byte DeviceId => _rawData[2];

		public byte DeviceTableId => _rawData[1];

		protected MyRvLinkBrakingSystemStatus(IReadOnlyList<byte> rawData)
		{
			ValidateEventRawDataBasic(rawData);
			if (rawData.Count > 11)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkBrakingSystemStatus));
				defaultInterpolatedStringHandler.AppendLiteral(" received more ");
				defaultInterpolatedStringHandler.AppendLiteral("than ");
				defaultInterpolatedStringHandler.AppendFormatted(11);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkBrakingSystemStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkBrakingSystemStatus(rawData);
		}

		public LogicalDeviceBrakingSystemStatus? GetBrakingSystemStatus()
		{
			LogicalDeviceBrakingSystemStatus logicalDeviceBrakingSystemStatus = new LogicalDeviceBrakingSystemStatus();
			try
			{
				switch (_rawData.Length)
				{
				case 9:
					logicalDeviceBrakingSystemStatus.Update(new ArraySegment<byte>(_rawData, 3, 6), 6);
					return logicalDeviceBrakingSystemStatus;
				case 11:
					logicalDeviceBrakingSystemStatus.Update(new ArraySegment<byte>(_rawData, 3, 8), 8);
					return logicalDeviceBrakingSystemStatus;
				default:
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 4);
					defaultInterpolatedStringHandler.AppendLiteral(" received invalid data size of ");
					defaultInterpolatedStringHandler.AppendFormatted(_rawData.Length);
					defaultInterpolatedStringHandler.AppendLiteral(" when ");
					defaultInterpolatedStringHandler.AppendFormatted(9);
					defaultInterpolatedStringHandler.AppendLiteral(" or ");
					defaultInterpolatedStringHandler.AppendFormatted(11);
					defaultInterpolatedStringHandler.AppendLiteral(" was expected, raw bytes: ");
					defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
					throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				}
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to parse braking system status for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkBrakingSystemStatus));
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Warning("MyRvLinkBrakingSystemStatus", defaultInterpolatedStringHandler.ToStringAndClear());
				return null;
			}
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
