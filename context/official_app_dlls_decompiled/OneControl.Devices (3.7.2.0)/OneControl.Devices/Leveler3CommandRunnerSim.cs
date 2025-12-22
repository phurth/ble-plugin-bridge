using System;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.Leveler.Type3.State;

namespace OneControl.Devices
{
	public class Leveler3CommandRunnerSim : LogicalDeviceCommandRunnerIdsCanSim<LogicalDeviceLevelerStatusType3>
	{
		private LogicalDeviceLevelerType3SimState _levelerState;

		public Leveler3CommandRunnerSim(LogicalDeviceLevelerType3SimState deviceState)
			: base(deviceState.Status)
		{
			_levelerState = deviceState;
		}

		public override Task<CommandResult> SendCommandAsync(byte commandByte, byte[] data, uint dataSize, int responseTimeMs, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl> cmdControl = null, CommandSendOption options = CommandSendOption.None)
		{
			if (dataSize != 3 || data.Length < 3)
			{
				return Task.FromResult(CommandResult.ErrorOther);
			}
			LogicalDeviceLevelerCommandType3 command = new LogicalDeviceLevelerCommandType3(commandByte, data, responseTimeMs);
			_levelerState.ProcessCommand(command);
			return Task.FromResult(CommandResult.Completed);
		}
	}
}
