using System;
using System.Collections.Generic;
using System.Text;
using IDS.Portable.LogicalDevice;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkCloudGatewayStatus : MyRvLinkEventDevicesMultiByte<MyRvLinkCloudGatewayStatus>
	{
		private const int CloudGatewayStatusSize = 1;

		private const int SoftwareUpdateStateSize = 1;

		private const byte SoftwareUpdateStateMask = 3;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.CloudGatewayStatus;

		protected override int BytesPerDevice => 3;

		public MyRvLinkCloudGatewayStatus(byte deviceTableId, params (byte DeviceId, LogicalDeviceCloudGatewayStatus Status, SoftwareUpdateState UpdateState)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceCloudGatewayStatus, SoftwareUpdateState) tuple = deviceMessages[i];
				_rawData[num] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num + 1, 1);
				_rawData[num + 1] = (byte)tuple.Item3;
				num += BytesPerDevice;
			}
		}

		protected MyRvLinkCloudGatewayStatus(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkCloudGatewayStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkCloudGatewayStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceCloudGatewayStatus CloudGatewayStatus, SoftwareUpdateState UpdateState)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceCloudGatewayStatus logicalDeviceCloudGatewayStatus = new LogicalDeviceCloudGatewayStatus();
				logicalDeviceCloudGatewayStatus.Update(new ArraySegment<byte>(_rawData, index + 1, 1), 1);
				SoftwareUpdateState softwareUpdateState = (SoftwareUpdateState)(_rawData[index + 1] & 3);
				yield return (b, logicalDeviceCloudGatewayStatus, softwareUpdateState);
			}
		}

		public (LogicalDeviceCloudGatewayStatus CloudGatewayStatus, SoftwareUpdateState UpdateState)? GetStatus(byte deviceId)
		{
			foreach (var item in EnumerateStatus())
			{
				if (item.DeviceId == deviceId)
				{
					return (item.CloudGatewayStatus, item.UpdateState);
				}
			}
			return null;
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
				handler.AppendFormatted(item.CloudGatewayStatus);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
