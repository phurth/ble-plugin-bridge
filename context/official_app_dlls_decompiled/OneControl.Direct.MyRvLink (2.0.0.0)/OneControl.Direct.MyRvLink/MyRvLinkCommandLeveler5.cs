using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using OneControl.Devices.Leveler.Type5;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandLeveler5 : MyRvLinkCommand
	{
		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int DeviceCommandStartIndex = 5;

		private const int DeviceCommandByteIndex = 6;

		private const int CommandDataCommandTypeBitIndex = 0;

		private const int MinimumCommandLength = 1;

		private const int CancelCurrentCommandLength = 1;

		private const int AutoLevelCommandLength = 3;

		private const int AutoHitchCommandLength = 1;

		private const int AutoRetractCommandLength = 3;

		private const int AutoExtendCommandLength = 3;

		private const int ManualMovementCommandLength = 3;

		private const int SetZeroPointCommandLength = 3;

		private const int SetConfigCommandLength = 3;

		private const int ManualUnrestrictedMovementCommandLength = 3;

		private const int NumberOfAutoHitchBytesToRemove = 2;

		private readonly byte[] _rawData;

		private const int MaxPayloadLength = 8;

		protected virtual string LogTag { get; } = "MyRvLinkCommandLeveler5";


		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		protected override int MinPayloadLength => 6;

		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.Leveler5Command;


		public MyRvLinkCommandLeveler5(ushort clientCommandId, byte deviceTableId, byte deviceId, ILogicalDeviceLevelerCommandType5 command)
		{
			IReadOnlyList<byte> readOnlyList = command.RawData;
			int num = readOnlyList.Count;
			int num2;
			if (command.CommandByte == 0)
			{
				num2 = 5;
				num = 1;
			}
			else
			{
				if (command.CommandByte != 96)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(37, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Invalid/Unknown Leveler Command Code ");
					defaultInterpolatedStringHandler.AppendFormatted(command.CommandByte);
					throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				if (num < 1)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(76, 4);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
					defaultInterpolatedStringHandler.AppendFormatted(LogTag);
					defaultInterpolatedStringHandler.AppendLiteral(" because size is ");
					defaultInterpolatedStringHandler.AppendFormatted(readOnlyList.Count);
					defaultInterpolatedStringHandler.AppendLiteral(" when expecting at least ");
					defaultInterpolatedStringHandler.AppendFormatted(6);
					defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
					defaultInterpolatedStringHandler.AppendFormatted(readOnlyList.DebugDump(0, readOnlyList.Count));
					throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				switch (readOnlyList[0])
				{
				case 0:
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Invalid Leveler Operation Code ");
					defaultInterpolatedStringHandler.AppendFormatted(LogicalDeviceLevelerCommandType5.LevelerOperationCode.None);
					throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				case 1:
					num2 = 7;
					break;
				case 2:
					num -= 2;
					readOnlyList = readOnlyList.ToNewArray(0, num);
					num2 = 5;
					break;
				case 3:
					num2 = 7;
					break;
				case 8:
					num2 = 7;
					break;
				case 4:
					num2 = 7;
					break;
				case 5:
					num2 = 7;
					break;
				case 6:
					num2 = 7;
					break;
				case 7:
					num2 = 7;
					break;
				default:
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(75, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
					defaultInterpolatedStringHandler.AppendFormatted(LogTag);
					defaultInterpolatedStringHandler.AppendLiteral(" because ");
					defaultInterpolatedStringHandler.AppendFormatted(readOnlyList[6]);
					defaultInterpolatedStringHandler.AppendLiteral(" does not match a known command. bytes: ");
					defaultInterpolatedStringHandler.AppendFormatted(readOnlyList.DebugDump(0, readOnlyList.Count));
					throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				}
			}
			if (num2 != 4 + num)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(100, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(LogTag);
				defaultInterpolatedStringHandler.AppendLiteral(" because size is ");
				defaultInterpolatedStringHandler.AppendFormatted(readOnlyList.Count);
				defaultInterpolatedStringHandler.AppendLiteral(" which does not match the expected size for the command: ");
				defaultInterpolatedStringHandler.AppendFormatted(readOnlyList.DebugDump(0, readOnlyList.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = new byte[num2 + 1];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			int num3 = 5;
			foreach (byte item in readOnlyList)
			{
				_rawData[num3++] = item;
			}
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandLeveler5(IReadOnlyList<byte> rawData)
		{
			ValidateCommand(rawData);
			if (rawData.Count > 8)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(CommandType);
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(8);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (rawData.Count < MinPayloadLength)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(CommandType);
				defaultInterpolatedStringHandler.AppendLiteral(" received less then ");
				defaultInterpolatedStringHandler.AppendFormatted(MinPayloadLength);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkCommandLeveler5 Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandLeveler5(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override IMyRvLinkCommandEvent DecodeCommandEvent(IMyRvLinkCommandEvent commandEvent)
		{
			if (!(commandEvent is MyRvLinkCommandResponseSuccess result))
			{
				if (commandEvent is MyRvLinkCommandResponseFailure response)
				{
					return new MyRvLinkCommandLeveler5ResponseFailure(response);
				}
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
				defaultInterpolatedStringHandler.AppendLiteral("DecodeCommandEvent unexpected event type for ");
				defaultInterpolatedStringHandler.AppendFormatted(commandEvent);
				TaggedLog.Warning(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				return commandEvent;
			}
			return result;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(55, 5);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", Device Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
