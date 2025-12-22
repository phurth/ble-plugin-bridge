using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class RelayBasicLatchingCommandRunnerType1Sim : LogicalDeviceCommandRunnerIdsCanSim<ILogicalDeviceRelayBasicStatus>
	{
		public RelayBasicLatchingCommandRunnerType1Sim(ILogicalDeviceRelayBasicStatus deviceStatus)
			: base(deviceStatus)
		{
		}

		public override Task<CommandResult> SendCommandAsync(byte commandByte, byte[] data, uint dataSize, int responseTimeMs, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl> cmdControl = null, CommandSendOption options = CommandSendOption.None)
		{
			if (dataSize != 1 || data.Length < 1)
			{
				return Task.FromResult(CommandResult.ErrorOther);
			}
			LogicalDeviceRelayBasicCommandType1 logicalDeviceRelayBasicCommandType = new LogicalDeviceRelayBasicCommandType1(data[0]);
			if (logicalDeviceRelayBasicCommandType.ClearingFault)
			{
				SimDeviceStatus.SetFault(isFaulted: false);
				SimDeviceStatus.SetState(isOn: false);
			}
			else
			{
				SimDeviceStatus.SetState(logicalDeviceRelayBasicCommandType.IsOn);
			}
			return Task.FromResult(CommandResult.Completed);
		}
	}
}
