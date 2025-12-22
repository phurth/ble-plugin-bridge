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
	public class LogicalDeviceLevelerType3 : LogicalDevice<LogicalDeviceLevelerStatusType3, ILogicalDeviceCapability>, ILogicalDeviceWithTextConsole, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceDirectLevelerType3, ILogicalDeviceLevelerType3, ILogicalDeviceLeveler, IDevicesActivation, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceWithStatus<LogicalDeviceLevelerStatusType3>, ILogicalDeviceWithStatus, ITextConsole, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink
	{
		private const string LogTag = "LogicalDeviceLevelerType3";

		private readonly object _locker = new object();

		private const int LevelerTextConsoleWidth = 40;

		private const int LevelerTextConsoleHeight = 6;

		protected ILogicalDeviceCommandRunnerIdsCan IdsCanCommandRunner;

		private CancellationTokenSource _commandCancelSource = new CancellationTokenSource();

		private string _textConsoleMessage;

		private int _textConsoleWidth;

		private int _textConsoleHeight;

		private ILogicalDevicePidFixedPoint _batteryVoltagePidCan;

		private LogicalDeviceLevelerCommandType3 _lastSentCommand;

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

		public LogicalDeviceLevelerStatusIndicatorStateType3 LedFrontState => DeviceStatus.IndicatorStateType3(LogicalDeviceLevelerStatusIndicatorType3.Front);

		public LogicalDeviceLevelerStatusIndicatorStateType3 LedRearState => DeviceStatus.IndicatorStateType3(LogicalDeviceLevelerStatusIndicatorType3.Rear);

		public LogicalDeviceLevelerStatusIndicatorStateType3 LedLeftState => DeviceStatus.IndicatorStateType3(LogicalDeviceLevelerStatusIndicatorType3.Left);

		public LogicalDeviceLevelerStatusIndicatorStateType3 LedRightState => DeviceStatus.IndicatorStateType3(LogicalDeviceLevelerStatusIndicatorType3.Right);

		public LogicalDeviceLevelerStatusIndicatorStateType3 LedLevelState => DeviceStatus.IndicatorStateType3(LogicalDeviceLevelerStatusIndicatorType3.Level);

		public LogicalDeviceLevelerStatusIndicatorStateType3 LedExtendState => DeviceStatus.IndicatorStateType3(LogicalDeviceLevelerStatusIndicatorType3.Extend);

		public LogicalDeviceLevelerStatusIndicatorStateType3 LedRetractState => DeviceStatus.IndicatorStateType3(LogicalDeviceLevelerStatusIndicatorType3.Retract);

		public LogicalDeviceLevelerStatusIndicatorStateType3 BuzzerActiveState => DeviceStatus.IndicatorStateType3(LogicalDeviceLevelerStatusIndicatorType3.Buzzer);

		public LogicalDeviceLevelerScreenType3 CurrentScreenShowing => DeviceStatus.CurrentScreenShowing;

		public LogicalDeviceLevelerButtonType3 ButtonsDisabled => DeviceStatus.ButtonsDisabled;

		public IDevice Device => null;

		public bool IsDetected => true;

		public IReadOnlyList<string> Lines => LinesRaw;

		public List<string> LinesRaw { get; set; } = new List<string>();


		public TEXT_CONSOLE_SIZE Size { get; } = new TEXT_CONSOLE_SIZE(40, 6);


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

		public LogicalDeviceLevelerType3(ILogicalDeviceId logicalDeviceId, ILogicalDeviceService deviceService = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceLevelerStatusType3(), (ILogicalDeviceCapability)new LogicalDeviceCapability(), deviceService, isFunctionClassChangeable)
		{
			IdsCanCommandRunner = new LogicalDeviceCommandRunnerIdsCan(this)
			{
				CommandProcessingTime = MSCommandTimeout,
				SessionKeepAliveTime = MSSessionTimeout
			};
			IdsCanCommandRunner.PropertyChanged += OnIdsCanCommandRunnerPropertyChanged;
		}

		public override void OnDeviceOnlineChanged()
		{
			base.OnDeviceOnlineChanged();
			switch (ActiveConnection)
			{
			case LogicalDeviceActiveConnection.Direct:
			{
				ITextConsole textConsole = (DeviceService.GetPrimaryDeviceSourceDirect(this) as ILogicalDeviceSourceDirectConnectionIdsCan)?.FindRemoteDevice(this)?.TextConsole;
				if (textConsole == null)
				{
					TextConsoleMessage = "";
				}
				else
				{
					UpdateTextConsole(textConsole);
				}
				break;
			}
			case LogicalDeviceActiveConnection.Offline:
			case LogicalDeviceActiveConnection.Remote:
			case LogicalDeviceActiveConnection.Cloud:
				TextConsoleMessage = "";
				break;
			}
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
			OnPropertyChanged("LedFrontState");
			OnPropertyChanged("LedRearState");
			OnPropertyChanged("LedLeftState");
			OnPropertyChanged("LedRightState");
			OnPropertyChanged("LedLevelState");
			OnPropertyChanged("LedExtendState");
			OnPropertyChanged("LedRetractState");
			OnPropertyChanged("BuzzerActiveState");
			OnPropertyChanged("CurrentScreenShowing");
			OnPropertyChanged("ButtonsDisabled");
		}

		public void UpdateTextConsole(ITextConsole textConsole)
		{
			TextConsoleWidth = textConsole.Size.Width;
			TextConsoleHeight = textConsole.Size.Height;
			TextConsoleMessage = textConsole.Text();
		}

		public void OnDeviceTextConsoleMessageChanged()
		{
		}

		public async Task ActivateSession(CancellationToken cancelToken)
		{
			try
			{
				await IdsCanCommandRunner.CommandActivateSession(cancelToken);
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceLevelerType3", $"Unable to ActiveSession for {base.LogicalId} with error {ex.Message}");
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
			return await SendCommand(new LogicalDeviceLevelerCommandType3(DeviceStatus.CurrentScreenShowing, LogicalDeviceLevelerButtonType3.None), cancelToken);
		}

		public async Task<CommandResult> SendCommand(LogicalDeviceLevelerCommandType3 command, CancellationToken callerCancelToken)
		{
			if (!IdsCanCommandRunner.CommandSessionActivated)
			{
				return CommandResult.ErrorNoSession;
			}
			if (_commandCancelSource == null)
			{
				return CommandResult.Canceled;
			}
			if (command.ScreenSelected != DeviceStatus.CurrentScreenShowing)
			{
				return CommandResult.ErrorCommandNotAllowed;
			}
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceLevelerType3", "Unable to send command to disposed LogicalDevice " + DeviceName);
				return CommandResult.ErrorOther;
			}
			using CancellationTokenSource cancelSource = CancellationTokenSource.CreateLinkedTokenSource(callerCancelToken, _commandCancelSource.Token);
			if (cancelSource.IsCancellationRequested)
			{
				return CommandResult.Canceled;
			}
			CommandSendOption commandSendOption = CommandSendOption.CancelCurrentCommand;
			if (command.Equals(_lastSentCommand))
			{
				commandSendOption |= CommandSendOption.AutoClearLockoutDisabled;
			}
			if (DeviceService?.GetPrimaryDeviceSourceDirect(this) is IDirectCommandLeveler3 directCommandLeveler)
			{
				return await directCommandLeveler.SendDirectCommandLeveler3(this, command, cancelSource.Token);
			}
			CommandResult commandResult = await IdsCanCommandRunner.SendCommandAsync(command, cancelSource.Token, delegate
			{
				switch (command.ButtonsPressed)
				{
				case LogicalDeviceLevelerButtonType3.None:
					if (DeviceStatus.ButtonsDisabled == LogicalDeviceLevelerButtonType3.None)
					{
						return CommandControl.WaitAndResend;
					}
					return CommandControl.Completed;
				default:
					return CommandControl.Completed;
				}
			}, commandSendOption);
			_lastSentCommand = command;
			if (commandResult != 0)
			{
				TaggedLog.Debug("LogicalDeviceLevelerType3", $"Unable to successfully send leveler command because {commandResult}");
			}
			return commandResult;
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
