using System.ComponentModel;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public interface IGenerator : IHourMeter, IDevicesCommon, INotifyPropertyChanged
	{
		bool IsTemperatureSupported { get; }

		bool IsTemperatureSensorValid { get; }

		float TemperatureFahrenheit { get; }

		bool IsBatteryVoltageSupported { get; }

		float BatteryVoltage { get; }

		bool IsQuietHoursSupported { get; }

		bool QuietHoursActive { get; }

		ILogicalDevicePidTimeSpan GeneratorQuietHoursStartTimePid { get; }

		ILogicalDevicePidTimeSpan GeneratorQuietHoursEndTimePid { get; }

		bool IsAutoRunSupported { get; }

		bool IsAutoRunOnTempSupported { get; }

		bool IsAutoRunOnTempAvailable { get; }

		ILogicalDevicePidBool GeneratorQuietHoursEnable { get; }

		ILogicalDevicePidFixedPoint GeneratorAutoStartLowVoltagePid { get; }

		ILogicalDevicePidTimeSpan GeneratorAutoRunDurationMinutesPid { get; }

		ILogicalDevicePidTimeSpan GeneratorAutoRunMinOffTimeMinutesPid { get; }

		ILogicalDevicePidFixedPointBool GeneratorAutoStartHiTempCelicusPid { get; }

		bool IsHourMeterSupported { get; }

		bool HasHourMeter { get; }

		GeneratorState GeneratorCurrentState { get; }

		bool IsGeneratorOnePressCommandsSupported { get; }

		GeneratorType GeneratorType { get; }

		ILogicalDevicePidMap<string> CumminsOnanGeneratorFaultCode { get; }

		Task<CommandResult> SendGeneratorOnePressOnCommandAsync();

		Task<CommandResult> SendGeneratorOnePressOffCommandAsync();
	}
}
