using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkRelayBasicLatchingStatusType1 : MyRvLinkEventDevicesMultiByte<MyRvLinkRelayBasicLatchingStatusType1>
	{
		private const int LatchingRelaySize = 1;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.RelayBasicLatchingStatusType1;

		protected override int BytesPerDevice => 2;

		public MyRvLinkRelayBasicLatchingStatusType1(byte deviceTableId, params (byte DeviceId, LogicalDeviceRelayBasicStatusType1 State)[] latchingRelays)
			: base(deviceTableId, latchingRelays.Length)
		{
			int num = 2;
			for (int i = 0; i < latchingRelays.Length; i++)
			{
				(byte, LogicalDeviceRelayBasicStatusType1) tuple = latchingRelays[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 1);
				num++;
			}
		}

		protected MyRvLinkRelayBasicLatchingStatusType1(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkRelayBasicLatchingStatusType1 Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkRelayBasicLatchingStatusType1(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceRelayBasicStatusType1 State)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceRelayBasicStatusType1 logicalDeviceRelayBasicStatusType = new LogicalDeviceRelayBasicStatusType1();
				logicalDeviceRelayBasicStatusType.Update(new ArraySegment<byte>(_rawData, index + 1, 1), 1);
				yield return (b, logicalDeviceRelayBasicStatusType);
			}
		}

		public LogicalDeviceRelayBasicStatusType1? GetStatus(int deviceId)
		{
			LogicalDeviceRelayBasicStatusType1 logicalDeviceRelayBasicStatusType = new LogicalDeviceRelayBasicStatusType1();
			if (!GetStatus(deviceId, logicalDeviceRelayBasicStatusType))
			{
				return null;
			}
			return logicalDeviceRelayBasicStatusType;
		}

		public bool GetStatus(int deviceId, LogicalDeviceRelayBasicStatusType1 status)
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
