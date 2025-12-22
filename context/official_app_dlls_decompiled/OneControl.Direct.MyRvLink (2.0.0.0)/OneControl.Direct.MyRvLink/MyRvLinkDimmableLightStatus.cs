using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDimmableLightStatus : MyRvLinkEventDevicesMultiByte<MyRvLinkDimmableLightStatus>
	{
		private const int DimmableLightStatusSize = 8;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.DimmableLightStatus;

		protected override int BytesPerDevice => 9;

		public MyRvLinkDimmableLightStatus(byte deviceTableId, params (byte DeviceId, LogicalDeviceLightDimmableStatus DimmableStatus)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceLightDimmableStatus) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 8);
				num += 8;
			}
		}

		protected MyRvLinkDimmableLightStatus(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkDimmableLightStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkDimmableLightStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceLightDimmableStatus DimmableStatus)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceLightDimmableStatus logicalDeviceLightDimmableStatus = new LogicalDeviceLightDimmableStatus();
				logicalDeviceLightDimmableStatus.Update(new ArraySegment<byte>(_rawData, index + 1, 8), 8);
				yield return (b, logicalDeviceLightDimmableStatus);
			}
		}

		public LogicalDeviceLightDimmableStatus? GetDimmableStatus(int deviceId)
		{
			LogicalDeviceLightDimmableStatus logicalDeviceLightDimmableStatus = new LogicalDeviceLightDimmableStatus();
			if (!GetDimmableStatus(deviceId, logicalDeviceLightDimmableStatus))
			{
				return null;
			}
			return logicalDeviceLightDimmableStatus;
		}

		public bool GetDimmableStatus(int deviceId, LogicalDeviceLightDimmableStatus dimmableStatus)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					dimmableStatus.Update(new ArraySegment<byte>(_rawData, i + 1, 8), 8);
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
				handler.AppendFormatted(item.DimmableStatus);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
