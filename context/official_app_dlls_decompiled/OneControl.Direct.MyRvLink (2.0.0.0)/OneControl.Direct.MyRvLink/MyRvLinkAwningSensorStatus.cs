using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices.AwningSensor;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkAwningSensorStatus : MyRvLinkEventDevicesMultiByte<MyRvLinkAwningSensorStatus>
	{
		private const int AwningSensorStatusSize = 8;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.AwningSensorStatus;

		protected override int BytesPerDevice => 9;

		public MyRvLinkAwningSensorStatus(byte deviceTableId, params (byte DeviceId, LogicalDeviceAwningSensorStatus AwningSensorStatus)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceAwningSensorStatus) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 8);
				num += 8;
			}
		}

		protected MyRvLinkAwningSensorStatus(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkAwningSensorStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkAwningSensorStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceAwningSensorStatus AwningSensorStatus)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceAwningSensorStatus logicalDeviceAwningSensorStatus = new LogicalDeviceAwningSensorStatus();
				logicalDeviceAwningSensorStatus.Update(new ArraySegment<byte>(_rawData, index + 1, 8), 8);
				yield return (b, logicalDeviceAwningSensorStatus);
			}
		}

		public LogicalDeviceAwningSensorStatus? GetAwningSensorStatus(int deviceId)
		{
			LogicalDeviceAwningSensorStatus logicalDeviceAwningSensorStatus = new LogicalDeviceAwningSensorStatus();
			if (!GetAwningSensorStatus(deviceId, logicalDeviceAwningSensorStatus))
			{
				return null;
			}
			return logicalDeviceAwningSensorStatus;
		}

		public bool GetAwningSensorStatus(int deviceId, LogicalDeviceAwningSensorStatus awningSensorStatus)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					awningSensorStatus.Update(new ArraySegment<byte>(_rawData, i + 1, 8), 8);
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
				handler.AppendFormatted(item.AwningSensorStatus);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
