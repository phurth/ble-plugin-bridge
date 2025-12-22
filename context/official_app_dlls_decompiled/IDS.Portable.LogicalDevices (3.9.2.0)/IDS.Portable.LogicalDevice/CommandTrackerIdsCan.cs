using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.LogicalDevice
{
	internal class CommandTrackerIdsCan
	{
		public readonly byte CommandByte;

		public readonly byte[] Data;

		public readonly CancellationToken CancelToken;

		public readonly Func<ILogicalDevice, CommandControl>? CmdControl;

		public readonly TaskCompletionSource<CommandResult> Result;

		public bool SendCommand = true;

		public readonly int ResponseTimeMs;

		private Stopwatch? _commandRunTimer;

		public bool CommandReplaced;

		public CommandTrackerIdsCan(byte commandByte, byte[] data, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl>? cmdControl, TaskCompletionSource<CommandResult> result, int responseTimeMs)
		{
			CommandByte = commandByte;
			Data = data;
			CancelToken = cancelToken;
			CmdControl = cmdControl;
			ResponseTimeMs = responseTimeMs;
			Result = result;
			_commandRunTimer = null;
		}

		public double GetCommandRunningTime()
		{
			if (_commandRunTimer == null)
			{
				_commandRunTimer = Stopwatch.StartNew();
			}
			return _commandRunTimer!.ElapsedMilliseconds;
		}
	}
}
