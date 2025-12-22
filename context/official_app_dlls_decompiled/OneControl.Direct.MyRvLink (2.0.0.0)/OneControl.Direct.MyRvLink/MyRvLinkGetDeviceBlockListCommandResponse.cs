using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice.BlockTransfer;

namespace OneControl.Direct.MyRvLink
{
	internal class MyRvLinkGetDeviceBlockListCommandResponse : MyRvLinkCommandResponseSuccess
	{
		private const string LogTag = "MyRvLinkGetDeviceBlockListCommandResponse";

		private List<BlockTransferBlockId>? _blockIds;

		protected const int BlockSentCountIndex = 0;

		protected const int BlockSentCountLength = 1;

		protected const int BlockIdIndex = 1;

		protected const int BlockIdLength = 2;

		protected override int MinExtendedDataLength => 1;

		public IReadOnlyList<BlockTransferBlockId> BlockIds => _blockIds ?? (_blockIds = DecodeBlockIds());

		public MyRvLinkGetDeviceBlockListCommandResponse(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public MyRvLinkGetDeviceBlockListCommandResponse(MyRvLinkCommandResponseSuccess response)
			: base(response.ClientCommandId, response.IsCommandCompleted, response.ExtendedData)
		{
		}

		protected List<BlockTransferBlockId> DecodeBlockIds()
		{
			HashSet<BlockTransferBlockId> hashSet = new HashSet<BlockTransferBlockId>();
			if (base.ExtendedData == null)
			{
				return Enumerable.ToList(hashSet);
			}
			byte b = base.ExtendedData[0];
			if (base.ExtendedDataLength != b * 2 + 1)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode block ids, expected ");
				defaultInterpolatedStringHandler.AppendFormatted(b);
				defaultInterpolatedStringHandler.AppendLiteral(" but received ");
				defaultInterpolatedStringHandler.AppendFormatted(b * 2);
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			for (int i = 1; i < base.ExtendedDataLength; i += 2)
			{
				ushort valueUInt = base.ExtendedData.GetValueUInt16(i);
				hashSet.Add((BlockTransferBlockId)valueUInt);
			}
			return Enumerable.ToList(hashSet);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Command(0x");
			defaultInterpolatedStringHandler.AppendFormatted(base.ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(") Response ");
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkGetDeviceBlockListCommandResponse");
			defaultInterpolatedStringHandler.AppendLiteral(" Blocks Sent: ");
			defaultInterpolatedStringHandler.AppendFormatted(base.ExtendedData[0]);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
