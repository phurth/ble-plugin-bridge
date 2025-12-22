using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Interfaces;

namespace OneControl.Devices
{
	public interface ILightDimmable : IAutoOff
	{
		DimmableLightMode Mode { get; }

		byte Brightness { get; }

		byte MaxBrightness { get; }

		TimeSpan? MaxAutoOffDurationMinutes { get; }

		int CycleTime1 { get; }

		int CycleTime2 { get; }

		bool DimEnabled { get; }

		bool CycleTimeEnabled { get; }

		bool ConfiguredAsSwitchedLight { get; }

		Task SetSimulateOnOffStyleLightAsync(SimulatedOnOffStyleLightCapability onOffStyleLightCapability, CancellationToken cancellationToken);

		Task<CommandResult> SendCommandAsync(LogicalDeviceLightDimmableCommand command);

		void SendLightCommandRun(DimmableLightMode newMode, byte newOnMaxBrightness, byte newOnDuration, int newBlinkSwellCycleTime1, int newBlinkSwellCycleTime2);

		Task<CommandResult> SendCommandOffAsync(bool waitForCurrentStatus);

		Task<CommandResult> SendCommandOnAsync(byte newOnBrightness, byte newOnDuration, bool waitForCurrentStatus);

		Task<CommandResult> SendRestoreCommandAsync();

		Task<CommandResult> SendSettingsCommandAsync(byte newOnBrightness, byte newOnDuration);
	}
}
