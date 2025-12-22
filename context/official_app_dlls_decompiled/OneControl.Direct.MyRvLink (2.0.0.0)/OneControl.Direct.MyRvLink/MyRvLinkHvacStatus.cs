using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkHvacStatus : MyRvLinkEventDevicesMultiByte<MyRvLinkHvacStatus>
	{
		private const int HvacStatusSize = 8;

		private const int HvacStatusSizeEx = 2;

		public override MyRvLinkEventType EventType => MyRvLinkEventType.HvacStatus;

		protected override int BytesPerDevice => 11;

		public MyRvLinkHvacStatus(byte deviceTableId, params (byte DeviceId, LogicalDeviceClimateZoneStatus hvacStatus, LogicalDeviceClimateZoneStatusEx? hvacStatusEx)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, LogicalDeviceClimateZoneStatus, LogicalDeviceClimateZoneStatusEx) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 8);
				num += 8;
				if (tuple.Item3 == null)
				{
					Array.Clear(_rawData, num, 2);
				}
				else
				{
					tuple.Item3.CopyData(_rawData, num, 2);
				}
				num += 2;
			}
		}

		protected MyRvLinkHvacStatus(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkHvacStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkHvacStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, LogicalDeviceClimateZoneStatus HvacStatus, LogicalDeviceClimateZoneStatusEx hvacStatusEx)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				LogicalDeviceClimateZoneStatus logicalDeviceClimateZoneStatus = new LogicalDeviceClimateZoneStatus();
				logicalDeviceClimateZoneStatus.Update(new ArraySegment<byte>(_rawData, index + 1, 8), 8);
				LogicalDeviceClimateZoneStatusEx logicalDeviceClimateZoneStatusEx = new LogicalDeviceClimateZoneStatusEx();
				logicalDeviceClimateZoneStatusEx.Update(new ArraySegment<byte>(_rawData, index + 1 + 8, 2), 2);
				yield return (b, logicalDeviceClimateZoneStatus, logicalDeviceClimateZoneStatusEx);
			}
		}

		public LogicalDeviceClimateZoneStatus? GetClimateZoneStatus(int deviceId)
		{
			LogicalDeviceClimateZoneStatus logicalDeviceClimateZoneStatus = new LogicalDeviceClimateZoneStatus();
			if (!GetClimateZoneStatus(deviceId, logicalDeviceClimateZoneStatus))
			{
				return null;
			}
			return logicalDeviceClimateZoneStatus;
		}

		public bool GetClimateZoneStatus(int deviceId, LogicalDeviceClimateZoneStatus climateZoneStatus)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					climateZoneStatus.Update(new ArraySegment<byte>(_rawData, i + 1, 8), 8);
					return true;
				}
			}
			return false;
		}

		public LogicalDeviceClimateZoneStatusEx? GetClimateZoneStatusEx(int deviceId)
		{
			LogicalDeviceClimateZoneStatusEx logicalDeviceClimateZoneStatusEx = new LogicalDeviceClimateZoneStatusEx();
			if (!GetClimateZoneStatusEx(deviceId, logicalDeviceClimateZoneStatusEx))
			{
				return null;
			}
			return logicalDeviceClimateZoneStatusEx;
		}

		public bool GetClimateZoneStatusEx(int deviceId, LogicalDeviceClimateZoneStatusEx climateZoneStatusEx)
		{
			for (int i = 2; i < _rawData.Length; i += BytesPerDevice)
			{
				if (_rawData[i] == deviceId)
				{
					climateZoneStatusEx.Update(new ArraySegment<byte>(_rawData, i + 1 + 8, 2), 2);
					return true;
				}
			}
			return false;
		}

		protected override void DevicesToStringBuilder(StringBuilder stringBuilder)
		{
			foreach (var item in EnumerateStatus())
			{
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(9, 4, stringBuilder);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    0x");
				handler.AppendFormatted(item.DeviceId, "X2");
				handler.AppendLiteral(": ");
				handler.AppendFormatted(item.HvacStatus);
				handler.AppendLiteral(" ");
				handler.AppendFormatted(item.hvacStatusEx);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
