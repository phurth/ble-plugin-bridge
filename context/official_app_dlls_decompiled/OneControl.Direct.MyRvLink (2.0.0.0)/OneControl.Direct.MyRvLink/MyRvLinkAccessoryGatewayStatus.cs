using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices.AccessoryGateway;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkAccessoryGatewayStatus : MyRvLinkEventDevicesMultiByte<MyRvLinkHvacStatus>
	{
		private const int StatusSize = 1;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.AccessoryGatewayStatus;

		protected override int BytesPerDevice => 2;

		public MyRvLinkAccessoryGatewayStatus(byte deviceTableId, params (byte DeviceId, LogicalDeviceAccessoryGatewayStatus status)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceAccessoryGatewayStatus) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 1);
				num++;
			}
		}

		protected MyRvLinkAccessoryGatewayStatus(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkAccessoryGatewayStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkAccessoryGatewayStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceAccessoryGatewayStatus status)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceAccessoryGatewayStatus logicalDeviceAccessoryGatewayStatus = new LogicalDeviceAccessoryGatewayStatus();
				logicalDeviceAccessoryGatewayStatus.Update(new ArraySegment<byte>(_rawData, index + 1, 1), 1);
				yield return (b, logicalDeviceAccessoryGatewayStatus);
			}
		}

		public LogicalDeviceAccessoryGatewayStatus? GetAccessoryGatewayStatus(int deviceId)
		{
			LogicalDeviceAccessoryGatewayStatus logicalDeviceAccessoryGatewayStatus = new LogicalDeviceAccessoryGatewayStatus();
			if (!GetAccessoryGatewayStatus(deviceId, logicalDeviceAccessoryGatewayStatus))
			{
				return null;
			}
			return logicalDeviceAccessoryGatewayStatus;
		}

		public bool GetAccessoryGatewayStatus(int deviceId, LogicalDeviceAccessoryGatewayStatus status)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					status.Update(new ArraySegment<byte>(_rawData, i + 1, 1), 1);
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
				handler.AppendFormatted(item.status);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
