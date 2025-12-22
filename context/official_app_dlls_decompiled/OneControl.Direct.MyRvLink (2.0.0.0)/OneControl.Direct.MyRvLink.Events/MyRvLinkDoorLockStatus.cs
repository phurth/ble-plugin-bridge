using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using OneControl.Devices.DoorLock;

namespace OneControl.Direct.MyRvLink.Events
{
	internal class MyRvLinkDoorLockStatus : MyRvLinkEvent<MyRvLinkDoorLockStatus>
	{
		private const int MaxPayloadLength = 7;

		private const int DeviceTableIdIndex = 1;

		private const int DeviceIdIndex = 2;

		private const int StatusIndex = 3;

		private const int DoorLockStatusSize = 4;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.DoorLockStatus;

		protected override byte[] _rawData { get; }

		protected override int MinPayloadLength => 2;

		public int DeviceId => _rawData[2];

		public byte DeviceTableId => _rawData[1];

		protected MyRvLinkDoorLockStatus(IReadOnlyList<byte> rawData)
		{
			ValidateEventRawDataBasic(rawData);
			if (rawData.Count > 7)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkDoorLockStatus));
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(7);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkDoorLockStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkDoorLockStatus(rawData);
		}

		public LogicalDeviceDoorLockStatus GetStatus()
		{
			LogicalDeviceDoorLockStatus logicalDeviceDoorLockStatus = new LogicalDeviceDoorLockStatus();
			logicalDeviceDoorLockStatus.Update(new ArraySegment<byte>(_rawData, 3, 4), 4);
			return logicalDeviceDoorLockStatus;
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
