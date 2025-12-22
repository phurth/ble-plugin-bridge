using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class RelayBasicLatchingCommandRunnerType2Sim : LogicalDeviceCommandRunnerIdsCanSim<ILogicalDeviceRelayBasicStatus>
	{
		public RelayBasicLatchingCommandRunnerType2Sim(ILogicalDeviceRelayBasicStatus deviceStatus)
			: base(deviceStatus)
		{
		}

		public override Task<CommandResult> SendCommandAsync(byte commandByte, byte[] data, uint dataSize, int responseTimeMs, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl> cmdControl = null, CommandSendOption options = CommandSendOption.None)
		{
			if (dataSize != 0)
			{
				return Task.FromResult(CommandResult.ErrorOther);
			}
			LogicalDeviceRelayBasicLatchingCommandType2 logicalDeviceRelayBasicLatchingCommandType = new LogicalDeviceRelayBasicLatchingCommandType2(commandByte);
			if (logicalDeviceRelayBasicLatchingCommandType.ClearingFault)
			{
				SimDeviceStatus.SetFault(isFaulted: false);
				SimDeviceStatus.SetUserClearRequired(disabled: false);
				SimDeviceStatus.SetState(isOn: false);
			}
			else
			{
				SimDeviceStatus.SetState(logicalDeviceRelayBasicLatchingCommandType.IsOn);
			}
			return Task.FromResult(CommandResult.Completed);
		}
	}
}
