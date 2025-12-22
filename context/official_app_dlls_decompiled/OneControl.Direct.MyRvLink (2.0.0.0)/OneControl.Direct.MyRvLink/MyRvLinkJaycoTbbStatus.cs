using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using IDS.Portable.Devices.JaycoTbbGateway;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkJaycoTbbStatus : MyRvLinkEvent<MyRvLinkJaycoTbbStatus>
	{
		private const int MaxPayloadLength = 9;

		private const int DeviceTableIdIndex = 1;

		private const int DeviceIdIndex = 2;

		private const int AddressIndex = 3;

		private const int DataIndex = 5;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.JaycoTbbStatus;

		protected override byte[] _rawData { get; }

		protected override int MinPayloadLength => 2;

		public int DeviceId => _rawData[2];

		public byte DeviceTableId => _rawData[1];

		public ushort Address => _rawData.GetValueUInt16(3);

		public uint Data => _rawData.Length switch
		{
			6 => _rawData[5], 
			7 => _rawData.GetValueUInt16(5), 
			8 => (uint)((_rawData[5] << 16) | (_rawData[6] << 8) | _rawData[7]), 
			9 => _rawData.GetValueUInt32(5), 
			_ => 0u, 
		};

		protected MyRvLinkJaycoTbbStatus(IReadOnlyList<byte> rawData)
		{
			ValidateEventRawDataBasic(rawData);
			_rawData = rawData.ToNewArray(0, rawData.Count);
			if (rawData.Count > 9)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkJaycoTbbStatus));
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(9);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		public static MyRvLinkJaycoTbbStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkJaycoTbbStatus(rawData);
		}

		public LogicalDeviceJaycoTbbStatus GetStatus()
		{
			LogicalDeviceJaycoTbbStatus logicalDeviceJaycoTbbStatus = new LogicalDeviceJaycoTbbStatus();
			int num = _rawData.Length - 3;
			logicalDeviceJaycoTbbStatus.Update(new ArraySegment<byte>(_rawData, 3, num), num);
			return logicalDeviceJaycoTbbStatus;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 5);
			defaultInterpolatedStringHandler.AppendLiteral("DeviceId: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId);
			defaultInterpolatedStringHandler.AppendLiteral(" DeviceTableId: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId);
			defaultInterpolatedStringHandler.AppendLiteral(" Address: ");
			defaultInterpolatedStringHandler.AppendFormatted(Address);
			defaultInterpolatedStringHandler.AppendLiteral(" Data: ");
			defaultInterpolatedStringHandler.AppendFormatted(Data);
			defaultInterpolatedStringHandler.AppendLiteral(" Raw data: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
