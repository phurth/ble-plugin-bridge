using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevices;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayHBridgeMomentary<TRelayHBridgeStatus, TRelayCommandFactory, TCapability> : LogicalDevice<TRelayHBridgeStatus, TCapability>, ILogicalDeviceRelayHBridgeDirect<TRelayHBridgeStatus>, ILogicalDeviceRelayHBridge<TRelayHBridgeStatus>, ILogicalDeviceRelayHBridge, IRelayHBridge, IDevicesCommon, INotifyPropertyChanged, IRelayAutoOperations, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceWithStatus<TRelayHBridgeStatus>, ILogicalDeviceWithStatus, ILogicalDeviceWithStatusUpdate<TRelayHBridgeStatus>, ILogicalDeviceIdsCan, ILogicalDeviceMyRvLink where TRelayHBridgeStatus : class, ILogicalDeviceRelayHBridgeStatus, new()where TRelayCommandFactory : ILogicalDeviceRelayHBridgeCommandFactory, new()where TCapability : ILogicalDeviceRelayCapability
	{
		private const string LogTag = "LogicalDeviceRelayHBridgeMomentary";

		public const int MsCommandDirectionAutoGetSessionTimeout = 1000;

		public const int MsCommandDirectionAutoStopTimeout = 800;

		public const int MsCommandTimeout = 5000;

		public const int MsSessionTimeout = 15000;

		protected Stopwatch CommandDirectionLastSetTimer = Stopwatch.StartNew();

		protected TRelayCommandFactory CommandFactory = new TRelayCommandFactory();

		protected ILogicalDeviceCommandRunnerIdsCan IdsCanCommandRunner;

		private RelayHBridgeEnergized _relayEnergized;

		private RelayHBridgeEnergized _commandRelayEnergized;

		private CancellationTokenSource _commandCancelSource = new CancellationTokenSource();

		private ILogicalDeviceRelayHBridgeCommand _lastSentCommand;

		private readonly Stopwatch _lastCommandSendTime = Stopwatch.StartNew();

		private bool _faultStatus;

		private bool _userClearRequired;

		private bool _deviceHasFaulted;

		private bool _isHoming;

		private DTC_ID _userMessageDtc;

		protected ILogicalDevicePidFixedPoint BatteryVoltagePidCan;

		public const int CommandErrorDelayTimeMs = 250;

		public const int RelayDirectionSendTimeMs = 500;

		public const int RelayDirectionSendTimeHighSpeedMs = 175;

		public const int MaxStopCommands = 5;

		public override bool ShouldAutoClearInTransitLockout
		{
			get
			{
				if (DeviceStatus.IsStopped)
				{
					return base.ShouldAutoClearInTransitLockout;
				}
				return false;
			}
		}

		public virtual bool CommandSessionActivated => IdsCanCommandRunner.CommandSessionActivated;

		public virtual bool IsAutoOperationInProgress => false;

		public virtual bool IsWindSensorAutoOperationInProgress => false;

		public virtual bool IsAutoOperation => false;

		public virtual bool SendDirectionRequired { get; } = true;


		public virtual bool AutoForwardAllowed => false;

		public virtual bool AutoReverseAllowed => false;

		public bool AreAutoCommandsSupported => base.DeviceCapability.AreAutoCommandsSupported;

		public bool IsHomingSupported => base.DeviceCapability.IsHomingSupported;

		public bool IsAwningSensorSupported => base.DeviceCapability.IsAwningSensorSupported;

		public RelayHBridgeDirectionVerbose DirectionVerbose => RelayEnergized.ConvertToVerboseDirection(base.LogicalId);

		public RelayHBridgeDirectionVerbose CommandDirectionVerbose => CommandRelayEnergized.ConvertToVerboseDirection(base.LogicalId);

		public DeviceCategory DeviceCategory => DeviceCategory.GetDeviceCategory(base.LogicalId);

		public bool FaultStatus
		{
			get
			{
				return _faultStatus;
			}
			private set
			{
				SetBackingField(ref _faultStatus, value, "FaultStatus");
			}
		}

		public bool UserClearRequired
		{
			get
			{
				return _userClearRequired;
			}
			private set
			{
				SetBackingField(ref _userClearRequired, value, "UserClearRequired");
			}
		}

		public bool IsHoming
		{
			get
			{
				return _isHoming;
			}
			private set
			{
				SetBackingField(ref _isHoming, value, "IsHoming");
			}
		}

		public DTC_ID UserMessageDtc
		{
			get
			{
				return _userMessageDtc;
			}
			private set
			{
				SetBackingField(ref _userMessageDtc, value, "UserMessageDtc");
			}
		}

		public RelayHBridgeEnergized RelayEnergized
		{
			get
			{
				return _relayEnergized;
			}
			private set
			{
				if (value != _relayEnergized)
				{
					if (value < RelayHBridgeEnergized.Relay2 || value > RelayHBridgeEnergized.Relay1)
					{
						throw new ArgumentException("Relay.Direction: value must be in the range of [-1,1].");
					}
					_relayEnergized = value;
					OnPropertyChanged("RelayEnergized");
					OnPropertyChanged("DirectionVerbose");
				}
			}
		}

		public RelayHBridgeEnergized CommandRelayEnergized
		{
			get
			{
				return _commandRelayEnergized;
			}
			private set
			{
				if (value == _commandRelayEnergized)
				{
					CommandDirectionLastSetTimer.Restart();
					return;
				}
				if (value < RelayHBridgeEnergized.Relay2 || value > RelayHBridgeEnergized.Relay1)
				{
					throw new ArgumentException("Relay.CommandDirection: value must be in the range of [-1,1].");
				}
				_commandRelayEnergized = value;
				CommandDirectionLastSetTimer.Restart();
				OnPropertyChanged("CommandRelayEnergized");
				OnPropertyChanged("CommandDirectionVerbose");
			}
		}

		public bool Relay1Allowed
		{
			get
			{
				bool flag = DeviceStatus.CommandForwardAllowed(this);
				bool flag2 = DeviceStatus.CommandReverseAllowed(this);
				if (!(flag && flag2))
				{
					return RelayHBridgeEnergizedExtension.ConvertToRelayEnergized(flag, flag2, base.LogicalId) == RelayHBridgeEnergized.Relay1;
				}
				return true;
			}
		}

		public bool Relay2Allowed
		{
			get
			{
				bool flag = DeviceStatus.CommandForwardAllowed(this);
				bool flag2 = DeviceStatus.CommandReverseAllowed(this);
				if (!(flag && flag2))
				{
					return RelayHBridgeEnergizedExtension.ConvertToRelayEnergized(flag, flag2, base.LogicalId) == RelayHBridgeEnergized.Relay2;
				}
				return true;
			}
		}

		public virtual LogicalDeviceExScope VoltageMeasurementBatteryPidScope => LogicalDeviceExScope.Product;

		public virtual ILogicalDevicePidFixedPoint VoltageMeasurementBatteryPid => BatteryVoltagePidCan ?? (BatteryVoltagePidCan = new LogicalDevicePidFixedPoint(FixedPointType.UnsignedBigEndian16x16, this, PID.BATTERY_VOLTAGE, LogicalDeviceSessionType.None));

		public bool IsVoltagePidReadSupported => true;

		public static bool HighSpeedTransmission { get; set; }

		public virtual bool AutoForwardCommand()
		{
			return false;
		}

		public virtual bool AutoReverseCommand()
		{
			return false;
		}

		public LogicalDeviceRelayHBridgeMomentary(ILogicalDeviceId logicalDeviceId, TCapability capability, ILogicalDeviceService service = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, MakeNewStatus(), capability, service, isFunctionClassChangeable)
		{
			IdsCanCommandRunner = new LogicalDeviceCommandRunnerIdsCan(this)
			{
				CommandProcessingTime = 5000u,
				SessionKeepAliveTime = 15000u
			};
			IdsCanCommandRunner.PropertyChanged += OnCommandRunnerPropertyChanged;
		}

		public static TRelayHBridgeStatus MakeNewStatus()
		{
			return new TRelayHBridgeStatus();
		}

		protected void OnCommandRunnerPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "CommandSessionActivated")
			{
				OnPropertyChanged("CommandSessionActivated");
			}
		}

		public override void OnDeviceOnlineChanged()
		{
			base.OnDeviceOnlineChanged();
			if (ActiveConnection == LogicalDeviceActiveConnection.Offline && !DeviceStatus.IsStopped)
			{
				TaggedLog.Debug("LogicalDeviceRelayHBridgeMomentary", "Changing relay status to stopped as it went offline " + DeviceName);
				ILogicalDeviceRelayHBridgeStatus logicalDeviceRelayHBridgeStatus = DeviceStatus.CopyStatus();
				logicalDeviceRelayHBridgeStatus.SetState(relay1State: false, relay2State: false, base.LogicalId);
				UpdateDeviceStatus(logicalDeviceRelayHBridgeStatus.Data, logicalDeviceRelayHBridgeStatus.Size);
			}
		}

		public override void OnDeviceStatusChanged()
		{
			RelayEnergized = ResolveRelayEnergized(DeviceStatus.Relay1State(base.LogicalId), DeviceStatus.Relay2State(base.LogicalId));
			FaultStatus = DeviceStatus.IsFaulted;
			UserClearRequired = DeviceStatus.UserClearRequired;
			IsHoming = DeviceStatus.IsHoming;
			UserMessageDtc = DeviceStatus.UserMessageDtc;
			base.OnDeviceStatusChanged();
			OnPropertyChanged("Relay1Allowed");
			OnPropertyChanged("Relay2Allowed");
		}

		protected override void DebugUpdateDeviceStatusChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, uint dataLength, string optionalText = "")
		{
			RelayHBridgeDirection hBridgeDirection = DeviceStatus.GetHBridgeDirection(base.LogicalId);
			TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", $"Data Changed {base.LogicalId}: {DeviceStatus} Direction = {hBridgeDirection}, Relay1(right) = {DeviceStatus.Relay1State(base.LogicalId)}, Relay2(left) = {DeviceStatus.Relay2State(base.LogicalId)}, Faults = {DeviceStatus.IsFaulted} {optionalText}");
		}

		public override void UpdateInTransitLockout()
		{
			base.UpdateInTransitLockout();
			NotifyPropertyChanged("Relay1Allowed");
			NotifyPropertyChanged("Relay2Allowed");
		}

		public virtual Task<CommandResult> TryAutoReverseAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(CommandResult.ErrorCommandNotAllowed);
		}

		public virtual Task<CommandResult> TryAutoForwardAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(CommandResult.ErrorCommandNotAllowed);
		}

		protected async Task<CommandResult> SendDirectionCommand(HBridgeCommand hBridgeCommand, CancellationToken callerCancelToken)
		{
			if (ActiveConnection != LogicalDeviceActiveConnection.Direct)
			{
				TaggedLog.Debug("LogicalDeviceRelayHBridgeMomentary", "SendDirectionCommand Direct control not online for " + DeviceName);
				return CommandResult.ErrorDeviceOffline;
			}
			if (!IdsCanCommandRunner.CommandSessionActivated)
			{
				TaggedLog.Debug("LogicalDeviceRelayHBridgeMomentary", "SendDirectionCommand Session Not Activated so stopping relay for " + DeviceName);
				return CommandResult.ErrorNoSession;
			}
			if (!ActiveSession && CommandDirectionLastSetTimer.ElapsedMilliseconds > 1000)
			{
				TaggedLog.Debug("LogicalDeviceRelayHBridgeMomentary", $"No session active for relay {DeviceName}, command changes must be received within {1000}ms");
				RelayHBridgeEnergized relayHBridgeEnergized = ResolveRelayEnergized(DeviceStatus.Relay1State(base.LogicalId), DeviceStatus.Relay2State(base.LogicalId));
				if (relayHBridgeEnergized != 0)
				{
					TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", $"SendDirectionCommand Command={CommandDirectionVerbose} CurrentState={relayHBridgeEnergized} ERROR NO SESSION ACTIVE But Relay is moving!");
				}
				return CommandResult.ErrorOther;
			}
			if (_commandCancelSource == null)
			{
				TaggedLog.Debug("LogicalDeviceRelayHBridgeMomentary", "SendDirectionCommand No cancel source so stopping relay for " + DeviceName);
				return CommandResult.Canceled;
			}
			if (base.IsDisposed)
			{
				TaggedLog.Debug("LogicalDeviceRelayHBridgeMomentary", "SendDirectionCommand Unable to send command to disposed LogicalDevice " + DeviceName);
				return CommandResult.ErrorOther;
			}
			if (!Relay1Allowed && !Relay2Allowed)
			{
				TaggedLog.Debug("LogicalDeviceRelayHBridgeMomentary", "SendDirectionCommand Unable to send command for " + DeviceName + ", neither relay is allowed.");
				return CommandResult.ErrorCommandNotAllowed;
			}
			using CancellationTokenSource cancelSource = CancellationTokenSource.CreateLinkedTokenSource(callerCancelToken, _commandCancelSource.Token);
			bool relay1Energized = false;
			bool relay2Energized = false;
			TRelayCommandFactory val;
			ILogicalDeviceRelayHBridgeCommand command;
			switch (hBridgeCommand)
			{
			case HBridgeCommand.Forward:
			{
				if (ForwardRelay() == RelayHBridgeEnergized.Relay1)
				{
					relay1Energized = true;
				}
				else
				{
					relay2Energized = true;
				}
				if (await AutoClearFaultIfNeeded(relay1Energized, relay2Energized, checkOnly: false, _commandCancelSource.Token))
				{
					return CommandResult.Completed;
				}
				if (!Relay1Allowed && !Relay2Allowed)
				{
					relay1Energized = false;
					relay2Energized = false;
				}
				CommandRelayEnergized = ResolveRelayEnergized(relay1Energized, relay2Energized);
				ref TRelayCommandFactory reference2 = ref CommandFactory;
				val = default(TRelayCommandFactory);
				if (val == null)
				{
					val = reference2;
					reference2 = ref val;
				}
				command = reference2.MakeRelayCommand(new LogicalDeviceRelayHBridgeDirection(CommandRelayEnergized, base.LogicalId));
				break;
			}
			case HBridgeCommand.Reverse:
			{
				if (ReverseRelay() == RelayHBridgeEnergized.Relay1)
				{
					relay1Energized = true;
				}
				else
				{
					relay2Energized = true;
				}
				if (await AutoClearFaultIfNeeded(relay1Energized, relay2Energized, checkOnly: false, _commandCancelSource.Token))
				{
					return CommandResult.Completed;
				}
				if (!Relay1Allowed && !Relay2Allowed)
				{
					relay1Energized = false;
					relay2Energized = false;
				}
				CommandRelayEnergized = ResolveRelayEnergized(relay1Energized, relay2Energized);
				ref TRelayCommandFactory reference = ref CommandFactory;
				val = default(TRelayCommandFactory);
				if (val == null)
				{
					val = reference;
					reference = ref val;
				}
				command = reference.MakeRelayCommand(new LogicalDeviceRelayHBridgeDirection(CommandRelayEnergized, base.LogicalId));
				break;
			}
			case HBridgeCommand.Stop:
				command = CommandFactory.MakeRelayStopCommand();
				break;
			case HBridgeCommand.ClearUserClearRequiredLatch:
				command = CommandFactory.MakeClearFaultCommand();
				break;
			case HBridgeCommand.HomeReset:
				command = CommandFactory.MakeHomeResetCommand();
				break;
			case HBridgeCommand.AutoForward:
				command = CommandFactory.MakeAutoForwardCommand();
				break;
			case HBridgeCommand.AutoReverse:
				command = CommandFactory.MakeAutoReverseCommand();
				break;
			default:
				return CommandResult.ErrorCommandNotAllowed;
			}
			if (UserClearRequired)
			{
				TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", "UserClearRequired, overriding current command with stop command.");
				_deviceHasFaulted = true;
				command = CommandFactory.MakeRelayStopCommand();
			}
			if (_deviceHasFaulted)
			{
				TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", "Device has faulted during this press, overriding current command with stop command.");
				command = CommandFactory.MakeRelayStopCommand();
			}
			if (CommandRelayEnergized != 0 && CommandDirectionLastSetTimer.ElapsedMilliseconds > 800)
			{
				TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", $"Performing auto stop as last direction command was received at {CommandDirectionLastSetTimer.ElapsedMilliseconds}ms and must be received within {800}ms for {DeviceName}");
				command = CommandFactory.MakeRelayStopCommand();
			}
			if (command.IsStopCommand && DeviceStatus.IsStopped)
			{
				bool? flag = _lastSentCommand?.IsStopCommand;
				if (flag.HasValue && flag.GetValueOrDefault() && !UserClearRequired)
				{
					TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", "Command is stop and relay is already stopped, returning CommandResult.Completed.");
					return CommandResult.Completed;
				}
			}
			if (!command.Allowed(this))
			{
				if (ShouldAutoClearInTransitLockout)
				{
					((DeviceService.DeviceSourceManager.GetPrimaryDeviceSource<ILogicalDeviceSourceDirectConnectionIdsCan>(this)?.Gateway)?.LocalHost)?.SendDisableInMotionLockoutCommand();
				}
				bool flag2 = DeviceStatus.CommandForwardAllowed(this);
				bool flag3 = DeviceStatus.CommandReverseAllowed(this);
				TaggedLog.Debug("LogicalDeviceRelayHBridgeMomentary", $"CommandForwardAllowed={flag2} CommandReverseAllowed={flag3} Left Allowed={Relay2Allowed}, Right Allowed={Relay1Allowed} {this}");
				TaggedLog.Debug("LogicalDeviceRelayHBridgeMomentary", $"Command currently not allowed {command},  InTransit {base.InTransitLockout}/{base.HazardousDuringInTransitLockout} {this}");
				return CommandResult.ErrorCommandNotAllowed;
			}
			_lastSentCommand = command;
			long lastSentCommandMs = _lastCommandSendTime.ElapsedMilliseconds;
			_lastCommandSendTime.Restart();
			try
			{
				if (DeviceService.GetPrimaryDeviceSourceDirect(this) is IDirectCommandMovement directCommandMovement)
				{
					CommandResult commandResult = await directCommandMovement.SendDirectCommandRelayMomentary(this, command.Command, cancelSource.Token);
					TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", string.Format("SendDirectionCommand[{0}] Command={1} Result={2} CurrentState={3} LastSent: {4}ms", "IDirectCommandMovement", command, commandResult, ResolveRelayEnergized(DeviceStatus.Relay1State(base.LogicalId), DeviceStatus.Relay2State(base.LogicalId)), lastSentCommandMs));
					return commandResult;
				}
				CommandResult commandResult2 = await IdsCanCommandRunner.SendCommandAsync(command, cancelSource.Token, null, CommandSendOption.CancelCurrentCommand);
				TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", $"SendDirectionCommand Command={command} Result={commandResult2} CurrentState={ResolveRelayEnergized(DeviceStatus.Relay1State(base.LogicalId), DeviceStatus.Relay2State(base.LogicalId))} LastSent: {lastSentCommandMs}ms");
				return commandResult2;
			}
			catch (Exception ex)
			{
				TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", $"SendDirectionCommand Command={command} Result={ex.Message} CurrentState={ResolveRelayEnergized(DeviceStatus.Relay1State(base.LogicalId), DeviceStatus.Relay2State(base.LogicalId))} LastSent: {lastSentCommandMs}ms");
				return CommandResult.ErrorOther;
			}
		}

		protected async Task ActivateSession(CancellationToken cancelToken)
		{
			try
			{
				TryAbortAutoOperation();
				await IdsCanCommandRunner.CommandActivateSession(cancelToken, ActiveConnection == LogicalDeviceActiveConnection.Direct);
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDeviceRelayHBridgeMomentary", $"Unable to ActiveSession for {base.LogicalId} with error {ex.Message}");
			}
		}

		protected void DeactivateSession()
		{
			IdsCanCommandRunner.CommandDeactivateSession();
		}

		public void TryAbortAutoOperation()
		{
			DeviceService.GetExclusiveOperation<IRelayAutoOperations>().RequestStop();
		}

		protected async Task<bool> AutoClearFaultIfNeeded(bool relay1Energized, bool relay2Energized, bool checkOnly, CancellationToken cancelToken)
		{
			if (!UserClearRequired)
			{
				return false;
			}
			TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", "AutoClearFaultIfNeeded UserClearRequired.");
			if (CommandRelayEnergized != 0)
			{
				return false;
			}
			if (!relay1Energized && !relay2Energized)
			{
				return false;
			}
			if (DeviceService.GetPrimaryDeviceSourceDirect(this) is IDirectCommandMovement)
			{
				return false;
			}
			if (!checkOnly && IdsCanCommandRunner.CommandSessionActivated && _commandCancelSource != null)
			{
				TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", "Attempting to send a clear fault command.");
				ILogicalDeviceRelayHBridgeCommand dataPacket = CommandFactory.MakeClearFaultCommand();
				await IdsCanCommandRunner.SendCommandAsync(dataPacket, _commandCancelSource.Token);
			}
			return true;
		}

		protected static RelayHBridgeEnergized ResolveRelayEnergized(bool relay1, bool relay2)
		{
			if (relay1 == relay2)
			{
				return RelayHBridgeEnergized.None;
			}
			if (!relay1)
			{
				return RelayHBridgeEnergized.Relay2;
			}
			return RelayHBridgeEnergized.Relay1;
		}

		public override bool Rename(FUNCTION_NAME newFunctionName, int newFunctionInstance)
		{
			bool num = base.Rename(newFunctionName, newFunctionInstance);
			if (num)
			{
				OnPropertyChanged("DirectionVerbose");
				OnPropertyChanged("CommandDirectionVerbose");
			}
			return num;
		}

		public Task<float> ReadVoltageMeasurementAsync(CancellationToken cancellationToken)
		{
			return LogicalDeviceVoltageExtension.ReadVoltageMeasurementAsync(VoltageMeasurementBatteryPid, cancellationToken);
		}

		public override void Dispose(bool disposing)
		{
			try
			{
				IdsCanCommandRunner.PropertyChanged -= OnCommandRunnerPropertyChanged;
			}
			catch
			{
			}
			_commandCancelSource?.CancelAndDispose();
			_commandCancelSource = null;
			IdsCanCommandRunner?.TryDispose();
			base.Dispose(disposing);
		}

		public async Task PerformMovementOperationAsync(RelayHBridgeDirection direction, Action<RelayMovementStatus> updateRelayMovementStatus, CancellationToken cancellationToken)
		{
			CancellationTokenSource exclusiveOperationCts = new CancellationTokenSource();
			CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, exclusiveOperationCts.Token);
			CancellationToken linkedToken = cancellationTokenSource.Token;
			int relayDirectionSendTime = (HighSpeedTransmission ? 175 : 500);
			LogicalDeviceExclusiveOperation exclusiveOperation = DeviceService.GetExclusiveOperation<ILogicalDeviceRelayHBridge>();
			IDisposable startedExclusiveOperation = exclusiveOperation.Start(delegate
			{
				exclusiveOperationCts.TryCancelAndDispose();
			});
			if (startedExclusiveOperation == null)
			{
				TaggedLog.Error("LogicalDeviceRelayHBridgeMomentary", "PerformMovementOperationAsync failed to start an exclusive operation.");
				updateRelayMovementStatus(RelayMovementStatus.UnableToMoveNoExclusiveOperation);
				throw new MomentaryRelayOperationInProgressException("PerformMovementOperationAsync failed to start an exclusive operation.");
			}
			try
			{
				await ActivateSession(linkedToken);
				if (direction == RelayHBridgeDirection.Unknown)
				{
					TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", "PerformMovementOperationAsync direction of unknown is not supported.");
					throw new MomentaryRelayDirectionException(direction);
				}
				if (ActiveConnection == LogicalDeviceActiveConnection.Offline)
				{
					TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", $"PerformMovementOperationAsync device is offline: {this}");
					updateRelayMovementStatus(RelayMovementStatus.UnableToMoveNoConnection);
					throw new MomentaryRelayNoActiveConnectionException();
				}
				if (base.InTransitLockout.IsInLockout())
				{
					TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", "PerformMovementOperationAsync lockout occurred while attempting to command relay; stopping commands.");
					throw new MomentaryRelayInLockoutException();
				}
				if (!ActiveSession)
				{
					if (direction != 0)
					{
						TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", $"PerformMovementOperationAsync can't send direction of {direction} as session isn't active: {this}");
					}
					updateRelayMovementStatus(RelayMovementStatus.UnableToMoveNoSession);
					throw new MomentaryRelayNoActiveSessionException();
				}
				bool performedStopCommand = RelayEnergized == RelayHBridgeEnergized.None;
				while (!linkedToken.IsCancellationRequested)
				{
					try
					{
						if (RelayEnergized == RelayHBridgeEnergized.None && performedStopCommand && direction == RelayHBridgeDirection.Stop && !_deviceHasFaulted)
						{
							updateRelayMovementStatus(RelayMovementStatus.AbleToMove);
							TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", "PerformMovementOperationAsync performed stop command and relay stopped.");
						}
						else
						{
							if (UserClearRequired || _deviceHasFaulted)
							{
								updateRelayMovementStatus(RelayMovementStatus.UnableToMoveFaulted);
							}
							else
							{
								updateRelayMovementStatus(RelayMovementStatus.AbleToMove);
							}
							HBridgeCommand hBridgeCommand = HBridgeCommand.Unknown;
							switch (direction)
							{
							case RelayHBridgeDirection.Forward:
								hBridgeCommand = HBridgeCommand.Forward;
								performedStopCommand = false;
								break;
							case RelayHBridgeDirection.Reverse:
								hBridgeCommand = HBridgeCommand.Reverse;
								performedStopCommand = false;
								break;
							case RelayHBridgeDirection.Stop:
								hBridgeCommand = HBridgeCommand.Stop;
								if (!performedStopCommand)
								{
									performedStopCommand = true;
									break;
								}
								goto end_IL_024b;
							}
							TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", $"(PerformMovementOperationAsync Send Direction Command {hBridgeCommand}: {this}");
							CommandResult commandResult = await SendDirectionCommand(hBridgeCommand, linkedToken).ConfigureAwait(false);
							if (commandResult != 0 && commandResult != CommandResult.ErrorAssumed)
							{
								throw new MomentaryRelayCommandResultException(commandResult);
							}
						}
						end_IL_024b:;
					}
					catch (Exception ex)
					{
						TaggedLog.Error("LogicalDeviceRelayHBridgeMomentary", "PerformMovementOperationAsync exception " + ex.Message);
						await TaskExtension.TryDelay(250, linkedToken).ConfigureAwait(false);
					}
					finally
					{
						await TaskExtension.TryDelay(relayDirectionSendTime, linkedToken).ConfigureAwait(false);
					}
					int num;
					switch (num)
					{
					default:
						continue;
					case 1:
						break;
					}
					break;
				}
				for (int i = 0; i < 5; i++)
				{
					await SendDirectionCommand(HBridgeCommand.Stop, CancellationToken.None).ConfigureAwait(false);
					await Task.Delay(relayDirectionSendTime, CancellationToken.None).ConfigureAwait(false);
					if (RelayEnergized == RelayHBridgeEnergized.None)
					{
						CommandRelayEnergized = RelayHBridgeEnergized.None;
						break;
					}
				}
				DeactivateSession();
				linkedToken.ThrowIfCancellationRequested();
			}
			catch (OperationCanceledException)
			{
				TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", "PerformMovementOperationAsync operation canceled.");
				throw;
			}
			catch (TimeoutException)
			{
				TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", "PerformMovementOperationAsync operation timeout.");
				throw;
			}
			catch (Exception ex4)
			{
				TaggedLog.Warning("LogicalDeviceRelayHBridgeMomentary", "PerformMovementOperationAsync operation error " + ex4.Message);
				throw new Exception("Unable to perform operation", ex4);
			}
			finally
			{
				_deviceHasFaulted = false;
				startedExclusiveOperation?.Dispose();
			}
		}

		public async Task PerformHomeResetOperationAsync(Action<RelayOperationStatus> updateRelayHomeStatus, CancellationToken cancellationToken)
		{
			CancellationTokenSource exclusiveOperationCts = new CancellationTokenSource();
			CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, exclusiveOperationCts.Token);
			CancellationToken linkedToken = cancellationTokenSource.Token;
			int relayDirectionSendTime = (HighSpeedTransmission ? 175 : 500);
			if (!IsHomingSupported)
			{
				TaggedLog.Error("LogicalDeviceRelayHBridgeMomentary", "PerformHomeOperationAsync failed to start an exclusive operation.");
				updateRelayHomeStatus(RelayOperationStatus.UnableToPerformOperationNotSupported);
				throw new MomentaryRelayOperationInProgressException("PerformHomeOperationAsync home reset not supported.");
			}
			LogicalDeviceExclusiveOperation exclusiveOperation = DeviceService.GetExclusiveOperation<ILogicalDeviceRelayHBridge>();
			IDisposable startedExclusiveOperation = exclusiveOperation.Start(delegate
			{
				exclusiveOperationCts.TryCancelAndDispose();
			});
			if (startedExclusiveOperation == null)
			{
				TaggedLog.Error("LogicalDeviceRelayHBridgeMomentary", "PerformHomeOperationAsync failed to start an exclusive operation.");
				updateRelayHomeStatus(RelayOperationStatus.UnableToPerformOperationNoExclusiveOperation);
				throw new MomentaryRelayOperationInProgressException("PerformHomeOperationAsync failed to start an exclusive operation.");
			}
			try
			{
				await ActivateSession(linkedToken);
				if (ActiveConnection == LogicalDeviceActiveConnection.Offline)
				{
					TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", $"PerformHomeOperationAsync device is offline: {this}");
					updateRelayHomeStatus(RelayOperationStatus.UnableToPerformOperationNoConnection);
					throw new MomentaryRelayNoActiveConnectionException();
				}
				if (base.InTransitLockout.IsInLockout())
				{
					TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", "PerformHomeOperationAsync lockout occurred while attempting to command relay; stopping commands.");
					throw new MomentaryRelayInLockoutException();
				}
				if (!ActiveSession)
				{
					TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", $"PerformHomeOperationAsync can't home reset as session isn't active: {this}");
					updateRelayHomeStatus(RelayOperationStatus.UnableToPerformOperationNoSession);
					throw new MomentaryRelayNoActiveSessionException();
				}
				while (!linkedToken.IsCancellationRequested)
				{
					try
					{
						updateRelayHomeStatus(RelayOperationStatus.PerformingOperation);
						CommandDirectionLastSetTimer.Restart();
						TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", $"(PerformHomeOperationAsync Send Home Reset Command: {this}");
						CommandResult commandResult = await SendDirectionCommand(HBridgeCommand.HomeReset, linkedToken).ConfigureAwait(false);
						if (commandResult != 0 && commandResult != CommandResult.ErrorAssumed)
						{
							throw new MomentaryRelayCommandResultException(commandResult);
						}
					}
					catch (Exception ex)
					{
						TaggedLog.Error("LogicalDeviceRelayHBridgeMomentary", "PerformHomeOperationAsync exception " + ex.Message);
						await TaskExtension.TryDelay(250, linkedToken).ConfigureAwait(false);
					}
					finally
					{
						await TaskExtension.TryDelay(relayDirectionSendTime, linkedToken).ConfigureAwait(false);
					}
				}
				for (int i = 0; i < 5; i++)
				{
					await SendDirectionCommand(HBridgeCommand.Stop, CancellationToken.None).ConfigureAwait(false);
					await Task.Delay(relayDirectionSendTime, CancellationToken.None).ConfigureAwait(false);
					if (RelayEnergized == RelayHBridgeEnergized.None)
					{
						break;
					}
				}
				DeactivateSession();
				linkedToken.ThrowIfCancellationRequested();
			}
			catch (OperationCanceledException)
			{
				TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", "PerformHomeOperationAsync operation canceled.");
				throw;
			}
			catch (TimeoutException)
			{
				TaggedLog.Information("LogicalDeviceRelayHBridgeMomentary", "PerformHomeOperationAsync operation timeout.");
				throw;
			}
			catch (Exception ex4)
			{
				TaggedLog.Warning("LogicalDeviceRelayHBridgeMomentary", "PerformHomeOperationAsync operation error " + ex4.Message);
				throw new Exception("Unable to perform operation", ex4);
			}
			finally
			{
				startedExclusiveOperation?.Dispose();
			}
		}

		public bool IsForwardRelayEnergized()
		{
			return RelayEnergized == ForwardRelay();
		}

		public bool IsReverseRelayEnergized()
		{
			return RelayEnergized == ReverseRelay();
		}

		public bool IsForwardRelayAllowed()
		{
			if (ForwardRelay() != RelayHBridgeEnergized.Relay1 || !Relay1Allowed)
			{
				if (ForwardRelay() == RelayHBridgeEnergized.Relay2)
				{
					return Relay2Allowed;
				}
				return false;
			}
			return true;
		}

		public bool IsReverseRelayAllowed()
		{
			if (ReverseRelay() != RelayHBridgeEnergized.Relay1 || !Relay1Allowed)
			{
				if (ReverseRelay() == RelayHBridgeEnergized.Relay2)
				{
					return Relay2Allowed;
				}
				return false;
			}
			return true;
		}

		private RelayHBridgeEnergized ForwardRelay()
		{
			return RelayHBridgeDirection.Forward.ConvertToRelayEnergized(base.LogicalId);
		}

		private RelayHBridgeEnergized ReverseRelay()
		{
			return RelayHBridgeDirection.Reverse.ConvertToRelayEnergized(base.LogicalId);
		}

		NETWORK_STATUS ILogicalDeviceIdsCan.get_LastReceivedNetworkStatus()
		{
			return base.LastReceivedNetworkStatus;
		}
	}
}
