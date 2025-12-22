using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkLeveler3Status : MyRvLinkEventDevicesMultiByte<MyRvLinkLeveler3Status>
	{
		private const int Leveler3StatusSize = 6;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.Leveler3DeviceStatus;

		protected override int BytesPerDevice => 7;

		public MyRvLinkLeveler3Status(byte deviceTableId, params (byte DeviceId, LogicalDeviceLevelerStatusType3 Leveler3Status)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceLevelerStatusType3) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 6);
				num += 6;
			}
		}

		protected MyRvLinkLeveler3Status(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkLeveler3Status Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkLeveler3Status(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceLevelerStatusType3 Leveler3Status)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceLevelerStatusType3 logicalDeviceLevelerStatusType = new LogicalDeviceLevelerStatusType3();
				logicalDeviceLevelerStatusType.Update(new ArraySegment<byte>(_rawData, index + 1, 6), 6);
				yield return (b, logicalDeviceLevelerStatusType);
			}
		}

		public LogicalDeviceLevelerStatusType3? GetLeveler3Status(int deviceId)
		{
			LogicalDeviceLevelerStatusType3 logicalDeviceLevelerStatusType = new LogicalDeviceLevelerStatusType3();
			if (!GetLeveler3Status(deviceId, logicalDeviceLevelerStatusType))
			{
				return null;
			}
			return logicalDeviceLevelerStatusType;
		}

		public bool GetLeveler3Status(int deviceId, LogicalDeviceLevelerStatusType3 leveler3Status)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					leveler3Status.Update(new ArraySegment<byte>(_rawData, i + 1, 6), 6);
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
				handler.AppendFormatted(item.Leveler3Status);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
