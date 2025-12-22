using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice.BlockTransfer;

namespace OneControl.Direct.MyRvLink
{
	internal class MyRvLinkCommandStartDeviceBlockTransfer : MyRvLinkCommand
	{
		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int BlockIdStartIndex = 5;

		private const int OptionsIndex = 7;

		private const int StartAddressStartIndex = 8;

		private const int SizeStartIndex = 12;

		private readonly byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandStartDeviceBlockTransfer";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.StartDeviceBlockTransfer;


		protected override int MinPayloadLength => 8;

		private int MaxPayloadLength => 16;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public BlockTransferBlockId BlockId => DecodeBlockId(_rawData);

		public BlockTransferStartOptions Options => (BlockTransferStartOptions)_rawData[7];

		public uint StartAddress => DecodeStartAddress(_rawData);

		public uint Size => DecodeSize(_rawData);

		public MyRvLinkCommandStartDeviceBlockTransfer(ushort clientCommandId, byte deviceTableId, byte deviceId, BlockTransferBlockId blockId, BlockTransferStartOptions options, uint? startAddress, uint? size)
		{
			_rawData = new byte[MaxPayloadLength];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			_rawData.SetValueUInt16((ushort)blockId, 5);
			_rawData[7] = (byte)options;
			if (startAddress.HasValue)
			{
				_rawData.SetValueUInt32(startAddress.Value, 8);
			}
			if (size.HasValue)
			{
				_rawData.SetValueUInt32(size.Value, 12);
			}
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandStartDeviceBlockTransfer(IReadOnlyList<byte> rawData)
		{
			ValidateCommand(rawData);
			if (rawData.Count > MaxPayloadLength)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(CommandType);
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(MaxPayloadLength);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkCommandStartDeviceBlockTransfer Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandStartDeviceBlockTransfer(rawData);
		}

		protected static BlockTransferBlockId DecodeBlockId(IReadOnlyList<byte> decodeBuffer)
		{
			return (BlockTransferBlockId)decodeBuffer.GetValueUInt16(5);
		}

		protected static uint DecodeStartAddress(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer.GetValueUInt32(8);
		}

		protected static uint DecodeSize(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer.GetValueUInt32(12);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(107, 9);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", DeviceId: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendLiteral("Block Id: ");
			defaultInterpolatedStringHandler.AppendFormatted(BlockId);
			defaultInterpolatedStringHandler.AppendLiteral(" Options: ");
			defaultInterpolatedStringHandler.AppendFormatted(Options);
			defaultInterpolatedStringHandler.AppendLiteral(" StartAddress: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(StartAddress, "X");
			defaultInterpolatedStringHandler.AppendLiteral(" Size: ");
			defaultInterpolatedStringHandler.AppendFormatted(Size);
			defaultInterpolatedStringHandler.AppendLiteral(" Raw Data: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
