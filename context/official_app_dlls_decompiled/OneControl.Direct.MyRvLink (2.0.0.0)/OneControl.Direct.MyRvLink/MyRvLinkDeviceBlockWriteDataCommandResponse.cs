using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice.BlockTransfer;

namespace OneControl.Direct.MyRvLink
{
	internal class MyRvLinkDeviceBlockWriteDataCommandResponse : MyRvLinkCommandResponseSuccess
	{
		private const string LogTag = "MyRvLinkDeviceBlockWriteDataCommandResponse";

		protected const int BlockIdIndex = 0;

		protected const int Crc32Index = 2;

		protected const int ResponseDataLength = 6;

		protected override int MinExtendedDataLength => 6;

		public BlockTransferBlockId BlockId => DecodeBlockId();

		public uint Crc32 => DecodeCrc32();

		public MyRvLinkDeviceBlockWriteDataCommandResponse(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkDeviceBlockWriteDataCommandResponse(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		private BlockTransferBlockId DecodeBlockId()
		{
			if (base.ExtendedData == null || base.ExtendedData.Count != 6)
			{
				throw new MyRvLinkDecoderException("Unable to decode BlockID, received unexpected data size.");
			}
			return (BlockTransferBlockId)base.ExtendedData.GetValueUInt16(0);
		}

		private uint DecodeCrc32()
		{
			if (base.ExtendedData == null || base.ExtendedData.Count != 6)
			{
				throw new MyRvLinkDecoderException("Unable to decode crc, received unexpected data size.");
			}
			return base.ExtendedData.GetValueUInt32(2);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 4);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkDeviceBlockWriteDataCommandResponse");
			defaultInterpolatedStringHandler.AppendLiteral(" BlockId ");
			defaultInterpolatedStringHandler.AppendFormatted(BlockId);
			defaultInterpolatedStringHandler.AppendLiteral(" Crc32: ");
			defaultInterpolatedStringHandler.AppendFormatted(Crc32);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
