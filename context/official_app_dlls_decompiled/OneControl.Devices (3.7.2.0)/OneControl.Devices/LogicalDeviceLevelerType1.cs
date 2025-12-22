using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerType1 : LogicalDevice<LogicalDeviceLevelerStatusType1, LogicalDeviceLevelerStatusExtendedType1, ILogicalDeviceCapability>, ILogicalDeviceWithTextConsole, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceDirectLevelerType1, ILogicalDeviceLevelerType1, ILogicalDeviceLeveler, IDevicesActivation, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceWithStatus<LogicalDeviceLevelerStatusType1>, ILogicalDeviceWithStatus, ITextConsole, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink
	{
		private const string LogTag = "LogicalDeviceLevelerType1";

		private readonly object _locker = new object();

		private const int LevelerTextConsoleWidth = 16;

		private const int LevelerTextConsoleHeight = 2;

		protected ILogicalDeviceCommandRunnerIdsCan IdsCanCommandRunner;

		private CancellationTokenSource _commandCancelSource = new CancellationTokenSource();

		private string _textConsoleMessage;

		private int _textConsoleWidth;

		private int _textConsoleHeight;

		private ILogicalDevicePidFixedPoint _batteryVoltagePidCan;

		private LogicalDeviceLevelerCommandType1 _lastSentCommand;

		public virtual uint MSCommandTimeout => 5000u;

		public virtual uint MSSessionTimeout => 15000u;

		public override string DeviceName
		{
			get
			{
				if (base.LogicalId.FunctionInstance != 0)
				{
					return $"Leveler {base.LogicalId.FunctionInstance}";
				}
				return "Leveler";
			}
		}

		public LogicalDeviceLevelerStatusIndicatorStateType1 LedFrontState => DeviceStatus.IndicatorStateType1(LogicalDeviceLevelerStatusIndicatorType1.Front);

		public LogicalDeviceLevelerStatusIndicatorStateType1 LedRearState => DeviceStatus.IndicatorStateType1(LogicalDeviceLevelerStatusIndicatorType1.Rear);

		public LogicalDeviceLevelerStatusIndicatorStateType1 LedLeftState => DeviceStatus.IndicatorStateType1(LogicalDeviceLevelerStatusIndicatorType1.Left);

		public LogicalDeviceLevelerStatusIndicatorStateType1 LedRightState => DeviceStatus.IndicatorStateType1(LogicalDeviceLevelerStatusIndicatorType1.Right);

		public LogicalDeviceLevelerStatusIndicatorStateType1 LedLevelState => DeviceStatus.IndicatorStateType1(LogicalDeviceLevelerStatusIndicatorType1.Level);

		public LogicalDeviceLevelerStatusIndicatorStateType1 LedPowerState => DeviceStatus.IndicatorStateType1(LogicalDeviceLevelerStatusIndicatorType1.Power);

		public LogicalDeviceLevelerStatusIndicatorStateType1 LedRetractState => DeviceStatus.IndicatorStateType1(LogicalDeviceLevelerStatusIndicatorType1.Retract);

		public LogicalDeviceLevelerStatusIndicatorStateType1 BuzzerActiveState => DeviceStatus.IndicatorStateType1(LogicalDeviceLevelerStatusIndicatorType1.Buzzer);

		public bool IsPoweredActive => DeviceStatus.IsIndicatorActive(LogicalDeviceLevelerStatusIndicatorType1.Power);

		public IDevice Device => null;

		public bool IsDetected => true;

		public IReadOnlyList<string> Lines => LinesRaw;

		public List<string> LinesRaw { get; set; } = new List<string>();


		public TEXT_CONSOLE_SIZE Size { get; } = new TEXT_CONSOLE_SIZE(16, 2);


		public string TextConsoleMessage
		{
			get
			{
				return _textConsoleMessage;
			}
			private set
			{
				if (SetBackingField(ref _textConsoleMessage, value, "TextConsoleMessage"))
				{
					OnDeviceTextConsoleMessageChanged();
				}
			}
		}

		public int TextConsoleWidth
		{
			get
			{
				return _textConsoleWidth;
			}
			set
			{
				SetBackingField(ref _textConsoleWidth, value, "TextConsoleWidth");
			}
		}

		public int TextConsoleHeight
		{
			get
			{
				return _textConsoleHeight;
			}
			set
			{
				SetBackingField(ref _textConsoleHeight, value, "TextConsoleHeight");
			}
		}

		public virtual bool CommandSessionActivated => IdsCanCommandRunner.CommandSessionActivated;

		public LogicalDeviceExScope VoltageMeasurementBatteryPidScope => LogicalDeviceExScope.Product;

		public virtual ILogicalDevicePidFixedPoint VoltageMeasurementBatteryPid => _batteryVoltagePidCan ?? (_batteryVoltagePidCan = new LogicalDevicePidFixedPoint(FixedPointType.UnsignedBigEndian16x16, this, PID.BATTERY_VOLTAGE, LogicalDeviceSessionType.None));

		public bool IsVoltagePidReadSupported => true;

		public LogicalDeviceLevelerType1(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService deviceService = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceLevelerStatusType1(), new LogicalDeviceLevelerStatusExtendedType1(), (ILogicalDeviceCapability)new LogicalDeviceCapability(), deviceService, isFunctionClassChangeable)
		{
			IdsCanCommandRunner = new LogicalDeviceCommandRunnerIdsCan(this)
			{
				CommandProcessingTime = MSCommandTimeout,
				SessionKeepAliveTime = MSSessionTimeout
			};
			IdsCanCommandRunner.PropertyChanged += OnIdsCanCommandRunnerPropertyChanged;
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
			OnPropertyChanged("LedFrontState");
			OnPropertyChanged("LedRearState");
			OnPropertyChanged("LedLeftState");
			OnPropertyChanged("LedRightState");
			OnPropertyChanged("LedLevelState");
			OnPropertyChanged("LedPowerState");
			OnPropertyChanged("LedRetractState");
			OnPropertyChanged("BuzzerActiveState");
			OnPropertyChanged("IsPoweredActive");
		}

		public override void OnDeviceStatusExtendedChanged(IDeviceDataPacketMutableExtended dataChanged)
		{
			base.OnDeviceStatusExtendedChanged(dataChanged);
			TextConsoleMessage = base.DeviceStatusExtended.Text;
		}

		protected override void DebugUpdateDeviceStatusExtendedChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, uint dataLength, byte oldExtendedByte, byte extendedByte, string optionalText = "")
		{
		}

		public void UpdateDeviceConsoleText(List<string> text)
		{
			lock (_locker)
			{
				LinesRaw.Clear();
				LinesRaw = text;
			}
			UpdateTextConsole(this);
		}

		public void UpdateTextConsole(ITextConsole textConsole)
		{
			if (textConsole.Size.Width != TextConsoleWidth)
			{
				TaggedLog.Warning("LogicalDeviceLevelerType1", $"Ignoring TextConsole WIDTH of {textConsole.Size.Width} because Type-1 levelers support a fixed width of {TextConsoleWidth}.");
			}
			if (textConsole.Size.Height != TextConsoleHeight)
			{
				TaggedLog.Warning("LogicalDeviceLevelerType1", $"Ignoring TextConsole HEIGHT of {textConsole.Size.Height} because Type-1 levelers support a fixed height of {TextConsoleHeight}.");
			}
			TextConsoleWidth = textConsole.Size.Width;
			TextConsoleHeight = textConsole.Size.Height;
			TextConsoleMessage = textConsole.Text();
		}

		public void OnDeviceTextConsoleMessageChanged()
		{
		}

		public async Task ActivateSession(CancellationToken cancelToken)
		{
			_ = 1;
			try
			{
				await IdsCanCommandRunner.CommandActivateSession(cancelToken);
				await SendWakeupCommandAsync(cancelToken);
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceLevelerType1", $"Unable to ActiveSession for {base.LogicalId} with error {ex.Message}");
			}
		}

		public void DeactivateSession()
		{
			IdsCanCommandRunner.CommandDeactivateSession();
		}

		public Task<float> ReadVoltageMeasurementAsync(CancellationToken cancellationToken)
		{
			return LogicalDeviceVoltageExtension.ReadVoltageMeasurementAsync(VoltageMeasurementBatteryPid, cancellationToken);
		}

		protected void OnIdsCanCommandRunnerPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "CommandSessionActivated")
			{
				OnPropertyChanged("CommandSessionActivated");
			}
		}

		public async Task<CommandResult> SendWakeupCommandAsync(CancellationToken cancelToken)
		{
			if (cancelToken.IsCancellationRequested)
			{
				return CommandResult.Canceled;
			}
			CommandSendOption commandSendOption = CommandSendOption.CancelCurrentCommand;
			if (LogicalDeviceLevelerCommandType1.PowerOnCommand.Equals(_lastSentCommand))
			{
				commandSendOption |= CommandSendOption.AutoClearLockoutDisabled;
			}
			if (DeviceService.GetPrimaryDeviceSourceDirect(this) is IDirectCommandLeveler1 directCommandLeveler)
			{
				return await directCommandLeveler.SendDirectCommandLeveler1(this, LogicalDeviceLevelerCommandType1.PowerOnCommand, cancelToken);
			}
			CommandResult result = await IdsCanCommandRunner.SendCommandAsync(LogicalDeviceLevelerCommandType1.PowerOnCommand, cancelToken, (ILogicalDevice device) => (!IsPoweredActive) ? CommandControl.WaitAndResend : CommandControl.Completed, commandSendOption);
			_lastSentCommand = LogicalDeviceLevelerCommandType1.PowerOnCommand;
			return result;
		}

		public async Task<CommandResult> SendCommand(LogicalDeviceLevelerCommandType1 command, CancellationToken callerCancelToken)
		{
			if (!IdsCanCommandRunner.CommandSessionActivated)
			{
				return CommandResult.ErrorNoSession;
			}
			if (_commandCancelSource == null)
			{
				return CommandResult.Canceled;
			}
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceLevelerType1", "Unable to send command to disposed LogicalDevice " + DeviceName);
				return CommandResult.ErrorOther;
			}
			using CancellationTokenSource cancelSource = CancellationTokenSource.CreateLinkedTokenSource(callerCancelToken, _commandCancelSource.Token);
			if (cancelSource.IsCancellationRequested)
			{
				return CommandResult.Canceled;
			}
			if (!IsPoweredActive)
			{
				CommandResult commandResult = await SendWakeupCommandAsync(cancelSource.Token);
				if (commandResult != 0 || command.ButtonCommand == LogicalDeviceLevelerButtonType1.Power)
				{
					return commandResult;
				}
			}
			CommandSendOption commandSendOption = CommandSendOption.CancelCurrentCommand;
			if (LogicalDeviceLevelerCommandType1.PowerOnCommand.Equals(_lastSentCommand))
			{
				commandSendOption |= CommandSendOption.AutoClearLockoutDisabled;
			}
			if (DeviceService?.GetPrimaryDeviceSourceDirect(this) is IDirectCommandLeveler1 directCommandLeveler)
			{
				return await directCommandLeveler.SendDirectCommandLeveler1(this, LogicalDeviceLevelerCommandType1.PowerOnCommand, cancelSource.Token);
			}
			CommandResult result = await IdsCanCommandRunner.SendCommandAsync(command, cancelSource.Token, null, commandSendOption);
			_lastSentCommand = LogicalDeviceLevelerCommandType1.PowerOnCommand;
			return result;
		}

		public override void Dispose(bool disposing)
		{
			try
			{
				IdsCanCommandRunner.PropertyChanged -= OnIdsCanCommandRunnerPropertyChanged;
			}
			catch
			{
			}
			_commandCancelSource?.CancelAndDispose();
			_commandCancelSource = null;
			IdsCanCommandRunner?.TryDispose();
			base.Dispose(disposing);
		}

		NETWORK_STATUS ILogicalDeviceIdsCan.get_LastReceivedNetworkStatus()
		{
			return base.LastReceivedNetworkStatus;
		}
	}
}
