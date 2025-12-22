using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice.BlockTransfer;

namespace OneControl.Direct.MyRvLink
{
	internal class MyRvLinkCommandDeviceBlockWriteData : MyRvLinkCommand
	{
		protected const int RequiredHeaderSize = 12;

		private const int MaxDataLength = 128;

		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int BlockIdStartIndex = 5;

		private const int AddressOffsetIndex = 7;

		private const int SizeIndex = 11;

		private const int DataStartIndex = 12;

		private byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandDeviceBlockWriteData";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.DeviceBlockWriteData;


		protected override int MinPayloadLength => 12;

		private int MaxPayloadLength => 140;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public BlockTransferBlockId BlockId => DecodeBlockId(_rawData);

		public byte Size => _rawData[11];

		public uint AddressOffset => DecodeAddressOffset(_rawData);

		public IReadOnlyList<byte> Data => new ArraySegment<byte>(_rawData, 12, _rawData.Length - 12);

		public MyRvLinkCommandDeviceBlockWriteData(ushort clientCommandId, byte deviceTableId, byte deviceId, BlockTransferBlockId blockId, uint addressOffset, byte size, IReadOnlyList<byte>? data = null)
		{
			_rawData = new byte[MaxPayloadLength];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			_rawData.SetValueUInt16((ushort)blockId, 5);
			UpdateCommand(addressOffset, size, data);
		}

		protected MyRvLinkCommandDeviceBlockWriteData(IReadOnlyList<byte> rawData)
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

		public void UpdateCommand(uint addressOffset, byte size, IReadOnlyList<byte>? data = null)
		{
			_rawData.SetValueUInt32(addressOffset, 7);
			_rawData[11] = size;
			if (data != null)
			{
				if (data!.Count > 128)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(57, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Block transfer data size is greater than the maximum of: ");
					defaultInterpolatedStringHandler.AppendFormatted(128);
					throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				for (int i = 0; i < data!.Count; i++)
				{
					_rawData[12 + i] = data![i];
				}
			}
			ValidateCommand(_rawData, ClientCommandId);
		}

		public override IMyRvLinkCommandEvent DecodeCommandEvent(IMyRvLinkCommandEvent commandEvent)
		{
			if (commandEvent is MyRvLinkCommandResponseSuccess myRvLinkCommandResponseSuccess)
			{
				if (myRvLinkCommandResponseSuccess.IsCommandCompleted)
				{
					return new MyRvLinkDeviceBlockWriteDataCompletedCommandResponse(myRvLinkCommandResponseSuccess);
				}
				return new MyRvLinkDeviceBlockWriteDataCommandResponse(myRvLinkCommandResponseSuccess);
			}
			return commandEvent;
		}

		public static MyRvLinkCommandDeviceBlockWriteData Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandDeviceBlockWriteData(rawData);
		}

		protected static BlockTransferBlockId DecodeBlockId(IReadOnlyList<byte> decodeBuffer)
		{
			return (BlockTransferBlockId)decodeBuffer.GetValueUInt16(5);
		}

		protected static uint DecodeAddressOffset(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer.GetValueUInt32(7);
		}

		public override IReadOnlyList<byte> Encode()
		{
			int num = Size + 12;
			int num2 = Math.Min(num, _rawData.Length);
			if (num2 != num)
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Size truncated from ");
				defaultInterpolatedStringHandler.AppendFormatted(Size);
				defaultInterpolatedStringHandler.AppendLiteral(" to ");
				defaultInterpolatedStringHandler.AppendFormatted(num2);
				TaggedLog.Warning(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return new ArraySegment<byte>(_rawData, 0, num2);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(97, 8);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", DeviceId: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Block Id: ");
			defaultInterpolatedStringHandler.AppendFormatted(BlockId);
			defaultInterpolatedStringHandler.AppendLiteral(" Address Offset: ");
			defaultInterpolatedStringHandler.AppendFormatted(DecodeAddressOffset(_rawData), "X2");
			defaultInterpolatedStringHandler.AppendLiteral(" Size: ");
			defaultInterpolatedStringHandler.AppendFormatted(Size);
			defaultInterpolatedStringHandler.AppendLiteral(" Raw Data: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
