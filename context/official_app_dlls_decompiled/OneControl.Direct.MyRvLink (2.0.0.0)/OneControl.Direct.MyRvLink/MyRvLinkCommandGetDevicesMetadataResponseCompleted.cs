using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetDevicesMetadataResponseCompleted : MyRvLinkCommandResponseSuccess
	{
		protected const int DeviceMetadataTableCrcIndex = 0;

		protected const int DeviceCountIndex = 4;

		protected override int MinExtendedDataLength => 5;

		public uint DeviceMetadataTableCrc => base.ExtendedData.GetValueUInt32(0);

		public byte DeviceCount => base.ExtendedData[4];

		public MyRvLinkCommandGetDevicesMetadataResponseCompleted(ushort clientCommandId, uint deviceMetadataTableCrc, byte deviceCount)
			: base(clientCommandId, commandCompleted: true, EncodeExtendedData(deviceMetadataTableCrc, deviceCount))
		{
		}

		public MyRvLinkCommandGetDevicesMetadataResponseCompleted(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandGetDevicesMetadataResponseCompleted(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		private static IReadOnlyList<byte> EncodeExtendedData(uint deviceMetadataTableCrc, byte deviceCount)
		{
			int num = 5;
			byte[] array = new byte[num];
			array.SetValueUInt32(deviceMetadataTableCrc, 0);
			array[4] = deviceCount;
			return new ArraySegment<byte>(array, 0, num);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(68, 4);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandGetDevicesMetadataResponseCompleted");
			defaultInterpolatedStringHandler.AppendLiteral(" ResponseReceivedDeviceTableCrc: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceMetadataTableCrc, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(" DeviceCount: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceCount);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
