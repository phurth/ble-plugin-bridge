using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices.Leveler.Type5;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkLeveler5Status : MyRvLinkEventDevicesMultiByte<MyRvLinkLeveler5Status>
	{
		private const int Leveler5StatusSize = 8;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.Leveler5DeviceStatus;

		protected override int BytesPerDevice => 9;

		public MyRvLinkLeveler5Status(byte deviceTableId, params (byte DeviceId, LogicalDeviceLevelerStatusType5 Leveler5Status)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceLevelerStatusType5) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 8);
				num += 8;
			}
		}

		protected MyRvLinkLeveler5Status(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkLeveler5Status Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkLeveler5Status(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceLevelerStatusType5 Leveler5Status)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceLevelerStatusType5 logicalDeviceLevelerStatusType = new LogicalDeviceLevelerStatusType5();
				logicalDeviceLevelerStatusType.Update(new ArraySegment<byte>(_rawData, index + 1, 8), 8);
				yield return (b, logicalDeviceLevelerStatusType);
			}
		}

		public LogicalDeviceLevelerStatusType5? GetLeveler5Status(int deviceId)
		{
			LogicalDeviceLevelerStatusType5 logicalDeviceLevelerStatusType = new LogicalDeviceLevelerStatusType5();
			if (!GetLeveler5Status(deviceId, logicalDeviceLevelerStatusType))
			{
				return null;
			}
			return logicalDeviceLevelerStatusType;
		}

		public bool GetLeveler5Status(int deviceId, LogicalDeviceLevelerStatusType5 leveler5Status)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					leveler5Status.Update(new ArraySegment<byte>(_rawData, i + 1, 8), 8);
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
				handler.AppendFormatted(item.Leveler5Status);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
