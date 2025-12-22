using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class LogicalDeviceLevelerType4 : LogicalDevice<LogicalDeviceLevelerStatusType4, LogicalDeviceLevelerCapabilityType4>, ILogicalDeviceWithTextConsole, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceDirectLevelerType4, ILogicalDeviceLevelerType4, ILogicalDeviceLeveler, IDevicesActivation, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceWithStatus<LogicalDeviceLevelerStatusType4>, ILogicalDeviceWithStatus, ILogicalDeviceWithCapability<LogicalDeviceLevelerCapabilityType4>, ITextConsole, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink
	{
		[Flags]
		public enum FeatureSupport
		{
			AutoHitch = 1,
			RetractFrontJacks = 2,
			RetractRearJacks = 4,
			ManualAirSuspensionControl = 8,
			RfRemotePairing = 0x10,
			ReHomeJackPositions = 0x20,
			All = 0x3F,
			Unknown = 0x800000
		}

		private const string LogTag = "LogicalDeviceLevelerType4";

		private readonly object _locker = new object();

		private const int LevelerTextConsoleWidth = 40;

		private const int LevelerTextConsoleHeight = 6;

		protected ILogicalDeviceCommandRunnerIdsCan IdsCanCommandRunner;

		private CancellationTokenSource _commandCancelSource = new CancellationTokenSource();

		private const uint DelayStatusChangedLogMessageThrottleTimeMs = 60000u;

		private readonly Stopwatch _delayStatusChangedLogMessageThrottleTimer = Stopwatch.StartNew();

		private readonly Stopwatch _delayStatusChange = new Stopwatch();

		private const uint DeviceStatusChangedThrottleTimeMs = 250u;

		private string _textConsoleMessage;

		private int _textConsoleWidth;

		private int _textConsoleHeight;

		private ITextConsole _cachedTextConsole;

		private ILogicalDevicePidFixedPoint _batteryVoltagePidCan;

		private ILogicalDeviceLevelerCommandType4 _lastSentCommand;

		private readonly ILogicalDevicePidULong _uiSupportedFeaturesPid;

		private LogicalDevicePidBindableAsyncValue? _uiSupportedFeaturePidValue;

		private FeatureSupport _featuresSupported = FeatureSupport.Unknown;

		private static List<PID> _autoStepsDetailPidList = new List<PID>
		{
			PID.LEVELER_AUTO_PROCESS_STEPS_1,
			PID.LEVELER_AUTO_PROCESS_STEPS_2,
			PID.LEVELER_AUTO_PROCESS_STEPS_3,
			PID.LEVELER_AUTO_PROCESS_STEPS_4,
			PID.LEVELER_AUTO_PROCESS_STEPS_5
		};

		private const int AutoStepsPerPid = 6;

		public virtual uint MsCommandTimeout => 5000u;

		public virtual uint MsSessionTimeout => 15000u;

		public bool MuteDebugUpdateDeviceStatusChanged { get; set; }

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

		public ObservableCollection<string> TextConsoleLines { get; } = new ObservableCollection<string>();


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

		public FeatureSupport FeatureSupported
		{
			get
			{
				return _featuresSupported;
			}
			set
			{
				SetBackingField(ref _featuresSupported, value, "FeatureSupported");
			}
		}

		public LogicalDeviceLevelerType4(ILogicalDeviceId logicalDeviceId, LogicalDeviceLevelerCapabilityType4 levelerCapability, ILogicalDeviceService deviceService, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, new LogicalDeviceLevelerStatusType4(), levelerCapability, deviceService, isFunctionClassChangeable)
		{
			IdsCanCommandRunner = new LogicalDeviceCommandRunnerIdsCan(this)
			{
				CommandProcessingTime = MsCommandTimeout,
				SessionKeepAliveTime = MsSessionTimeout
			};
			IdsCanCommandRunner.PropertyChanged += OnIdsCanCommandRunnerPropertyChanged;
			_uiSupportedFeaturesPid = MakeUiSupportedFeaturesPid();
			_uiSupportedFeaturePidValue = null;
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
				if (_uiSupportedFeaturePidValue == null && !base.IsDisposed)
				{
					_uiSupportedFeaturePidValue = new LogicalDevicePidBindableAsyncValue(_uiSupportedFeaturesPid, autoLoadPid: true, autoSavePid: false, autoRefreshPid: true, DoUpdateFeatureSupported);
					_uiSupportedFeaturePidValue!.ValueToUseWhenInvalidData = 8388608uL;
					_uiSupportedFeaturePidValue!.LoadAsync(CancellationToken.None);
				}
				break;
			}
			case LogicalDeviceActiveConnection.Offline:
			case LogicalDeviceActiveConnection.Remote:
			case LogicalDeviceActiveConnection.Cloud:
				TextConsoleMessage = "";
				_uiSupportedFeaturePidValue?.TryDispose();
				_uiSupportedFeaturePidValue = null;
				break;
			}
		}

		protected override void DebugUpdateDeviceStatusChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, uint dataLength, string optionalText = "")
		{
			if (_delayStatusChangedLogMessageThrottleTimer.ElapsedMilliseconds >= 60000)
			{
				base.DebugUpdateDeviceStatusChanged(oldStatusData, statusData, dataLength, $"{DeviceStatus.XAngle:F1}°/{DeviceStatus.YAngle:F1}° - Skipped debug messages over {60000u}ms");
				_delayStatusChangedLogMessageThrottleTimer.Restart();
			}
		}

		protected override bool ShouldNotifyDeviceStatusChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, int dataLength, int matchingDataLength)
		{
			if (MuteDebugUpdateDeviceStatusChanged)
			{
				return false;
			}
			if (matchingDataLength < 5)
			{
				_delayStatusChange.Stop();
				return true;
			}
			if (_delayStatusChange.IsRunning && _delayStatusChange.ElapsedMilliseconds >= 250)
			{
				_delayStatusChange.Stop();
				return true;
			}
			if (dataLength == matchingDataLength)
			{
				return false;
			}
			if (!_delayStatusChange.IsRunning)
			{
				_delayStatusChange.Restart();
			}
			return false;
		}

		public void UpdateTextConsole(ITextConsole textConsole)
		{
			_cachedTextConsole = textConsole;
			TextConsoleWidth = textConsole.Size.Width;
			TextConsoleHeight = textConsole.Size.Height;
			TextConsoleMessage = textConsole.Text();
		}

		public void OnDeviceTextConsoleMessageChanged()
		{
			MainThread.RequestMainThreadAction(delegate
			{
				List<string> list = _cachedTextConsole?.ToList();
				TextConsoleLines.Clear();
				if (list == null)
				{
					return;
				}
				foreach (string item in list)
				{
					TextConsoleLines.Add(item);
				}
			});
		}

		public async Task ActivateSession(CancellationToken cancelToken)
		{
			try
			{
				await IdsCanCommandRunner.CommandActivateSession(cancelToken);
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceLevelerType4", $"Unable to ActiveSession for {base.LogicalId} with error {ex.Message}");
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
			return await SendCommand(LogicalDeviceLevelerCommandType4.MakeWakeupCommand(DeviceStatus.ScreenSelected), cancelToken);
		}

		public async Task<CommandResult> SendCommand(ILogicalDeviceLevelerCommandType4 command, CancellationToken callerCancelToken, bool ignoreScreenValidation = false)
		{
			if (!IdsCanCommandRunner.CommandSessionActivated || ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				return CommandResult.ErrorNoSession;
			}
			if (_commandCancelSource == null)
			{
				return CommandResult.Canceled;
			}
			if (command is ILogicalDeviceLevelerCommandWithScreenSelectionType4 logicalDeviceLevelerCommandWithScreenSelectionType && logicalDeviceLevelerCommandWithScreenSelectionType.ScreenSelected != DeviceStatus.ScreenSelected && !ignoreScreenValidation)
			{
				TaggedLog.Debug("LogicalDeviceLevelerType4", $"Unable to send command {command} because screen doesn't match current screen {logicalDeviceLevelerCommandWithScreenSelectionType.ScreenSelected} != {DeviceStatus.ScreenSelected}.");
				return CommandResult.ErrorCommandNotAllowed;
			}
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceLevelerType4", "Unable to send command to disposed LogicalDevice " + DeviceName);
				return CommandResult.ErrorOther;
			}
			using CancellationTokenSource cancelSource = CancellationTokenSource.CreateLinkedTokenSource(callerCancelToken, _commandCancelSource.Token);
			if (cancelSource.IsCancellationRequested)
			{
				return CommandResult.Canceled;
			}
			CommandSendOption commandSendOption = CommandSendOption.CancelCurrentCommand;
			if (command.Equals(_lastSentCommand) || command.Command == LogicalDeviceLevelerCommandType4.LevelerCommandCode.Home)
			{
				commandSendOption |= CommandSendOption.AutoClearLockoutDisabled;
			}
			if (DeviceService.GetPrimaryDeviceSourceDirect(this) is IDirectCommandLeveler4 directCommandLeveler)
			{
				return await directCommandLeveler.SendDirectCommandLeveler4(this, command, cancelSource.Token);
			}
			CommandResult commandResult = await IdsCanCommandRunner.SendCommandAsync(command, cancelSource.Token, (ILogicalDevice logicalDevice) => CommandControl.Completed, commandSendOption);
			_lastSentCommand = command;
			if (commandResult != 0)
			{
				TaggedLog.Debug("LogicalDeviceLevelerType4", $"Unable to successfully send leveler command because {commandResult}");
			}
			return commandResult;
		}

		protected virtual ILogicalDevicePidULong MakeUiSupportedFeaturesPid()
		{
			return new LogicalDevicePidULong(this, PID.LEVELER_UI_SUPPORTED_FEATURES, LogicalDeviceSessionType.None);
		}

		private void DoUpdateFeatureSupported(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "Value" || args.PropertyName == "HasValueBeenLoaded")
			{
				if (!(sender is LogicalDevicePidBindableAsyncValue logicalDevicePidBindableAsyncValue) || !logicalDevicePidBindableAsyncValue.HasValueBeenLoaded)
				{
					FeatureSupported = FeatureSupport.Unknown;
				}
				else
				{
					FeatureSupported = (FeatureSupport)logicalDevicePidBindableAsyncValue.Value;
				}
			}
		}

		public virtual async Task<(LogicalDeviceLevelerScreenType4 stepsScreen, int stepsCount, int stepsCompleted)> GetAutoStepsProgressAsync(CancellationToken cancelToken)
		{
			if (base.IsDisposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}
			if (ActiveConnection != LogicalDeviceActiveConnection.Direct)
			{
				throw new PhysicalDeviceNotFoundException("LogicalDeviceLevelerType4", "");
			}
			ulong num = await new LogicalDevicePid(this, PID.LEVELER_AUTO_MODE_PROGRESS, LogicalDeviceSessionType.None).ReadValueAsync(cancelToken);
			LogicalDeviceLevelerScreenType4 logicalDeviceLevelerScreenType = (LogicalDeviceLevelerScreenType4)(num & 0xFF);
			int num2 = (int)((num >> 8) & 0xFF);
			int num3 = (int)((num >> 16) & 0xFF);
			if (num3 > num2)
			{
				throw new ArgumentException("Invalid Step data stepsCompleted > stepsCount", "stepsCompleted");
			}
			return (logicalDeviceLevelerScreenType, num2, num3);
		}

		public virtual async Task UpdateAutoStepsCollectionWithLatestDetails(int expectedStepsCount, BaseObservableCollection<(LogicalDeviceLevelerAutoStepType4 autoStep, int index)> collection, CancellationToken cancelToken)
		{
			BaseObservableCollection<(LogicalDeviceLevelerAutoStepType4 autoStep, int index)> collection2 = collection;
			List<LogicalDeviceLevelerAutoStepType4> autoStepsList = await GetAutoStepListDetailsAsync(expectedStepsCount, cancelToken);
			await MainThread.RequestMainThreadActionAsync(delegate
			{
				using (collection2.SuppressEvents())
				{
					collection2.Clear();
					int num = 0;
					foreach (LogicalDeviceLevelerAutoStepType4 item in autoStepsList)
					{
						collection2.Add((item, num++));
					}
				}
			});
		}

		public virtual async Task<List<LogicalDeviceLevelerAutoStepType4>> GetAutoStepListDetailsAsync(int expectedStepsCount, CancellationToken cancelToken)
		{
			if (base.IsDisposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}
			if (ActiveConnection != LogicalDeviceActiveConnection.Direct)
			{
				throw new PhysicalDeviceNotFoundException("LogicalDeviceLevelerType4", "");
			}
			List<LogicalDeviceLevelerAutoStepType4> autoStepsList = new List<LogicalDeviceLevelerAutoStepType4>();
			LogicalDevicePid currentPid = null;
			for (int index = 0; index < expectedStepsCount && index < _autoStepsDetailPidList.Count * 6; index++)
			{
				if (index % 6 == 0)
				{
					currentPid = new LogicalDevicePid(this, _autoStepsDetailPidList[index / 6], LogicalDeviceSessionType.None);
				}
				ulong num = await currentPid.ReadValueAsync(cancelToken);
				int num2 = 5 - index % 6;
				LogicalDeviceLevelerAutoStepType4 logicalDeviceLevelerAutoStepType = (LogicalDeviceLevelerAutoStepType4)((num >> num2 * 8) & 0xFF);
				if (logicalDeviceLevelerAutoStepType == LogicalDeviceLevelerAutoStepType4.None)
				{
					throw new ArgumentException($"Invalid (no step) defined for step {index} of {expectedStepsCount}", "expectedStepsCount");
				}
				autoStepsList.Add(logicalDeviceLevelerAutoStepType);
			}
			if (autoStepsList.Count != expectedStepsCount)
			{
				throw new ArgumentException($"Complete step data not available {autoStepsList.Count} of {expectedStepsCount} found.", "expectedStepsCount");
			}
			return autoStepsList;
		}

		public ILogicalDeviceLightDimmable? GetAssociatedDimmableLight()
		{
			List<ILogicalDeviceLightDimmable> list = DeviceService.DeviceManager?.FindLogicalDevices((ILogicalDeviceLightDimmable ld) => (object)ld.LogicalId.ProductMacAddress != null && ld.LogicalId.ProductMacAddress == base.LogicalId.ProductMacAddress);
			list?.Sort(delegate(ILogicalDeviceLightDimmable light1, ILogicalDeviceLightDimmable light2)
			{
				if (light1.LogicalId == null && light2.LogicalId == null)
				{
					return 0;
				}
				if (light1.LogicalId == null)
				{
					return 1;
				}
				return (light2.LogicalId == null) ? (-1) : light1.LogicalId.CompareTo(light2.LogicalId);
			});
			if (list != null && list.Count > 1)
			{
				TaggedLog.Warning("LogicalDeviceLevelerType4", $"{list.Count} associated lights found for Leveler, only 1 is expected.");
			}
			if (list == null)
			{
				return null;
			}
			return Enumerable.FirstOrDefault(list);
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
			_uiSupportedFeaturePidValue?.TryDispose();
			_uiSupportedFeaturePidValue = null;
			base.Dispose(disposing);
		}

		NETWORK_STATUS ILogicalDeviceIdsCan.get_LastReceivedNetworkStatus()
		{
			return base.LastReceivedNetworkStatus;
		}
	}
}
