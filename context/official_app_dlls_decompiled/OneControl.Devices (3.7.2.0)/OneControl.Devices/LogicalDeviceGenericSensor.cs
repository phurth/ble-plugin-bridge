using System;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.GenericSensor;

namespace OneControl.Devices
{
	public class LogicalDeviceGenericSensor : LogicalDevice<LogicalDeviceGenericSensorStatus, ILogicalDeviceCapability>
	{
		public override bool IsLegacyDeviceHazardous => false;

		public LogicalDeviceGenericSensor(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService deviceService = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceGenericSensorStatus(), (ILogicalDeviceCapability)new LogicalDeviceCapability(), deviceService, isFunctionClassChangeable)
		{
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
		}

		public void UpdateAlert(byte[] alertData)
		{
			LogicalDeviceAlert newAlert = new LogicalDeviceAlert("Alert Data: " + BitConverter.ToString(alertData).Trim(), isActive: true, null);
			PerformLogicalDeviceExAction(delegate(ILogicalDeviceExAlertChanged logicalDeviceEx)
			{
				logicalDeviceEx.LogicalDeviceAlertChanged(this, null, newAlert);
			});
		}
	}
}
