using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCommandActionMovement : MyRvLinkCommand
	{
		public enum RvLinkMovementCommand : byte
		{
			Stop,
			Invalid,
			Forward,
			Reverse,
			HomeReset,
			AutoForward,
			AutoReverse
		}

		private const int DeviceTableIdIndex = 3;

		private const int DeviceIdIndex = 4;

		private const int DeviceStateIndex = 5;

		public const byte MovementBitMask = 15;

		public const byte Relay1StateBit = 16;

		public const byte Relay2StateBit = 32;

		public const byte RelayEnergizedValidBit = 128;

		private readonly byte[] _rawData;

		protected virtual string LogTag { get; } = "MyRvLinkCommandActionMovement";


		public override MyRvLinkCommandType CommandType { get; } = MyRvLinkCommandType.ActionMovement;


		protected override int MinPayloadLength => 6;

		private int MaxPayloadLength => MinPayloadLength;

		public override ushort ClientCommandId => MyRvLinkCommand.DecodeClientCommandId(_rawData);

		public byte DeviceTableId => _rawData[3];

		public byte DeviceId => _rawData[4];

		public int DeviceCount => 1;

		public RelayHBridgeDirection Direction => (RelayHBridgeDirection)(_rawData[5] & 0xF);

		public bool TurningOnRelay1
		{
			get
			{
				if ((_rawData[5] & 0x80u) != 0)
				{
					return (_rawData[5] & 0x10) != 0;
				}
				return false;
			}
		}

		public bool TurningOnRelay2
		{
			get
			{
				if ((_rawData[5] & 0x80u) != 0)
				{
					return (_rawData[5] & 0x20) != 0;
				}
				return false;
			}
		}

		public MyRvLinkCommandActionMovement(ushort clientCommandId, byte deviceTableId, byte deviceId, ILogicalDeviceId logicalId, HBridgeCommand command)
		{
			_rawData = new byte[MaxPayloadLength];
			_rawData.SetValueUInt16(clientCommandId, 0);
			_rawData[2] = (byte)CommandType;
			_rawData[3] = deviceTableId;
			_rawData[4] = deviceId;
			bool flag = false;
			RelayHBridgeEnergized relayHBridgeEnergized;
			RvLinkMovementCommand rvLinkMovementCommand;
			switch (command)
			{
			case HBridgeCommand.Forward:
				relayHBridgeEnergized = RelayHBridgeDirection.Forward.ConvertToRelayEnergized(logicalId);
				rvLinkMovementCommand = RvLinkMovementCommand.Forward;
				flag = true;
				break;
			case HBridgeCommand.Reverse:
				relayHBridgeEnergized = RelayHBridgeDirection.Reverse.ConvertToRelayEnergized(logicalId);
				rvLinkMovementCommand = RvLinkMovementCommand.Reverse;
				flag = true;
				break;
			case HBridgeCommand.AutoForward:
				relayHBridgeEnergized = RelayHBridgeEnergized.None;
				rvLinkMovementCommand = RvLinkMovementCommand.AutoForward;
				break;
			case HBridgeCommand.AutoReverse:
				relayHBridgeEnergized = RelayHBridgeEnergized.None;
				rvLinkMovementCommand = RvLinkMovementCommand.AutoReverse;
				break;
			case HBridgeCommand.HomeReset:
				relayHBridgeEnergized = RelayHBridgeEnergized.None;
				rvLinkMovementCommand = RvLinkMovementCommand.HomeReset;
				break;
			default:
				relayHBridgeEnergized = RelayHBridgeEnergized.None;
				rvLinkMovementCommand = RvLinkMovementCommand.Stop;
				flag = true;
				break;
			}
			byte b = (byte)rvLinkMovementCommand;
			if (flag)
			{
				if (relayHBridgeEnergized == RelayHBridgeEnergized.Relay1)
				{
					b = (byte)(b | 0x10u);
				}
				if (relayHBridgeEnergized == RelayHBridgeEnergized.Relay2)
				{
					b = (byte)(b | 0x20u);
				}
				b = (byte)(b | 0x80u);
			}
			_rawData[5] = b;
			ValidateCommand(_rawData, clientCommandId);
		}

		protected MyRvLinkCommandActionMovement(IReadOnlyList<byte> rawData)
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

		public static MyRvLinkCommandActionMovement Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCommandActionMovement(rawData);
		}

		public override IReadOnlyList<byte> Encode()
		{
			return new ArraySegment<byte>(_rawData, 0, _rawData.Length);
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(88, 8);
			defaultInterpolatedStringHandler.AppendFormatted(LogTag);
			defaultInterpolatedStringHandler.AppendLiteral("[Client Command Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(ClientCommandId, "X4");
			defaultInterpolatedStringHandler.AppendLiteral(", Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", DeviceId: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(", ");
			defaultInterpolatedStringHandler.AppendFormatted(Direction);
			defaultInterpolatedStringHandler.AppendLiteral(", LegacyRelay1: ");
			defaultInterpolatedStringHandler.AppendFormatted(TurningOnRelay1);
			defaultInterpolatedStringHandler.AppendLiteral(", LegacyRelay2: ");
			defaultInterpolatedStringHandler.AppendFormatted(TurningOnRelay2);
			defaultInterpolatedStringHandler.AppendLiteral("]: ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
