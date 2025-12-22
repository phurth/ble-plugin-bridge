using System;
using System.Collections.Generic;
using System.Text;
using OneControl.Devices.Leveler.Type5;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkLevelerType5ExtendedStatus : MyRvLinkEventDevicesMultiByte<MyRvLinkLevelerType5ExtendedStatus>
	{
		private const int LevelerType5ExtendedStatusSize = 8;

		private const int EnhancedByteIndexOffsetFromDeviceId = 1;

		private new const int EventTypeIndex = 0;

		private const byte AutoOperationEnhancedByte = 0;

		private bool _isAutoOperationProgressStatus;

		public override MyRvLinkEventType EventType
		{
			get
			{
				if (_rawData.Length < 0 || _rawData[0] != 21)
				{
					return MyRvLinkEventType.LevelerType5ExtendedStatus;
				}
				return MyRvLinkEventType.AutoOperationProgressStatus;
			}
		}

		protected override int BytesPerDevice
		{
			get
			{
				if (EventType != MyRvLinkEventType.AutoOperationProgressStatus)
				{
					return 10;
				}
				return 9;
			}
		}

		public MyRvLinkLevelerType5ExtendedStatus(byte deviceTableId, params (byte DeviceId, ILogicalDeviceLevelerStatusExtendedType5 ExtendedStatus)[] deviceMessages)
			: base(deviceTableId, deviceMessages.Length)
		{
			int num = 2;
			for (int i = 0; i < deviceMessages.Length; i++)
			{
				(byte, ILogicalDeviceLevelerStatusExtendedType5) tuple = deviceMessages[i];
				_rawData[num++] = tuple.Item1;
				tuple.Item2.CopyData(_rawData, num, 8);
				num += 8;
			}
		}

		protected MyRvLinkLevelerType5ExtendedStatus(IReadOnlyList<byte> rawData)
			: base(rawData)
		{
		}

		public static MyRvLinkLevelerType5ExtendedStatus Decode(IReadOnlyList<byte> rawData)
		{
			return new MyRvLinkLevelerType5ExtendedStatus(rawData);
		}

		public IEnumerable<(byte DeviceId, ILogicalDeviceLevelerStatusExtendedType5 ExtendedStatus)> EnumerateStatus()
		{
			for (int index = 2; index < _rawData.Length; index += BytesPerDevice)
			{
				byte b = _rawData[index];
				ILogicalDeviceLevelerStatusExtendedType5 logicalDeviceLevelerStatusExtendedType = LogicalDeviceLevelerType5.MakeNewStatusExtendedFromMultiplexedDataImpl((LogicalDeviceLevelerStatusExtendedType5Kind)((EventType != MyRvLinkEventType.AutoOperationProgressStatus) ? _rawData[index + 1] : 0));
				int num = ((EventType == MyRvLinkEventType.AutoOperationProgressStatus) ? (index + 1) : (index + 1 + 1));
				logicalDeviceLevelerStatusExtendedType.Update(new ArraySegment<byte>(_rawData, num, 8), 8);
				yield return (b, logicalDeviceLevelerStatusExtendedType);
			}
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
				handler.AppendFormatted(item.ExtendedStatus);
				stringBuilder.Append(ref handler);
			}
		}
	}
}
