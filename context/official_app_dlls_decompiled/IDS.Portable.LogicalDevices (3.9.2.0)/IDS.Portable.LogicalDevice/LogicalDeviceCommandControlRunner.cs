using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceCommandControlRunner : CommonDisposable, ILogicalDeviceCommandControlRunner, ICommonDisposable, IDisposable
	{
		public const string LogTag = "LogicalDeviceCommandControlRunner";

		public int CommandProcessingTime = 5000;

		public CancellationTokenSource? CurrentCommandCancelTokenSource;

		public int RetryInterval => 500;

		public async Task<CommandResult> SendCommandAsync(Func<CancellationToken, Task<CommandResult>> command, CancellationToken cancelToken, ILogicalDevice logicalDevice, Func<ILogicalDevice, CommandControl>? cmdControl = null, CommandSendOption options = CommandSendOption.None)
		{
			if (options == CommandSendOption.CancelCurrentCommand)
			{
				CurrentCommandCancelTokenSource?.TryCancelAndDispose();
			}
			CancellationTokenSource commandCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(new CancellationToken[1] { cancelToken });
			CancellationToken commandCancelToken = commandCancelTokenSource.Token;
			CurrentCommandCancelTokenSource = commandCancelTokenSource;
			try
			{
				Stopwatch startTimer = Stopwatch.StartNew();
				CommandResult result;
				CommandControl cmdControlResult;
				CommandControl commandControl;
				do
				{
					if (base.IsDisposed)
					{
						return CommandResult.ErrorOther;
					}
					if (commandCancelToken.IsCancellationRequested)
					{
						TaggedLog.Debug("LogicalDeviceCommandControlRunner", "SendCommandAsync Send Command Canceled because of Cancellation Token");
						return CommandResult.Canceled;
					}
					if (startTimer.ElapsedMilliseconds > CommandProcessingTime)
					{
						TaggedLog.Debug("LogicalDeviceCommandControlRunner", $"SendCommandAsync timeout {startTimer.ElapsedMilliseconds} ms");
						return CommandResult.ErrorCommandTimeout;
					}
					result = await command(commandCancelToken);
					if ((uint)(result - 1) <= 1u)
					{
						TaggedLog.Debug("LogicalDeviceCommandControlRunner", $"SendCommandAsync Sent Command {result}");
						return result;
					}
					cmdControlResult = cmdControl?.Invoke(logicalDevice) ?? CommandControl.Completed;
					TaggedLog.Debug("LogicalDeviceCommandControlRunner", $"SendCommandAsync Sent Command {result} {cmdControlResult} {startTimer.ElapsedMilliseconds} ms");
					CommandControl num;
					switch (cmdControlResult)
					{
					case CommandControl.Completed:
						return result;
					case CommandControl.WaitAndResend:
						await TaskExtension.TryDelay(RetryInterval, commandCancelToken);
						num = cmdControl?.Invoke(logicalDevice) ?? CommandControl.Completed;
						break;
					case CommandControl.WaitNoResend:
						await TaskExtension.TryDelay(RetryInterval, commandCancelToken);
						return result;
					default:
						return result;
					}
					commandControl = num;
				}
				while (commandControl == CommandControl.WaitAndResend);
				TaggedLog.Debug("LogicalDeviceCommandControlRunner", $"SendCommandAsync Sent Command {result} {cmdControlResult} {startTimer.ElapsedMilliseconds} ms - Resend Skipped because {commandControl}");
				return result;
			}
			finally
			{
				commandCancelTokenSource.TryCancelAndDispose();
			}
		}

		public override void Dispose(bool disposing)
		{
			CurrentCommandCancelTokenSource?.TryCancelAndDispose();
		}
	}
}
