namespace OneControl.Devices.TankSensor.Mopeka
{
	public class NotificationSettingsUpdatedEventArgs
	{
		public static string MessageId { get; } = "NotificationSettingsUpdatedEventArgs";


		public ILogicalDeviceTankSensor Device { get; }

		public bool IsThresholdNotificationEnabled { get; }

		public int NotificationThresholdAsPercent { get; }

		public NotificationSettingsUpdatedEventArgs(ILogicalDeviceTankSensor device, bool isThresholdNotificationEnabled, int notificationThresholdAsPercent)
		{
			Device = device;
			IsThresholdNotificationEnabled = isThresholdNotificationEnabled;
			NotificationThresholdAsPercent = notificationThresholdAsPercent;
		}
	}
}
