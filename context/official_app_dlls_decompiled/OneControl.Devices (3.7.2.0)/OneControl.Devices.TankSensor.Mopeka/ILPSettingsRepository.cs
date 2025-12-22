using System.Threading;
using System.Threading.Tasks;

namespace OneControl.Devices.TankSensor.Mopeka
{
	public interface ILPSettingsRepository
	{
		Task CreateSettings(ILogicalDeviceTankSensor device, LPTankName name, ILPTankSize size, bool isNotificationEnabled, int notificationThreshold, float accelXOffset, float accelYOffset, TankHeightUnits preferredUnits, CancellationToken token = default(CancellationToken));

		Task DeleteSettings(ILogicalDeviceTankSensor device, CancellationToken token = default(CancellationToken));

		Task<bool> HasLPSettings(ILogicalDeviceTankSensor device, CancellationToken token = default(CancellationToken));

		Task<LPTankName> GetTankName(ILogicalDeviceTankSensor device, CancellationToken token = default(CancellationToken));

		Task SetTankName(ILogicalDeviceTankSensor device, LPTankName name, CancellationToken token = default(CancellationToken));

		Task<ILPTankSize> GetTankSize(ILogicalDeviceTankSensor device, CancellationToken token = default(CancellationToken));

		Task SetTankSize(ILogicalDeviceTankSensor device, ILPTankSize size, CancellationToken token = default(CancellationToken));

		Task<bool> IsThresholdNotificationEnabled(ILogicalDeviceTankSensor device, CancellationToken token = default(CancellationToken));

		Task EnableThresholdNotification(ILogicalDeviceTankSensor device, CancellationToken token = default(CancellationToken));

		Task DisableThresholdNotification(ILogicalDeviceTankSensor device, CancellationToken token = default(CancellationToken));

		Task<int> GetNotificationThreshold(ILogicalDeviceTankSensor device, CancellationToken token = default(CancellationToken));

		Task SetNotificationThreshold(ILogicalDeviceTankSensor device, int tankLevelPercent, CancellationToken token = default(CancellationToken));

		Task<CalibrationOffsets> GetPositionCalibrationOffsets(ILogicalDeviceTankSensor device, CancellationToken token = default(CancellationToken));

		Task SetPositionCalibrationOffsets(ILogicalDeviceTankSensor device, float xOffset, float yOffset, CancellationToken token = default(CancellationToken));

		Task SetFaulted(LPFaultType faultType, ILogicalDeviceTankSensor device, bool isFaulted, CancellationToken token = default(CancellationToken));

		Task<bool> IsFaulted(LPFaultType faultType, ILogicalDeviceTankSensor device, CancellationToken token = default(CancellationToken));

		Task SetPreferredUnits(ILogicalDeviceTankSensor device, TankHeightUnits preferredUnits, CancellationToken token = default(CancellationToken));

		Task<TankHeightUnits> GetPreferredUnits(ILogicalDeviceTankSensor device, CancellationToken token = default(CancellationToken));
	}
}
