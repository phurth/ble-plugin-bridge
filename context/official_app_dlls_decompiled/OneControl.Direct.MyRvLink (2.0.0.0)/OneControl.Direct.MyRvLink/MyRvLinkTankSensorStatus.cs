using System;
using System.Collections.Generic;
using System.Text;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkTankSensorStatus : MyRvLinkEventDevicesMultiByte<MyRvLinkTankSensorStatus>
	{
		public override MyRvLinkEventType EventType => MyRvLinkEventType.TankSensorStatus;

		protected override int BytesPerDevice => 2;

		public MyRvLinkTankSensorStatus(byte deviceTableId, params (byte DeviceId, byte Percent)[] devicePositions)
			: base(deviceTableId, devicePositions.Length)
		{
			int num = 2;
			for (int i = 0; i < devicePositions.Length; i++)
			{
				(byte, byte) tuple = devicePositions[i];
				_rawData[num++] = tuple.Item1;
				_rawData[num++] = tuple.Item2;
			}
		}

		protected MyRvLinkTankSensorStatus(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkTankSensorStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkTankSensorStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, byte Percent)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				byte b2 = _rawData[index + 1];
				yield return (b, b2);
			}
		}

		public byte? GetPercent(int deviceId)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					return _rawData[i + 1];
				}
			}
			return null;
		}

		protected override void DevicesToStringBuilder(StringBuilder stringBuilder)
		{
			foreach (var item in EnumerateStatus())
			{
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 3, stringBuilder);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    0x");
				handler.AppendFormatted(item.DeviceId, "X2");
				handler.AppendLiteral(": ");
				handler.AppendFormatted(item.Percent);
				handler.AppendLiteral("%");
				stringBuilder.Append(ref handler);
			}
		}
	}
}
