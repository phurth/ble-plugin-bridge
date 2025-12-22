using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkLeveler4Status : MyRvLinkEventDevicesMultiByte<MyRvLinkLeveler4Status>
	{
		private const int Leveler4StatusSize = 8;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.Leveler4DeviceStatus;

		protected override int BytesPerDevice => 9;

		public MyRvLinkLeveler4Status(byte deviceTableId, params (byte DeviceId, LogicalDeviceLevelerStatusType4 Leveler4Status)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceLevelerStatusType4) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 8);
				num += 8;
			}
		}

		protected MyRvLinkLeveler4Status(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkLeveler4Status Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkLeveler4Status(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceLevelerStatusType4 Leveler4Status)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceLevelerStatusType4 logicalDeviceLevelerStatusType = new LogicalDeviceLevelerStatusType4();
				logicalDeviceLevelerStatusType.Update(new ArraySegment<byte>(_rawData, index + 1, 8), 8);
				yield return (b, logicalDeviceLevelerStatusType);
			}
		}

		public LogicalDeviceLevelerStatusType4? GetLeveler4Status(int deviceId)
		{
			LogicalDeviceLevelerStatusType4 logicalDeviceLevelerStatusType = new LogicalDeviceLevelerStatusType4();
			if (!GetLeveler4Status(deviceId, logicalDeviceLevelerStatusType))
			{
				return null;
			}
			return logicalDeviceLevelerStatusType;
		}

		public bool GetLeveler4Status(int deviceId, LogicalDeviceLevelerStatusType4 leveler4Status)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					leveler4Status.Update(new ArraySegment<byte>(_rawData, i + 1, 8), 8);
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
				handler.AppendFormatted(item.Leveler4Status);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
