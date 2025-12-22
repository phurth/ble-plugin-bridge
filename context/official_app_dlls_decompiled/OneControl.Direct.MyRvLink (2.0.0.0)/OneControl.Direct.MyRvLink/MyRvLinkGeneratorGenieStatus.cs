using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkGeneratorGenieStatus : MyRvLinkEventDevicesMultiByte<MyRvLinkGeneratorGenieStatus>
	{
		private const int GeneratorStatusSize = 5;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.GeneratorGenieStatus;

		protected override int BytesPerDevice => 6;

		public MyRvLinkGeneratorGenieStatus(byte deviceTableId, params (byte DeviceId, LogicalDeviceGeneratorGenieStatus GeneratorStatus)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceGeneratorGenieStatus) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 5);
				num += 5;
			}
		}

		protected MyRvLinkGeneratorGenieStatus(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkGeneratorGenieStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkGeneratorGenieStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceGeneratorGenieStatus GeneratorStatus)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceGeneratorGenieStatus logicalDeviceGeneratorGenieStatus = new LogicalDeviceGeneratorGenieStatus();
				logicalDeviceGeneratorGenieStatus.Update(new ArraySegment<byte>(_rawData, index + 1, 5), 5);
				yield return (b, logicalDeviceGeneratorGenieStatus);
			}
		}

		public LogicalDeviceGeneratorGenieStatus? GetGeneratorStatus(int deviceId)
		{
			LogicalDeviceGeneratorGenieStatus logicalDeviceGeneratorGenieStatus = new LogicalDeviceGeneratorGenieStatus();
			if (!GetGeneratorGenieStatus(deviceId, logicalDeviceGeneratorGenieStatus))
			{
				return null;
			}
			return logicalDeviceGeneratorGenieStatus;
		}

		public bool GetGeneratorGenieStatus(int deviceId, LogicalDeviceGeneratorGenieStatus generatorStatus)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					generatorStatus.Update(new ArraySegment<byte>(_rawData, i + 1, 5), 5);
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
				handler.AppendFormatted(item.GeneratorStatus);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
