using System;
using System.Collections.Generic;
using System.Text;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceOnlineStatus : MyRvLinkEventDevicesSubByte<MyRvLinkDeviceOnlineStatus>
	{
		public override MyRvLinkEventType EventType => MyRvLinkEventType.DeviceOnlineStatus;

		protected override MyRvLinkEventDevicesSubByte<MyRvLinkDeviceOnlineStatus>.AllowedDevicesPerByte DevicesPerByte => MyRvLinkEventDevicesSubByte<MyRvLinkDeviceOnlineStatus>.AllowedDevicesPerByte.Eight;

		protected override int MinPayloadLength => 3;

		public byte StartDeviceId { get; }

		protected override int DeviceTableIdIndex => 1;

		protected override int DeviceCountIndex => 2;

		protected override int DeviceStatusStartIndex => 3;

		public MyRvLinkDeviceOnlineStatus(byte deviceTableId, byte deviceCount)
			: base(deviceTableId, (int)deviceCount)
		{
		}

		protected MyRvLinkDeviceOnlineStatus(IReadOnlyList<byte> rawData)
			: base((IReadOnlyList<byte>)rawData.ToNewArray(0, rawData.Count))
		{
		}

		public static MyRvLinkDeviceOnlineStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkDeviceOnlineStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, bool isOnline)> EnumerateIsDeviceOnline()
		{
			int endDeviceId = StartDeviceId + base.DeviceCount;
			for (byte deviceId = StartDeviceId; deviceId < endDeviceId; deviceId = (byte)(deviceId + 1))
			{
				yield return (deviceId, IsDeviceOnline(deviceId));
			}
		}

		public bool IsDeviceOnline(byte deviceId)
		{
			return GetDeviceStatus(deviceId, StartDeviceId) != 0;
		}

		public void SetDeviceOnline(byte deviceId, bool isOnline)
		{
			SetDeviceStatus(deviceId, isOnline ? ((byte)1) : ((byte)0), StartDeviceId);
		}

		public void SetAllDevicesOnline(bool isOnline)
		{
			int num = StartDeviceId + base.DeviceCount;
			for (byte b = StartDeviceId; b < num; b = (byte)(b + 1))
			{
				SetDeviceOnline(b, isOnline);
			}
		}

		protected override void DevicesToStringBuilder(StringBuilder stringBuilder)
		{
			foreach (var item in EnumerateIsDeviceOnline())
			{
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 3, stringBuilder);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    0x");
				handler.AppendFormatted(item.DeviceId, "X2");
				handler.AppendLiteral(": ");
				handler.AppendFormatted(item.isOnline ? "Online" : "Offline");
				stringBuilder.Append(ref handler);
			}
		}
	}
}
