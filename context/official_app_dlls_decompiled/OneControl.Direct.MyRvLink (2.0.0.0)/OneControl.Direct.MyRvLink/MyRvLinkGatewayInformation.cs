using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkGatewayInformation : MyRvLinkEvent<MyRvLinkGatewayInformation>
	{
		private const int MaxPayloadLength = 13;

		private const int ProtocolVersionStartIndex = 1;

		private const int OptionsIndex = 2;

		private const int DeviceCountIndex = 3;

		private const int DeviceTableIdIndex = 4;

		private const int DeviceTableCrcStartIndex = 5;

		private const int DeviceMetadataCrcStartIndex = 9;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.GatewayInformation;

		protected override int MinPayloadLength => 13;

		protected override byte[] _rawData { get; }

		public MyRvLinkProtocolVersionMajor ProtocolVersionMajor => (MyRvLinkProtocolVersionMajor)_rawData[1];

		public MyRvLinkGatewayInformationOptions Options => (MyRvLinkGatewayInformationOptions)_rawData[2];

		public bool IsProductionMode => !Options.HasFlag(MyRvLinkGatewayInformationOptions.ConfigurationMode);

		public int DeviceCount => _rawData[3];

		public byte DeviceTableId => _rawData[4];

		public uint DeviceTableCrc => _rawData.GetValueUInt32(5);

		public uint DeviceMetadataTableCrc => _rawData.GetValueUInt32(9);

		public bool IsExactDeviceTableMatch(byte deviceTableId, uint deviceTableCrc)
		{
			if (DeviceTableId == deviceTableId)
			{
				return DeviceTableCrc == deviceTableCrc;
			}
			return false;
		}

		public MyRvLinkGatewayInformation(byte protocolVersion, MyRvLinkGatewayInformationOptions options, byte deviceCount, byte deviceTableId, uint deviceTableCrc, uint deviceMetadataTableCrc)
		{
			_rawData = new byte[13];
			_rawData[0] = (byte)EventType;
			_rawData[1] = protocolVersion;
			_rawData[2] = (byte)options;
			_rawData[3] = deviceCount;
			_rawData[4] = deviceTableId;
			_rawData.SetValueUInt32(deviceTableCrc, 5);
			_rawData.SetValueUInt32(deviceMetadataTableCrc, 9);
		}

		protected MyRvLinkGatewayInformation(IReadOnlyList<byte> rawData)
		{
			ValidateEventRawDataBasic(rawData);
			_rawData = rawData.ToNewArray(0, rawData.Count);
			if (rawData.Count > 13)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkGatewayInformation));
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(13);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		public static MyRvLinkGatewayInformation Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkGatewayInformation(rawData);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(60, 7);
			defaultInterpolatedStringHandler.AppendLiteral("Version: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ProtocolVersionMajor, "X");
			defaultInterpolatedStringHandler.AppendLiteral(", Devices: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceCount);
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(" CRC: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableCrc, "X4");
			defaultInterpolatedStringHandler.AppendLiteral("/0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceMetadataTableCrc, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(", Options: ");
			defaultInterpolatedStringHandler.AppendFormatted(Options.DebugDumpAsFlags());
			defaultInterpolatedStringHandler.AppendLiteral(": ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
