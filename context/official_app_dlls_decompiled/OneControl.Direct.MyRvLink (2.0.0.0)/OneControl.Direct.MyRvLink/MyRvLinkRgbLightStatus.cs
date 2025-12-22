using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices.LightRgb;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkRgbLightStatus : MyRvLinkEventDevicesMultiByte<MyRvLinkRgbLightStatus>
	{
		private const int RgbLightStatusSize = 8;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.RgbLightStatus;

		protected override int BytesPerDevice => 9;

		public MyRvLinkRgbLightStatus(byte deviceTableId, params (byte DeviceId, LogicalDeviceLightRgbStatus DimmableStatus)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceLightRgbStatus) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 8);
				num += 8;
			}
		}

		protected MyRvLinkRgbLightStatus(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkRgbLightStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkRgbLightStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceLightRgbStatus RgbStatus)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceLightRgbStatus logicalDeviceLightRgbStatus = new LogicalDeviceLightRgbStatus();
				logicalDeviceLightRgbStatus.Update(new ArraySegment<byte>(_rawData, index + 1, 8), 8);
				yield return (b, logicalDeviceLightRgbStatus);
			}
		}

		public LogicalDeviceLightRgbStatus? GetRgbStatus(int deviceId)
		{
			LogicalDeviceLightRgbStatus logicalDeviceLightRgbStatus = new LogicalDeviceLightRgbStatus();
			if (!GetRgbStatus(deviceId, logicalDeviceLightRgbStatus))
			{
				return null;
			}
			return logicalDeviceLightRgbStatus;
		}

		public bool GetRgbStatus(int deviceId, LogicalDeviceLightRgbStatus rgbStatus)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					rgbStatus.Update(new ArraySegment<byte>(_rawData, i + 1, 8), 8);
					return true;
				}
			}
			return false;
		}

		protected override void DevicesToStringBuilder(StringBuilder stringBuilder)
		{
			foreach (var item in EnumerateStatus())
			{
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 3, stringBuilder);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    0x");
				handler.AppendFormatted(item.DeviceId, "X2");
				handler.AppendLiteral(": ");
				handler.AppendFormatted(item.RgbStatus);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
