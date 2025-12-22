using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice.BlockTransfer;

namespace OneControl.Direct.MyRvLink
{
	internal class MyRvLinkDeviceBlockWriteDataCompletedCommandResponse : MyRvLinkCommandResponseSuccess
	{
		private const string LogTag = "MyRvLinkGetDeviceBlockListCommandResponse";

		protected const int BlockIdIndex = 0;

		protected const int ResponseDataLength = 2;

		protected override int MinExtendedDataLength => 2;

		public BlockTransferBlockId BlockId => DecodeBlockId();

		public MyRvLinkDeviceBlockWriteDataCompletedCommandResponse(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkDeviceBlockWriteDataCompletedCommandResponse(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		private BlockTransferBlockId DecodeBlockId()
		{
			if (base.ExtendedData == null || base.ExtendedData.Count != 2)
			{
				throw new MyRvLinkDecoderException("Unable to decode BlockID, received unexpected data size.");
			}
			return (BlockTransferBlockId)base.ExtendedData.GetValueUInt16(0);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkGetDeviceBlockListCommandResponse");
			defaultInterpolatedStringHandler.AppendLiteral(" BlockId: ");
			defaultInterpolatedStringHandler.AppendFormatted(BlockId);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
