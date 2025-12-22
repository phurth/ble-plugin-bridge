using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDimmableLightStatusExtended : MyRvLinkEvent<MyRvLinkDimmableLightStatusExtended>
	{
		private const int PayloadLength = 11;

		private const int MaxPayloadLength = 11;

		private const int DeviceTableIdIndex = 1;

		private const int DeviceIdIndex = 2;

		private const int StatusIndex = 3;

		private const int StatusExtendedSize = 8;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.DimmableLightExtendedStatus;

		protected override byte[] _rawData { get; }

		protected override int MinPayloadLength => 11;

		public int DeviceId => _rawData[2];

		public byte DeviceTableId => _rawData[1];

		protected MyRvLinkDimmableLightStatusExtended(IReadOnlyList<byte> rawData)
		{
			ValidateEventRawDataBasic(rawData);
			if (rawData.Count > 11)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkDimmableLightStatusExtended));
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(11);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkDimmableLightStatusExtended Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkDimmableLightStatusExtended(rawData);
		}

		public LogicalDeviceLightDimmableStatusExtended GetExtendedStatus()
		{
			LogicalDeviceLightDimmableStatusExtended logicalDeviceLightDimmableStatusExtended = new LogicalDeviceLightDimmableStatusExtended();
			logicalDeviceLightDimmableStatusExtended.Update(new ArraySegment<byte>(_rawData, 3, 8), 8);
			return logicalDeviceLightDimmableStatusExtended;
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
