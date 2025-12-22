using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLightMasterSwitchControllableIdsCanEx : LogicalDeviceExBase<ILogicalDeviceSwitchableLight>
	{
		public const int MaxConcurrentLightOperations = 3;

		private int _debugBackgroundOperationInstanceCount;

		private int _performingOperation;

		protected override string LogTag => "LogicalDeviceLightMasterSwitchControllableIdsCanEx";

		public static LogicalDeviceLightMasterSwitchControllableIdsCanEx SharedExtension => LogicalDeviceExBase<ILogicalDeviceSwitchableLight>.GetSharedExtension<LogicalDeviceLightMasterSwitchControllableIdsCanEx>(autoCreate: true);

		public static ILogicalDeviceEx LogicalDeviceExFactory(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExBase<ILogicalDeviceSwitchableLight>.LogicalDeviceExFactory<LogicalDeviceLightMasterSwitchControllableIdsCanEx>(logicalDevice, GetLogicalDeviceScope);
		}

		protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDeviceSwitchableLight logicalDevice)
		{
			return LogicalDeviceExScope.Shared;
		}

		public async Task<bool> TryChangeStateAsync(ILogicalDeviceService deviceService, LightMasterSwitchControllableState state, Func<ILogicalDeviceSwitchableLight, bool> lightFilter, CancellationToken cancellationToken)
		{
			if (Interlocked.Exchange(ref _performingOperation, 1) != 0)
			{
				TaggedLog.Debug(LogTag, $"Unable to perform {state} operation as an operation is already in progress.");
				return false;
			}
			ConcurrentQueue<ILogicalDeviceSwitchableLight> lightQueue = new ConcurrentQueue<ILogicalDeviceSwitchableLight>();
			try
			{
				if (!Enumerable.Any(GetAttachedLogicalDevices()))
				{
					return false;
				}
				foreach (ILogicalDeviceSwitchableLight attachedLogicalDevice in GetAttachedLogicalDevices())
				{
					if (!attachedLogicalDevice.IsMasterSwitchControllable || (attachedLogicalDevice.ActiveConnection != LogicalDeviceActiveConnection.Direct && attachedLogicalDevice.ActiveConnection != LogicalDeviceActiveConnection.Cloud))
					{
						continue;
					}
					ILogicalDeviceSourceDirect logicalDeviceSourceDirect = deviceService?.GetPrimaryDeviceSourceDirect(attachedLogicalDevice);
					if (logicalDeviceSourceDirect == null || logicalDeviceSourceDirect is ILogicalDeviceSourceDirectSwitchMasterControllable)
					{
						continue;
					}
					LightMasterSwitchControllableState lightMasterSwitchControllableState = state;
					if (lightMasterSwitchControllableState != 0)
					{
						if ((uint)(lightMasterSwitchControllableState - 1) > 1u || attachedLogicalDevice.On)
						{
							continue;
						}
					}
					else if (!attachedLogicalDevice.On)
					{
						continue;
					}
					if (lightFilter?.Invoke(attachedLogicalDevice) ?? true)
					{
						lightQueue.Enqueue(attachedLogicalDevice);
					}
				}
				if (lightQueue.Count <= 0)
				{
					return false;
				}
				_debugBackgroundOperationInstanceCount = 0;
				int numParallelOperations = Math.Min(3, lightQueue.Count);
				await TryPerformParallelOperationAsync(numParallelOperations, async delegate
				{
					int debugId = Interlocked.Increment(ref _debugBackgroundOperationInstanceCount);
					TaggedLog.Debug(LogTag, $"[{debugId}] {state} Started");
					try
					{
						while (!cancellationToken.IsCancellationRequested)
						{
							if (!lightQueue.TryDequeue(out var switchedLight))
							{
								return;
							}
							TaggedLog.Debug(LogTag, $"[{debugId}] Turning {state} {switchedLight.DeviceName} Start");
							switch (state)
							{
							case LightMasterSwitchControllableState.Off:
								await switchedLight.TurnOffAsync();
								break;
							case LightMasterSwitchControllableState.On:
								await switchedLight.TurnOnAsync(restore: false);
								break;
							case LightMasterSwitchControllableState.Restore:
								await switchedLight.TurnOnAsync(restore: true);
								break;
							}
							TaggedLog.Debug(LogTag, $"[{debugId}] Turning {state} {switchedLight.DeviceName} Finished");
							switchedLight = null;
						}
					}
					catch (Exception ex)
					{
						TaggedLog.Error(LogTag, $"{state} failure {ex.Message}");
					}
					TaggedLog.Debug(LogTag, $"[{debugId}] {state} Finished");
				});
			}
			finally
			{
				_performingOperation = 0;
			}
			return true;
		}

		private async Task TryPerformParallelOperationAsync(int numParallelOperations, Func<Task> operation)
		{
			if (numParallelOperations > 0)
			{
				Task[] array = new Task[numParallelOperations];
				for (int i = 0; i < numParallelOperations; i++)
				{
					array[i] = Task.Run(operation);
				}
				await Task.WhenAll(array);
			}
		}
	}
}
