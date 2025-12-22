using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkRelayHBridgeMomentaryStatusType1 : MyRvLinkEventDevicesMultiByte<MyRvLinkRelayHBridgeMomentaryStatusType1>
	{
		private const int LatchingRelaySize = 1;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.RelayHBridgeMomentaryStatusType1;

		protected override int BytesPerDevice => 2;

		public MyRvLinkRelayHBridgeMomentaryStatusType1(byte deviceTableId, params (byte DeviceId, LogicalDeviceRelayHBridgeStatusType1 State)[] latchingRelays)
			: base(deviceTableId, latchingRelays.Length)
		{
			int num = 2;
			for (int i = 0; i < latchingRelays.Length; i++)
			{
				(byte, LogicalDeviceRelayHBridgeStatusType1) tuple = latchingRelays[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 1);
				num++;
			}
		}

		protected MyRvLinkRelayHBridgeMomentaryStatusType1(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkRelayHBridgeMomentaryStatusType1 Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkRelayHBridgeMomentaryStatusType1(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceRelayHBridgeStatusType1 State)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceRelayHBridgeStatusType1 logicalDeviceRelayHBridgeStatusType = new LogicalDeviceRelayHBridgeStatusType1();
				logicalDeviceRelayHBridgeStatusType.Update(new ArraySegment<byte>(_rawData, index + 1, 1), 1);
				yield return (b, logicalDeviceRelayHBridgeStatusType);
			}
		}

		public LogicalDeviceRelayHBridgeStatusType1? GetStatus(int deviceId)
		{
			LogicalDeviceRelayHBridgeStatusType1 logicalDeviceRelayHBridgeStatusType = new LogicalDeviceRelayHBridgeStatusType1();
			if (!GetStatus(deviceId, logicalDeviceRelayHBridgeStatusType))
			{
				return null;
			}
			return logicalDeviceRelayHBridgeStatusType;
		}

		public bool GetStatus(int deviceId, LogicalDeviceRelayHBridgeStatusType1 status)
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
				handler.AppendFormatted(item);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
