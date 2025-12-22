using System;
using System.Collections.Generic;
using System.Text;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceSessionStatus : MyRvLinkEventDevicesSubByte<MyRvLinkDeviceSessionStatus>
	{
		public override MyRvLinkEventType EventType => MyRvLinkEventType.DeviceSessionStatus;

		protected override MyRvLinkEventDevicesSubByte<MyRvLinkDeviceSessionStatus>.AllowedDevicesPerByte DevicesPerByte => MyRvLinkEventDevicesSubByte<MyRvLinkDeviceSessionStatus>.AllowedDevicesPerByte.Eight;

		protected override int MinPayloadLength => 3;

		public byte StartDeviceId { get; }

		protected override int DeviceTableIdIndex => 1;

		protected override int DeviceCountIndex => 2;

		protected override int DeviceStatusStartIndex => 3;

		public MyRvLinkDeviceSessionStatus(byte deviceTableId, byte deviceCount)
			: base(deviceTableId, (int)deviceCount)
		{
		}

		protected MyRvLinkDeviceSessionStatus(IReadOnlyList<byte> rawData)
			: base((IReadOnlyList<byte>)rawData.ToNewArray(0, rawData.Count))
		{
		}

		public static MyRvLinkDeviceSessionStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkDeviceSessionStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, bool isSessionOpen)> EnumerateIsDeviceSessionOpen()
		{
			int endDeviceId = StartDeviceId + base.DeviceCount;
			for (byte deviceId = StartDeviceId; deviceId < endDeviceId; deviceId = (byte)(deviceId + 1))
			{
				yield return (deviceId, IsDeviceSessionOpen(deviceId));
			}
		}

		public bool IsDeviceSessionOpen(byte deviceId)
		{
			return GetDeviceStatus(deviceId, StartDeviceId) != 0;
		}

		public void SetDeviceSessionOpen(byte deviceId, bool isSessionOpen)
		{
			SetDeviceStatus(deviceId, isSessionOpen ? ((byte)1) : ((byte)0), StartDeviceId);
		}

		public void SetAllDevicesSessionOpenTo(bool isSessionOpen)
		{
			int num = StartDeviceId + base.DeviceCount;
			for (byte b = StartDeviceId; b < num; b = (byte)(b + 1))
			{
				SetDeviceSessionOpen(b, isSessionOpen);
			}
		}

		protected override void DevicesToStringBuilder(StringBuilder stringBuilder)
		{
			foreach (var item in EnumerateIsDeviceSessionOpen())
			{
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(8, 3, stringBuilder);
				handler.AppendFormatted(Environment.NewLine);
				handler.AppendLiteral("    0x");
				handler.AppendFormatted(item.DeviceId, "X2");
				handler.AppendLiteral(": ");
				handler.AppendFormatted(item.isSessionOpen ? "Open" : "NotOpen");
				stringBuilder.Append(ref handler);
			}
		}
	}
}
