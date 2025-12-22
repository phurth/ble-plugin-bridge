using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkHourMeterStatus : MyRvLinkEventDevicesMultiByte<MyRvLinkHourMeterStatus>
	{
		private const int HourMeterStatusSize = 5;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.HourMeterStatus;

		protected override int BytesPerDevice => 6;

		public MyRvLinkHourMeterStatus(byte deviceTableId, params (byte DeviceId, LogicalDeviceHourMeterStatus HourMeterStatus)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceHourMeterStatus) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 5);
				num += 5;
			}
		}

		protected MyRvLinkHourMeterStatus(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkHourMeterStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkHourMeterStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceHourMeterStatus HourMeterStatus)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceHourMeterStatus logicalDeviceHourMeterStatus = new LogicalDeviceHourMeterStatus();
				logicalDeviceHourMeterStatus.Update(new ArraySegment<byte>(_rawData, index + 1, 5), 5);
				yield return (b, logicalDeviceHourMeterStatus);
			}
		}

		public LogicalDeviceHourMeterStatus? GetHourMeterStatus(int deviceId)
		{
			LogicalDeviceHourMeterStatus logicalDeviceHourMeterStatus = new LogicalDeviceHourMeterStatus();
			if (!GetHourMeterStatus(deviceId, logicalDeviceHourMeterStatus))
			{
				return null;
			}
			return logicalDeviceHourMeterStatus;
		}

		public bool GetHourMeterStatus(int deviceId, LogicalDeviceHourMeterStatus hourMeterStatus)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					hourMeterStatus.Update(new ArraySegment<byte>(_rawData, i + 1, 5), 5);
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
				handler.AppendFormatted(item.HourMeterStatus);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
