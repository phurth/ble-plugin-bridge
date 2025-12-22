using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common.Extensions;

namespace IDS.Portable.LogicalDevice
{
	public class PidMonitorPanelDeviceId
	{
		public MonitorPanelControlType ControlType;

		public MonitorPanelControlPosition ControlPosition;

		private const int ControlTypeShiftIndex = 5;

		private const int ControlPositionShiftIndex = 4;

		private const int ProductIdStartShiftIndex = 2;

		private const int DeviceInstanceShiftIndex = 1;

		private const int DeviceTypeShiftIndex = 0;

		public PRODUCT_ID ProductId { get; }

		public byte DeviceInstance { get; }

		public DEVICE_TYPE DeviceType { get; }

		public UInt48 RawValue
		{
			get
			{
				ulong data = 0uL;
				data.SetUInt16(2, ProductId);
				data.SetByte(1, DeviceInstance);
				data.SetByte(0, DeviceType);
				data.SetByte(5, (byte)ControlType);
				data.SetByte(4, (byte)ControlPosition);
				return (UInt48)data;
			}
		}

		public PidMonitorPanelDeviceId(PRODUCT_ID productId, DEVICE_TYPE deviceType, byte deviceInstance, MonitorPanelControlType controlType, MonitorPanelControlPosition position)
		{
			ProductId = productId;
			DeviceInstance = deviceInstance;
			DeviceType = deviceType;
			ControlType = controlType;
			ControlPosition = position;
		}

		public PidMonitorPanelDeviceId(LogicalDeviceId logicalDeviceId, MonitorPanelControlType controlType, MonitorPanelControlPosition position)
			: this(logicalDeviceId.ProductId, logicalDeviceId.DeviceType, (byte)logicalDeviceId.DeviceInstance, controlType, position)
		{
		}

		public PidMonitorPanelDeviceId(DEVICE_ID deviceId, MonitorPanelControlType controlType, MonitorPanelControlPosition position)
			: this(deviceId.ProductID, deviceId.DeviceType, (byte)deviceId.DeviceInstance, controlType, position)
		{
		}

		public PidMonitorPanelDeviceId(ulong rawValue)
		{
			ProductId = rawValue.GetUInt16(2);
			DeviceInstance = rawValue.GetByte(1);
			DeviceType = rawValue.GetByte(0);
			ControlType = (MonitorPanelControlType)rawValue.GetByte(5);
			ControlPosition = (MonitorPanelControlPosition)rawValue.GetByte(4);
		}

		public static PidMonitorPanelDeviceId ToPidDeviceIdMonitorPanel(LogicalDeviceId logicalDeviceId, MonitorPanelControlType controlType, MonitorPanelControlPosition position)
		{
			return new PidMonitorPanelDeviceId(logicalDeviceId.ProductId, logicalDeviceId.DeviceType, (byte)logicalDeviceId.DeviceInstance, controlType, position);
		}

		public static PidMonitorPanelDeviceId ToPidDeviceIdMonitorPanel(DEVICE_ID deviceId, MonitorPanelControlType controlType, MonitorPanelControlPosition position)
		{
			return new PidMonitorPanelDeviceId(deviceId.ProductID, deviceId.DeviceType, (byte)deviceId.DeviceInstance, controlType, position);
		}

		public override string ToString()
		{
			return $"{ProductId}: {DeviceType} #{DeviceInstance}  {ControlType}:{ControlPosition}";
		}
	}
}
