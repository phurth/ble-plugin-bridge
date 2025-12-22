using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using OneControl.Devices.BootLoader;

namespace OneControl.Direct.MyRvLink.Events
{
	internal class MyRvLinkBootLoaderStatus : MyRvLinkEvent<MyRvLinkBootLoaderStatus>
	{
		private const int MaxPayloadLength = 5;

		private const int DeviceTableIdIndex = 1;

		private const int DeviceIdIndex = 2;

		private const int ReFlashVersionIndex = 3;

		private const int ReFlashVersionSize = 1;

		private const int SPBVersionSize = 1;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.ReFlashBootloader;

		protected override int MinPayloadLength => 2;

		protected override byte[] _rawData { get; }

		public int DeviceId => _rawData[2];

		public byte DeviceTableId => _rawData[1];

		protected MyRvLinkBootLoaderStatus(IReadOnlyList<byte> rawData)
		{
			ValidateEventRawDataBasic(rawData);
			if (rawData.Count > 5)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkBootLoaderStatus));
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(5);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkBootLoaderStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkBootLoaderStatus(rawData);
		}

		public LogicalDeviceReflashBootLoaderStatus GetStatus()
		{
			LogicalDeviceReflashBootLoaderStatus logicalDeviceReflashBootLoaderStatus = new LogicalDeviceReflashBootLoaderStatus();
			logicalDeviceReflashBootLoaderStatus.Update(new ArraySegment<byte>(_rawData, 3, 2), 2);
			return logicalDeviceReflashBootLoaderStatus;
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
