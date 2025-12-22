using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	internal class MyRvLinkTankSensorStatusV2 : MyRvLinkEvent<MyRvLinkTankSensorStatusV2>
	{
		public const string LogTag = "MyRvLinkTankSensorStatusV2";

		public const int DeviceTableIdIndex = 1;

		public const int DeviceIdIndex = 2;

		public const int StatusIndex = 3;

		public const int HeaderSize = 3;

		public const int StatusMinSize = 1;

		public const int StatusMaxSize = 8;

		private const int _minPayloadLength = 4;

		private const int _maxPayloadLength = 11;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.TankSensorStatusV2;

		protected override int MinPayloadLength => 4;

		protected override byte[] _rawData { get; }

		public byte DeviceId => _rawData[2];

		public byte DeviceTableId => _rawData[1];

		protected MyRvLinkTankSensorStatusV2(IReadOnlyList<byte> rawData)
		{
			ValidateEventRawDataBasic(rawData);
			if (rawData.Count > 11)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkTankSensorStatusV2");
				defaultInterpolatedStringHandler.AppendLiteral(" received more ");
				defaultInterpolatedStringHandler.AppendLiteral("than ");
				defaultInterpolatedStringHandler.AppendFormatted(11);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkTankSensorStatusV2 Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkTankSensorStatusV2(rawData);
		}

		public LogicalDeviceTankSensorStatus? GetTankSensorStatus()
		{
			LogicalDeviceTankSensorStatus logicalDeviceTankSensorStatus = new LogicalDeviceTankSensorStatus();
			try
			{
				switch (_rawData.Length)
				{
				case 4:
					logicalDeviceTankSensorStatus.Update(new ArraySegment<byte>(_rawData, 3, 1), 1);
					return logicalDeviceTankSensorStatus;
				case 11:
					logicalDeviceTankSensorStatus.Update(new ArraySegment<byte>(_rawData, 3, 8), 8);
					return logicalDeviceTankSensorStatus;
				default:
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(65, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
					defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkTankSensorStatusV2");
					defaultInterpolatedStringHandler.AppendLiteral(" into ");
					defaultInterpolatedStringHandler.AppendFormatted("LogicalDeviceTankSensorStatus");
					defaultInterpolatedStringHandler.AppendLiteral(" because size of ");
					defaultInterpolatedStringHandler.AppendFormatted(_rawData.Length);
					defaultInterpolatedStringHandler.AppendLiteral(" was unexpected.");
					throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				}
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("MyRvLinkTankSensorStatusV2", "Unable to update status " + ex.Message);
				return logicalDeviceTankSensorStatus;
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
