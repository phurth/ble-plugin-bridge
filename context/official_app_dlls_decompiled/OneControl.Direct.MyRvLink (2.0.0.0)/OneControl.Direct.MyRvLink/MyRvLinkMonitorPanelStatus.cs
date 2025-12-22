using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkMonitorPanelStatus : MyRvLinkEventDevicesMultiByte<MyRvLinkMonitorPanelStatus>
	{
		private const int MonitorPanelStatusSize = 1;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.MonitorPanelStatus;

		protected override int BytesPerDevice => 2;

		public MyRvLinkMonitorPanelStatus(byte deviceTableId, params (byte DeviceId, LogicalDeviceMonitorPanelStatus MonitorPanelStatus)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceMonitorPanelStatus) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 1);
				num++;
			}
		}

		protected MyRvLinkMonitorPanelStatus(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkMonitorPanelStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkMonitorPanelStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceMonitorPanelStatus MonitorPanelStatus)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceMonitorPanelStatus logicalDeviceMonitorPanelStatus = new LogicalDeviceMonitorPanelStatus();
				logicalDeviceMonitorPanelStatus.Update(new ArraySegment<byte>(_rawData, index + 1, 1), 1);
				yield return (b, logicalDeviceMonitorPanelStatus);
			}
		}

		public LogicalDeviceMonitorPanelStatus? GetMonitorPanelStatus(int deviceId)
		{
			LogicalDeviceMonitorPanelStatus logicalDeviceMonitorPanelStatus = new LogicalDeviceMonitorPanelStatus();
			if (!GetMonitorPanelStatus(deviceId, logicalDeviceMonitorPanelStatus))
			{
				return null;
			}
			return logicalDeviceMonitorPanelStatus;
		}

		public bool GetMonitorPanelStatus(int deviceId, LogicalDeviceMonitorPanelStatus monitorPanelStatus)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					monitorPanelStatus.Update(new ArraySegment<byte>(_rawData, i + 1, 1), 1);
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
				handler.AppendFormatted(item.MonitorPanelStatus);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
