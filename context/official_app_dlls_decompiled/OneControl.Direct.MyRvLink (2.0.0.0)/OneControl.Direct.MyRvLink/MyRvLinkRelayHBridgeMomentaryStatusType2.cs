using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkRelayHBridgeMomentaryStatusType2 : MyRvLinkEventDevicesMultiByte<MyRvLinkRelayHBridgeMomentaryStatusType2>
	{
		private const int LatchingRelaySize = 6;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.RelayHBridgeMomentaryStatusType2;

		protected override int BytesPerDevice => 7;

		public MyRvLinkRelayHBridgeMomentaryStatusType2(byte deviceTableId, params (byte DeviceId, LogicalDeviceRelayHBridgeStatusType2 State)[] latchingRelays)
			: base(deviceTableId, latchingRelays.Length)
		{
			int num = 2;
			for (int i = 0; i < latchingRelays.Length; i++)
			{
				(byte, LogicalDeviceRelayHBridgeStatusType2) tuple = latchingRelays[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 6);
				num += 6;
			}
		}

		protected MyRvLinkRelayHBridgeMomentaryStatusType2(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkRelayHBridgeMomentaryStatusType2 Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkRelayHBridgeMomentaryStatusType2(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceRelayHBridgeStatusType2 State)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceRelayHBridgeStatusType2 logicalDeviceRelayHBridgeStatusType = new LogicalDeviceRelayHBridgeStatusType2();
				logicalDeviceRelayHBridgeStatusType.Update(new ArraySegment<byte>(_rawData, index + 1, 6), 6);
				yield return (b, logicalDeviceRelayHBridgeStatusType);
			}
		}

		public LogicalDeviceRelayHBridgeStatusType2? GetStatus(int deviceId)
		{
			LogicalDeviceRelayHBridgeStatusType2 logicalDeviceRelayHBridgeStatusType = new LogicalDeviceRelayHBridgeStatusType2();
			if (!GetStatus(deviceId, logicalDeviceRelayHBridgeStatusType))
			{
				return null;
			}
			return logicalDeviceRelayHBridgeStatusType;
		}

		public bool GetStatus(int deviceId, LogicalDeviceRelayHBridgeStatusType2 status)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					status.Update(new ArraySegment<byte>(_rawData, i + 1, 6), 6);
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
