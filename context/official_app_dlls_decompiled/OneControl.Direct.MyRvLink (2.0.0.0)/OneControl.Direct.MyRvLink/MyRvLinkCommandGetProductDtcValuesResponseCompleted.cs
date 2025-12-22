using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandGetProductDtcValuesResponseCompleted : MyRvLinkCommandResponseSuccess
	{
		protected const int DtcCountIndex = 0;

		protected override int MinExtendedDataLength => 1;

		public byte DtcCount => base.ExtendedData[0];

		public MyRvLinkCommandGetProductDtcValuesResponseCompleted(ushort clientCommandId, byte dtcCount)
			: base(clientCommandId, commandCompleted: true, EncodeExtendedData(dtcCount))
		{
		}

		public MyRvLinkCommandGetProductDtcValuesResponseCompleted(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkCommandGetProductDtcValuesResponseCompleted(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		private static IReadOnlyList<byte> EncodeExtendedData(byte dtcCount)
		{
			int num = 1;
			byte[] array = new byte[num];
			array[0] = dtcCount;
			return new ArraySegment<byte>(array, 0, num);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkCommandGetDevicesMetadataResponseCompleted");
			defaultInterpolatedStringHandler.AppendLiteral(" DTC Count: ");
			defaultInterpolatedStringHandler.AppendFormatted(DtcCount);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
