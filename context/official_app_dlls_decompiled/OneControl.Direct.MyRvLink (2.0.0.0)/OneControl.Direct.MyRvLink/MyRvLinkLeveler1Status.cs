using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkLeveler1Status : MyRvLinkEventDevicesMultiByte<MyRvLinkLeveler1Status>
	{
		private const int Leveler1StatusSize = 3;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.Leveler1DeviceStatus;

		protected override int BytesPerDevice => 4;

		public MyRvLinkLeveler1Status(byte deviceTableId, params (byte DeviceId, LogicalDeviceLevelerStatusType1 Leveler1Status)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceLevelerStatusType1) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 3);
				num += 3;
			}
		}

		protected MyRvLinkLeveler1Status(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkLeveler1Status Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkLeveler1Status(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceLevelerStatusType1 Leveler1Status)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceLevelerStatusType1 logicalDeviceLevelerStatusType = new LogicalDeviceLevelerStatusType1();
				logicalDeviceLevelerStatusType.Update(new ArraySegment<byte>(_rawData, index + 1, 3), 3);
				yield return (b, logicalDeviceLevelerStatusType);
			}
		}

		public LogicalDeviceLevelerStatusType1? GetLeveler1Status(int deviceId)
		{
			LogicalDeviceLevelerStatusType1 logicalDeviceLevelerStatusType = new LogicalDeviceLevelerStatusType1();
			if (!GetLeveler1Status(deviceId, logicalDeviceLevelerStatusType))
			{
				return null;
			}
			return logicalDeviceLevelerStatusType;
		}

		public bool GetLeveler1Status(int deviceId, LogicalDeviceLevelerStatusType1 leveler1Status)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					leveler1Status.Update(new ArraySegment<byte>(_rawData, i + 1, 3), 3);
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
				handler.AppendFormatted(item.Leveler1Status);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
