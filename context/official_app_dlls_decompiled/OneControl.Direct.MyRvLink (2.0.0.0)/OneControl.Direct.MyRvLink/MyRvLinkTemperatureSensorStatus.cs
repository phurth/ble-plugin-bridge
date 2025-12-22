using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices.TemperatureSensor;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkTemperatureSensorStatus : MyRvLinkEventDevicesMultiByte<MyRvLinkTemperatureSensorStatus>
	{
		private const int TemperatureSensorStatusSize = 8;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.TemperatureSensorStatus;

		protected override int BytesPerDevice => 9;

		public MyRvLinkTemperatureSensorStatus(byte deviceTableId, params (byte DeviceId, LogicalDeviceTemperatureSensorStatus TemperatureSensorStatus)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceTemperatureSensorStatus) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 8);
				num += 8;
			}
		}

		protected MyRvLinkTemperatureSensorStatus(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkTemperatureSensorStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkTemperatureSensorStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceTemperatureSensorStatus TemperatureSensorStatus)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceTemperatureSensorStatus logicalDeviceTemperatureSensorStatus = new LogicalDeviceTemperatureSensorStatus();
				logicalDeviceTemperatureSensorStatus.Update(new ArraySegment<byte>(_rawData, index + 1, 8), 8);
				yield return (b, logicalDeviceTemperatureSensorStatus);
			}
		}

		public LogicalDeviceTemperatureSensorStatus? GetTemperatureSensorStatus(int deviceId)
		{
			LogicalDeviceTemperatureSensorStatus logicalDeviceTemperatureSensorStatus = new LogicalDeviceTemperatureSensorStatus();
			if (!GetTemperatureSensorStatus(deviceId, logicalDeviceTemperatureSensorStatus))
			{
				return null;
			}
			return logicalDeviceTemperatureSensorStatus;
		}

		public bool GetTemperatureSensorStatus(int deviceId, LogicalDeviceTemperatureSensorStatus temperatureSensorStatus)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					temperatureSensorStatus.Update(new ArraySegment<byte>(_rawData, i + 1, 8), 8);
					return true;
				}
			}
			return false;
		}

		protected override void DevicesToStringBuilder(StringBuilder stringBuilder)
		{
			foreach (var item in EnumerateStatus())
			{
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(6, 3, stringBuilder);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    ");
				handler.AppendFormatted(item.DeviceId);
				handler.AppendLiteral(": ");
				handler.AppendFormatted(item.TemperatureSensorStatus);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
