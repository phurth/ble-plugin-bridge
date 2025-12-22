using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandSetDevicePidWithAddress : MyRvLinkCommand
	{
		private const int RequiredPidAddressSize = 2;

		private const int MaxPidValueSize = 4;

		private const int MaxPayloadLength = 15;

		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int PidIdIndex = 5;

		private const int PidSessionIdIndex = 7;

		private const int PidAddressIndex = 9;

		private const int PidValueIndex = 11;

		private readonly byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandSetDevicePidWithAddress";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.SetDevicePidWithAddress;


		protected override int MinPayloadLength => 11;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public Pid Pid => (Pid)_rawData.GetValueUInt16(5);

		public ushort Address => _rawData.GetValueUInt16(9);

		public SESSION_ID SessionId => _rawData.GetValueUInt16(7);

		public uint PidValue => DecodePidValue();

		public MyRvLinkCommandSetDevicePidWithAddress(ushort clientCommandId, byte deviceTableId, byte deviceId, Pid pidId, SESSION_ID sessionId, ushort pidAddress, uint pidValue, LogicalDeviceSessionType pidWriteAccess)
		{
			int minPayloadLength = MinPayloadLength;
			byte[] array = new byte[4];
			int num = array.SetValueBigEndianRemoveLeadingZeros(pidValue, 0, 4);
			_rawData = new byte[minPayloadLength + num];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			_rawData.SetValueUInt16((ushort)pidId, 5);
			_rawData.SetValueUInt16(sessionId, 7);
			_rawData.SetValueUInt16(pidAddress, 9);
			for (int i = 0; i < num; i++)
			{
				_rawData[i + minPayloadLength] = array[i];
			}
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandSetDevicePidWithAddress(IReadOnlyList<byte> rawData)
		{
			ValidateCommand(rawData);
			if (rawData.Count > 15)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(56, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(MyRvLinkCommandSetDevicePidWithAddress));
				defaultInterpolatedStringHandler.AppendLiteral(" because greater then ");
				defaultInterpolatedStringHandler.AppendFormatted(15);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			_rawData = rawData.ToNewArray(0, rawData.Count);
		}

		public static MyRvLinkCommandSetDevicePidWithAddress Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandSetDevicePidWithAddress(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		protected uint DecodePidValue()
		{
			if (_rawData.Length <= MinPayloadLength)
			{
				return 0u;
			}
			int num = MathCommon.Clamp(_rawData.Length - 11, 0, 4) + 11;
			uint num2 = 0u;
			for (int i = 11; i < num; i++)
			{
				num2 <<= 8;
				num2 |= _rawData[i];
			}
			return num2;
		}

		public override IMyRvLinkCommandEvent DecodeCommandEvent(IMyRvLinkCommandEvent commandEvent)
		{
			if (commandEvent is MyRvLinkCommandResponseSuccess myRvLinkCommandResponseSuccess)
			{
				if (myRvLinkCommandResponseSuccess.IsCommandCompleted)
				{
					return new MyRvLinkCommandSetDevicePidWithAddressResponseCompleted(myRvLinkCommandResponseSuccess);
				}
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(53, 2);
				defaultInterpolatedStringHandler.AppendLiteral("DecodeCommandEvent for ");
				defaultInterpolatedStringHandler.AppendFormatted(LogTag);
				defaultInterpolatedStringHandler.AppendLiteral(" Received UNEXPECTED event of ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseSuccess);
				TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				return commandEvent;
			}
			return commandEvent;
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 5);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(" Pid: ");
			defaultInterpolatedStringHandler.AppendFormatted(Pid);
			defaultInterpolatedStringHandler.AppendLiteral(" SessionId: ");
			defaultInterpolatedStringHandler.AppendFormatted(SessionId);
			defaultInterpolatedStringHandler.AppendLiteral(" Value: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(PidValue, "X");
			defaultInterpolatedStringHandler.AppendLiteral("]");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
