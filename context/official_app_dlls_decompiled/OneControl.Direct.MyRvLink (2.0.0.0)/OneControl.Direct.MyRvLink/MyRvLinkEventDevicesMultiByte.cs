using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public abstract class MyRvLinkEventDevicesMultiByte<TEvent> : MyRvLinkEventDevices<TEvent> where TEvent : IMyRvLinkEvent
	{
		protected const int DeviceTableIdIndex = 1;

		protected const int DeviceStatusStartIndex = 2;

		protected abstract int BytesPerDevice { get; }

		protected sealed override int MinPayloadLength => 2;

		public byte DeviceTableId => _rawData[1];

		public int DeviceCount => DeviceCountFromBufferSize(_rawData.Length);

		protected override int MaxPayloadLength(int deviceCount)
		{
			return MinPayloadLength + deviceCount * BytesPerDevice;
		}

		protected MyRvLinkEventDevicesMultiByte(byte deviceTableId, int deviceCount)
			: base(deviceCount)
		{
			_rawData[1] = deviceTableId;
		}

		protected MyRvLinkEventDevicesMultiByte(IReadOnlyList<byte> rawData)
			: base(rawData.ToNewArray(0, rawData.Count))
		{
			if (DeviceCount == 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(124, 5);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(EventType);
				defaultInterpolatedStringHandler.AppendLiteral(" received ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.Count);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes which isn't enough to hold a single full device status which requires ");
				defaultInterpolatedStringHandler.AppendFormatted(BytesPerDevice);
				defaultInterpolatedStringHandler.AppendLiteral(" + ");
				defaultInterpolatedStringHandler.AppendFormatted(MinPayloadLength);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes: ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			int num = MaxPayloadLength(DeviceCount);
			if (rawData.Count > num)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(95, 5);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to decode data for ");
				defaultInterpolatedStringHandler.AppendFormatted(EventType);
				defaultInterpolatedStringHandler.AppendLiteral(" received more then ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				defaultInterpolatedStringHandler.AppendLiteral(" bytes (Number of Devices ");
				defaultInterpolatedStringHandler.AppendFormatted(DeviceCount);
				defaultInterpolatedStringHandler.AppendLiteral(" Bytes Per Device ");
				defaultInterpolatedStringHandler.AppendFormatted(BytesPerDevice);
				defaultInterpolatedStringHandler.AppendLiteral(")  : ");
				defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
				throw new MyRvLinkDecoderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		protected int DeviceCountFromBufferSize(int rawBufferSize)
		{
			int num = (rawBufferSize - MinPayloadLength) / BytesPerDevice;
			if (num < 0)
			{
				return 0;
			}
			return num;
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
