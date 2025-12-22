using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDevicePowerMonitorSetec : LogicalDevice<LogicalDevicePowerMonitorSetecStatus, ILogicalDeviceCapability>, ILogicalDevicePowerMonitorSetec, IDevicesActivation, ILogicalDeviceWithStatus<LogicalDevicePowerMonitorSetecStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged
	{
		private const string LogTag = "LogicalDeviceGeneratorGenie";

		protected ILogicalDeviceCommandRunnerIdsCan IdsCanCommandRunner;

		private CancellationTokenSource _commandCancelSource = new CancellationTokenSource();

		public virtual uint MSCommandTimeout => 12000u;

		public virtual uint MSSessionTimeout => 15000u;

		public bool CommandSessionActivated => IdsCanCommandRunner.CommandSessionActivated;

		public bool IsRunningCommands => IdsCanCommandRunner.IsRunningCommands;

		public LogicalDevicePowerMonitorSetec(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService service, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDevicePowerMonitorSetecStatus(), (ILogicalDeviceCapability)new LogicalDeviceCapability(), service, isFunctionClassChangeable)
		{
			IdsCanCommandRunner = new LogicalDeviceCommandRunnerIdsCan(this)
			{
				CommandProcessingTime = MSCommandTimeout,
				SessionKeepAliveTime = MSSessionTimeout
			};
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
			TaggedLog.Information("LogicalDeviceGeneratorGenie", $"Data Changed {base.LogicalId}: Status = {DeviceStatus}");
		}

		public async Task ActivateSession(CancellationToken cancelToken)
		{
			try
			{
				await IdsCanCommandRunner.CommandActivateSession(cancelToken);
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceGeneratorGenie", $"Unable to ActiveSession for {base.LogicalId} with error ${ex.Message}\n{ex.StackTrace}");
			}
		}

		public void DeactivateSession()
		{
			IdsCanCommandRunner.CommandDeactivateSession();
		}

		public async Task<CommandResult> SendOnCommandAsync()
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceGeneratorGenie", DeviceName + " PowerMonitorSetec SendOnCommandAsync ignored as the LogicalDevice has been disposed");
				return CommandResult.ErrorOther;
			}
			LogicalDevicePowerMonitorSetecCommand dataPacket = new LogicalDevicePowerMonitorSetecCommand(LogicalDevicePowerMonitorCommandType.On);
			return await IdsCanCommandRunner.SendCommandAsync(dataPacket, _commandCancelSource.Token, delegate
			{
				switch (DeviceStatus.Mode)
				{
				case PowerMonitorSetecMode.TurningOn:
				case PowerMonitorSetecMode.On:
					return CommandControl.Completed;
				case PowerMonitorSetecMode.HardwareSwitchOn:
					return CommandControl.Cancel;
				default:
					return CommandControl.WaitAndResend;
				}
			}, CommandSendOption.CancelCurrentCommand);
		}

		public async Task<CommandResult> SendOffCommandAsync()
		{
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceGeneratorGenie", DeviceName + " PowerMonitorSetec SendOffCommandAsync ignored as the LogicalDevice has been disposed");
				return CommandResult.ErrorOther;
			}
			LogicalDevicePowerMonitorSetecCommand dataPacket = new LogicalDevicePowerMonitorSetecCommand(LogicalDevicePowerMonitorCommandType.On);
			return await IdsCanCommandRunner.SendCommandAsync(dataPacket, _commandCancelSource.Token, delegate
			{
				switch (DeviceStatus.Mode)
				{
				case PowerMonitorSetecMode.Off:
				case PowerMonitorSetecMode.TurningOff:
				case PowerMonitorSetecMode.HardwareSwitchOn:
					return CommandControl.Completed;
				default:
					return CommandControl.WaitAndResend;
				}
			}, CommandSendOption.CancelCurrentCommand);
		}

		public override void Dispose(bool disposing)
		{
			_commandCancelSource?.CancelAndDispose();
			_commandCancelSource = null;
			IdsCanCommandRunner?.TryDispose();
			base.Dispose(disposing);
		}
	}
}
