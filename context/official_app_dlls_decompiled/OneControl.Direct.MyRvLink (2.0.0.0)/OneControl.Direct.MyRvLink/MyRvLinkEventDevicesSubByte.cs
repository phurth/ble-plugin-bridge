using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public abstract class MyRvLinkEventDevicesSubByte<TEvent> : MyRvLinkEventDevices<TEvent> where TEvent : IMyRvLinkEvent
	{
		protected enum AllowedDevicesPerByte
		{
			Two = 2,
			Four = 4,
			Eight = 8
		}

		protected abstract AllowedDevicesPerByte DevicesPerByte { get; }

		protected int DeviceBitsPerStatus => DevicesPerByte switch
		{
			AllowedDevicesPerByte.Two => 4, 
			AllowedDevicesPerByte.Four => 2, 
			AllowedDevicesPerByte.Eight => 1, 
			_ => throw new MyRvLinkException("Unsupported DevicesPerByte"), 
		};

		protected int DeviceStatusBitMask => DevicesPerByte switch
		{
			AllowedDevicesPerByte.Two => 15, 
			AllowedDevicesPerByte.Four => 3, 
			AllowedDevicesPerByte.Eight => 1, 
			_ => throw new MyRvLinkException("Unsupported DevicesPerByte"), 
		};

		protected abstract int DeviceTableIdIndex { get; }

		protected abstract int DeviceCountIndex { get; }

		protected abstract int DeviceStatusStartIndex { get; }

		public byte DeviceTableId => _rawData[DeviceTableIdIndex];

		public int DeviceCount => _rawData[DeviceCountIndex];

		protected override int MaxPayloadLength(int deviceCount)
		{
			return MinPayloadLength + (int)Math.Ceiling((double)deviceCount / (double)DevicesPerByte);
		}

		protected MyRvLinkEventDevicesSubByte(byte deviceTableId, int deviceCount)
			: base(deviceCount)
		{
			_rawData[DeviceTableIdIndex] = deviceTableId;
			_rawData[DeviceCountIndex] = (byte)deviceCount;
		}

		protected MyRvLinkEventDevicesSubByte(IReadOnlyList<byte> rawData)
			: base(rawData.ToNewArray(0, rawData.Count))
		{
			byte deviceCount = rawData[DeviceCountIndex];
			int num = MaxPayloadLength(deviceCount);
			if (rawData.Count > num)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(EventType);
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		private (int byteOffset, int bitShift) GetStatusIndex(int deviceId, int startDeviceId)
		{
			int num = deviceId - startDeviceId;
			int num2 = num / (int)DevicesPerByte;
			int num3 = (int)(DevicesPerByte - 1 - num % (int)DevicesPerByte) * DeviceBitsPerStatus;
			return (num2, num3);
		}

		public byte GetDeviceStatus(byte deviceId, int startDeviceId)
		{
			(int, int) statusIndex = GetStatusIndex(deviceId, startDeviceId);
			int num = statusIndex.Item1 + DeviceStatusStartIndex;
			if (statusIndex.Item1 < 0)
			{
				return 0;
			}
			if (num >= _rawData.Length)
			{
				return 0;
			}
			return (byte)((_rawData[num] >> statusIndex.Item2) & (uint)DeviceStatusBitMask);
		}

		protected IEnumerable<(byte DeviceId, byte status)> EnumerateStatus(int startDeviceId)
		{
			int endDeviceId = startDeviceId + DeviceCount;
			for (byte deviceId = (byte)startDeviceId; deviceId < endDeviceId; deviceId = (byte)(deviceId + 1))
			{
				yield return (deviceId, GetDeviceStatus(deviceId, startDeviceId));
			}
		}

		public void SetDeviceStatus(byte deviceId, byte status, int startDeviceId)
		{
			(int, int) statusIndex = GetStatusIndex(deviceId, startDeviceId);
			int num = statusIndex.Item1 + DeviceStatusStartIndex;
			if (statusIndex.Item1 >= 0 && num < _rawData.Length)
			{
				byte b = (byte)(status & DeviceStatusBitMask);
				byte b2 = _rawData[num];
				b2 = (byte)(b2 & ~(DeviceStatusBitMask << statusIndex.Item2));
				if (b != 0)
				{
					b2 = (byte)(b2 | (b << statusIndex.Item2));
				}
				_rawData[num] = b2;
			}
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(23, 4);
			defaultInterpolatedStringHandler.AppendFormatted(EventType);
			defaultInterpolatedStringHandler.AppendLiteral(" Table Id: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X2");
			defaultInterpolatedStringHandler.AppendLiteral(" Total: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceCount);
			defaultInterpolatedStringHandler.AppendLiteral(": ");
			defaultInterpolatedStringHandler.AppendFormatted(_rawData.DebugDump());
			StringBuilder stringBuilder = new StringBuilder(defaultInterpolatedStringHandler.ToStringAndClear());
			try
			{
				DevicesToStringBuilder(stringBuilder);
			}
			catch (Exception ex)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(31, 2, stringBuilder2);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    ERROR Trying to Get Device ");
				handler.AppendFormatted(ex.Message);
				stringBuilder2.Append(ref handler);
			}
			return stringBuilder.ToString();
		}

		protected abstract void DevicesToStringBuilder(StringBuilder stringBuilder);
	}
}
