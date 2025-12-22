using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public abstract class MyRvLinkCommandEvent : IMyRvLinkCommandEvent, IMyRvLinkEvent
	{
		private const string LogTag = "MyRvLinkCommandEvent";

		public const byte CommandCompletedMask = 128;

		protected static IReadOnlyList<byte> EmptyExtendedData = Array.Empty<byte>();

		private readonly int _indexOfExtendedData;

		private readonly byte[]? _extendedRawData;

		private const int EventTypeIndex = 0;

		private const int ClientCommandIdStartIndex = 1;

		private const int CommandEventTypeIndex = 3;

		public ushort ClientCommandId { get; }

		public bool IsCommandCompleted => (CommandResponseType & (MyRvLinkCommandResponseType)128) == (MyRvLinkCommandResponseType)128;

		protected MyRvLinkCommandResponseType CommandResponseType { get; }

		public IReadOnlyList<byte> ExtendedData
		{
			get
			{
				byte[]? extendedRawData = _extendedRawData;
				return GetExtendedData(0, (extendedRawData != null) ? extendedRawData!.Length : 0);
			}
		}

		public int ExtendedDataLength
		{
			get
			{
				byte[]? extendedRawData = _extendedRawData;
				if (extendedRawData == null)
				{
					return 0;
				}
				return extendedRawData!.Length;
			}
		}

		public MyRvLinkEventType EventType { get; } = MyRvLinkEventType.DeviceCommand;


		protected abstract int MinPayloadLength { get; }

		protected virtual int MinExtendedDataLength => 0;

		protected MyRvLinkCommandEvent(ushort clientCommandId, MyRvLinkCommandResponseType commandResponseType, int indexOfExtendedData, IReadOnlyList<byte>? extendedData = null)
		{
			ClientCommandId = clientCommandId;
			CommandResponseType = commandResponseType;
			_indexOfExtendedData = indexOfExtendedData;
			_extendedRawData = ((extendedData != null) ? Enumerable.ToArray(extendedData) : null);
		}

		protected MyRvLinkCommandEvent(IReadOnlyList<byte> rawData, MyRvLinkCommandResponseType commandResponseType, int indexOfExtendedData)
		{
			if (rawData.Count < MinPayloadLength)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkGatewayInformation));
				defaultInterpolatedStringHandler.AppendLiteral(" received less then ");
				defaultInterpolatedStringHandler.AppendFormatted(MinPayloadLength);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (rawData.Count < MinPayloadLength + MinExtendedDataLength)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(76, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkGatewayInformation));
				defaultInterpolatedStringHandler.AppendLiteral(" received less extended data then required bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (DecodeEventType(rawData) != EventType)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkCommandResponseSuccess));
				defaultInterpolatedStringHandler.AppendLiteral(" because Event Type isn't ");
				defaultInterpolatedStringHandler.AppendFormatted(EventType);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			CommandResponseType = commandResponseType;
			if (DecodeCommandResponseType(rawData) != commandResponseType)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(65, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkCommandResponseSuccess));
				defaultInterpolatedStringHandler.AppendLiteral(" because Command Response Type isn't ");
				defaultInterpolatedStringHandler.AppendFormatted(commandResponseType);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			ClientCommandId = DecodeClientCommandId(rawData);
			_indexOfExtendedData = indexOfExtendedData;
			_extendedRawData = TryMakeExtendedData(rawData, indexOfExtendedData);
		}

		public IReadOnlyList<byte> GetExtendedData(int offset)
		{
			return GetExtendedData(offset, ExtendedDataLength - offset);
		}

		public IReadOnlyList<byte> GetExtendedData(int offset, int count)
		{
			if (_extendedRawData == null || count == 0)
			{
				return EmptyExtendedData;
			}
			int num = ExtendedDataLength - offset;
			if (num < 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 3);
				defaultInterpolatedStringHandler.AppendLiteral("offset beyond end of buffer ");
				defaultInterpolatedStringHandler.AppendFormatted(ExtendedDataLength);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted(offset);
				defaultInterpolatedStringHandler.AppendLiteral(" > ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (count > num)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 3);
				defaultInterpolatedStringHandler.AppendLiteral("size beyond end of buffer ");
				defaultInterpolatedStringHandler.AppendFormatted(ExtendedDataLength);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted(count);
				defaultInterpolatedStringHandler.AppendLiteral(" > ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return new ArraySegment<byte>(_extendedRawData, offset, count);
		}

		protected static byte[]? TryMakeExtendedData(IReadOnlyList<byte> source, int indexOfExtendedData)
		{
			int num = source.Count - indexOfExtendedData;
			if (num >= 0)
			{
				return source.ToNewArray(indexOfExtendedData, num);
			}
			return null;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Command(");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId);
			defaultInterpolatedStringHandler.AppendLiteral(") Success");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		protected static MyRvLinkEventType DecodeEventType(IReadOnlyList<byte> decodeBuffer)
		{
			return (MyRvLinkEventType)decodeBuffer[0];
		}

		protected static ushort DecodeClientCommandId(IReadOnlyList<byte> decodeBuffer)
		{
			return decodeBuffer.GetValueUInt16(1);
		}

		protected static MyRvLinkCommandResponseType DecodeCommandResponseType(IReadOnlyList<byte> decodeBuffer)
		{
			return (MyRvLinkCommandResponseType)decodeBuffer[3];
		}

		protected virtual void EncodeBaseEventIntoBuffer(byte[] encodeBuffer)
		{
			encodeBuffer[0] = (byte)EventType;
			encodeBuffer.SetValueUInt16(ClientCommandId, 1);
			encodeBuffer[3] = (byte)CommandResponseType;
			if (_extendedRawData != null)
			{
				ExtendedData.ToExistingArray(0, encodeBuffer, _indexOfExtendedData, ExtendedData.Count);
			}
		}

		public static IMyRvLinkCommandEvent DecodeCommandEvent(IReadOnlyList<byte> rawData, Func<int, IMyRvLinkCommand?> getPendingCommand)
		{
			MyRvLinkCommandResponseType myRvLinkCommandResponseType = DecodeCommandResponseType(rawData);
			if (rawData == null)
			{
				throw new MyRvLinkDecoderException("Decode unknown for null data");
			}
			IMyRvLinkCommandEvent myRvLinkCommandEvent;
			switch (myRvLinkCommandResponseType)
			{
			case MyRvLinkCommandResponseType.SuccessMultipleResponse:
			case MyRvLinkCommandResponseType.SuccessCompleted:
				myRvLinkCommandEvent = new MyRvLinkCommandResponseSuccess(rawData);
				break;
			case MyRvLinkCommandResponseType.FailureMultipleResponse:
			case MyRvLinkCommandResponseType.FailureCompleted:
				myRvLinkCommandEvent = new MyRvLinkCommandResponseFailure(rawData);
				break;
			default:
			{
				MyRvLinkEventType myRvLinkEventType = (MyRvLinkEventType)((rawData.Count > 0) ? rawData[0] : 0);
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Decode unknown for ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkEventType);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			}
			try
			{
				ushort num = DecodeClientCommandId(rawData);
				IMyRvLinkCommand myRvLinkCommand = getPendingCommand(num);
				IMyRvLinkCommandEvent result = myRvLinkCommand?.DecodeCommandEvent(myRvLinkCommandEvent) ?? myRvLinkCommandEvent;
				if (myRvLinkCommand == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(94, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Processing Event Received For CommandId 0x");
					defaultInterpolatedStringHandler.AppendFormatted(num, "X");
					defaultInterpolatedStringHandler.AppendLiteral(" found pending command NOT FOUND Raw Response Data: ");
					defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
					TaggedLog.Information("MyRvLinkCommandEvent", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				else
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(85, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Processing Event Received For CommandId 0x");
					defaultInterpolatedStringHandler.AppendFormatted(num, "X");
					defaultInterpolatedStringHandler.AppendLiteral(" found pending command ");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommand?.ToString() ?? "NOT FOUND");
					defaultInterpolatedStringHandler.AppendLiteral(" Raw Response Data: ");
					defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
					TaggedLog.Information("MyRvLinkCommandEvent", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				return result;
			}
			catch (Exception)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Error processing event for command ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandEvent);
				TaggedLog.Error("MyRvLinkCommandEvent", defaultInterpolatedStringHandler.ToStringAndClear());
				return myRvLinkCommandEvent;
			}
		}
	}
}
