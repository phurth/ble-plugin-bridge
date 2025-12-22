using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDevice;
using OneControl.Devices.AwningSensor;

namespace OneControl.Devices
{
	public class LogicalDeviceRelayHBridgeMomentaryType2 : LogicalDeviceRelayHBridgeMomentary<LogicalDeviceRelayHBridgeStatusType2, LogicalDeviceRelayHBridgeCommandFactoryMomentaryType2, LogicalDeviceRelayHBridgeCapabilityType2>, ILogicalDeviceRelayHBridgeWithAssociatedAwningSensor, ILogicalDeviceRelayHBridgeWithAssociatedDevice, ILogicalDeviceRelayHBridge, IRelayHBridge, IDevicesCommon, INotifyPropertyChanged, IRelayAutoOperations, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, ILogicalDeviceVoltageMeasurementBatteryPid, ILogicalDeviceVoltageMeasurementBattery, ILogicalDeviceVoltageMeasurement, ILogicalDeviceReadVoltageMeasurement, IReadVoltageMeasurement, ILogicalDeviceRelayHBridgeWithAssociatedLight, ILogicalDeviceWithStatusAlerts
	{
		private const string LogTag = "LogicalDeviceRelayHBridgeMomentaryType2";

		private const int AutoOperationLoopDelayMs = 100;

		private const int CommandErrorDelayMs = 500;

		private const int MaxConsecutiveCommandRetries = 4;

		private const int MaxConsecutiveOperationRetries = 10;

		private new const int MaxStopCommands = 5;

		private const byte WindRetractEventAlertId = 0;

		private const string WindRetractEventAlertName = "WindRetractEvent";

		private const byte AlertActiveBitmask = 128;

		private const byte AlertCountBitmask = 127;

		private const byte AlertActiveFlag = 128;

		private static readonly IReadOnlyList<DTC_ID> AutoOperationDtcs = new List<DTC_ID>
		{
			DTC_ID.WIND_EVENT_AUTO_OPERATION_IN_PROGRESS,
			DTC_ID.WIND_EVENT_AUTO_OPERATION_ERROR,
			DTC_ID.WIND_EVENT_AUTO_OPERATION_COMPLETE,
			DTC_ID.USER_AUTO_OPERATION_IN_PROGRESS,
			DTC_ID.USER_AUTO_OPERATION_ERROR,
			DTC_ID.USER_AUTO_OPERATION_COMPLETE
		};

		public ILogicalDevicePidByte AwningWindEventSettingPid;

		public ILogicalDevicePidUInt16 AwningWindEventProtectionCountPid;

		public ILogicalDevicePidByte AutoOperationSafetyConfigPid;

		public ILogicalDevicePidByte MomentaryHBridgeCircuitRolePid;

		private const uint DebugUpdateDeviceStatusChangedThrottleTimeMs = 60000u;

		private readonly Stopwatch _debugUpdateDeviceStatusChangedThrottleTimer = Stopwatch.StartNew();

		private uint _debugUpdateDeviceStatusChangedThrottleCount;

		private readonly ConcurrentDictionary<string, ILogicalDeviceAlert> _alertDict = new ConcurrentDictionary<string, ILogicalDeviceAlert>();

		public IEnumerable<ILogicalDeviceAlert> Alerts => _alertDict.Values;

		public override bool IsAutoOperationInProgress => base.UserMessageDtc == DTC_ID.USER_AUTO_OPERATION_IN_PROGRESS;

		public override bool IsWindSensorAutoOperationInProgress => base.UserMessageDtc == DTC_ID.WIND_EVENT_AUTO_OPERATION_IN_PROGRESS;

		public override bool IsAutoOperation => Enumerable.Contains(AutoOperationDtcs, base.UserMessageDtc);

		public override bool AutoForwardAllowed
		{
			get
			{
				if (DeviceStatus.CommandForwardAllowed(this) && base.AreAutoCommandsSupported)
				{
					return !HasWindSensorUserMessageDtcErrors();
				}
				return false;
			}
		}

		public override bool AutoReverseAllowed
		{
			get
			{
				if (DeviceStatus.CommandReverseAllowed(this) && base.AreAutoCommandsSupported)
				{
					return !HasWindSensorUserMessageDtcErrors();
				}
				return false;
			}
		}

		public LogicalDeviceRelayHBridgeMomentaryType2(ILogicalDeviceId logicalDeviceId, LogicalDeviceRelayHBridgeCapabilityType2 capability, ILogicalDeviceService service = null, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, capability, service, isFunctionClassChangeable)
		{
			AwningWindEventSettingPid = new LogicalDevicePidByte(this, Pid.SmartArmWindEventSetting.ConvertToPid(), LogicalDeviceSessionType.Diagnostic);
			AwningWindEventProtectionCountPid = new LogicalDevicePidUInt16(this, Pid.AwningAutoProtectionCount.ConvertToPid(), LogicalDeviceSessionType.Diagnostic);
			AutoOperationSafetyConfigPid = new LogicalDevicePidByte(this, Pid.HBridgeSafetyAlertConfig.ConvertToPid(), LogicalDeviceSessionType.Diagnostic);
			MomentaryHBridgeCircuitRolePid = new LogicalDevicePidByte(this, Pid.MomentaryHBridgeCircuitRole.ConvertToPid(), LogicalDeviceSessionType.Diagnostic);
		}

		protected override void DebugUpdateDeviceStatusChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, uint dataLength, string optionalText = "")
		{
			if (LogicalDeviceRelayStatusType2<RelayHBridgeDirection>.IsSignificantlyDifferent(oldStatusData, statusData))
			{
				base.DebugUpdateDeviceStatusChanged(oldStatusData, statusData, dataLength, optionalText);
				_debugUpdateDeviceStatusChangedThrottleTimer.Restart();
				_debugUpdateDeviceStatusChangedThrottleCount = 1u;
			}
			else if (_debugUpdateDeviceStatusChangedThrottleCount != 0 && _debugUpdateDeviceStatusChangedThrottleTimer.ElapsedMilliseconds < 60000)
			{
				_debugUpdateDeviceStatusChangedThrottleCount++;
			}
			else
			{
				base.DebugUpdateDeviceStatusChanged(oldStatusData, statusData, dataLength, $" found {_debugUpdateDeviceStatusChangedThrottleCount} changes over {60000u}ms");
				_debugUpdateDeviceStatusChangedThrottleTimer.Restart();
				_debugUpdateDeviceStatusChangedThrottleCount = 1u;
			}
		}

		public void UpdateAlert(string alertName, bool isActive, int? count)
		{
			UpdateAlertDefaultImpl(alertName, isActive, count, _alertDict);
		}

		public void UpdateAlert(byte alertId, byte rawData)
		{
			if (alertId == 0)
			{
				bool isActive = (rawData & 0x80) == 128;
				int value = rawData & 0x7F;
				UpdateAlert("WindRetractEvent", isActive, value);
			}
		}

		public async Task<LogicalDeviceRelayHBridgeCircuitIdRole> TryGetAssociatedCircuitIdRoleAsync(CancellationToken cancellationToken)
		{
			try
			{
				return (LogicalDeviceRelayHBridgeCircuitIdRole)(await MomentaryHBridgeCircuitRolePid.ReadByteAsync(cancellationToken));
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("LogicalDeviceRelayHBridgeMomentaryType2", "Unable to get Circuit ID Role " + ex.Message);
				return LogicalDeviceRelayHBridgeCircuitIdRole.None;
			}
		}

		public async Task<bool> TrySetAssociatedCircuitIdRoleAsync(LogicalDeviceRelayHBridgeCircuitIdRole circuitRole, CancellationToken cancellationToken)
		{
			try
			{
				await MomentaryHBridgeCircuitRolePid.WriteByteAsync((byte)circuitRole, cancellationToken);
				return true;
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("LogicalDeviceRelayHBridgeMomentaryType2", "Unable to set Circuit ID Role " + ex.Message);
				return false;
			}
		}

		public ILogicalDeviceLight? GetAssociatedLight()
		{
			List<ILogicalDeviceLight> list = DeviceService.DeviceManager?.FindLogicalDevices((ILogicalDeviceLight ld) => GetLightAssociation(ld) == DeviceAssociation.AssociatedToSelf);
			list?.Sort(delegate(ILogicalDeviceLight light1, ILogicalDeviceLight light2)
			{
				if ((object)light1.LogicalId.ProductMacAddress == null && (object)light2.LogicalId.ProductMacAddress == null)
				{
					return 0;
				}
				if ((object)light1.LogicalId.ProductMacAddress == null)
				{
					return 1;
				}
				return ((object)light2.LogicalId.ProductMacAddress == null) ? (-1) : light1.LogicalId.ProductMacAddress!.CompareTo(light2.LogicalId.ProductMacAddress);
			});
			if (list != null && list.Count > 1)
			{
				TaggedLog.Warning("LogicalDeviceRelayHBridgeMomentaryType2", $"{list.Count} associated lights found for momentary relay, only 1 is expected.");
			}
			if (list == null)
			{
				return null;
			}
			return Enumerable.FirstOrDefault(list);
		}

		public DeviceAssociation GetLightAssociation(ILogicalDeviceLight light)
		{
			if (light.ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				return DeviceAssociation.Unknown;
			}
			if (!(light is ILogicalDeviceLatchingRelayLight) || (object)light.LogicalId.ProductMacAddress == null || light.LogicalId.ProductMacAddress != base.LogicalId.ProductMacAddress)
			{
				return DeviceAssociation.NotSupported;
			}
			if (!light.CircuitId.HasValueBeenLoaded)
			{
				return DeviceAssociation.Supported;
			}
			if ((uint)light.CircuitId.Value == (uint)base.CircuitId.Value)
			{
				return DeviceAssociation.AssociatedToSelf;
			}
			return DeviceAssociation.AssociatedToOther;
		}

		public ILogicalDeviceAwningSensor? GetAssociatedAwningSensor()
		{
			List<ILogicalDeviceAwningSensor> list = DeviceService.DeviceManager?.FindLogicalDevices((ILogicalDeviceAwningSensor ld) => GetAwningSensorAssociation(ld) == DeviceAssociation.AssociatedToSelf);
			list?.Sort(delegate(ILogicalDeviceAwningSensor awningSensor1, ILogicalDeviceAwningSensor awningSensor2)
			{
				if ((object)awningSensor1.LogicalId.ProductMacAddress == null && (object)awningSensor2.LogicalId.ProductMacAddress == null)
				{
					return 0;
				}
				if ((object)awningSensor1.LogicalId.ProductMacAddress == null)
				{
					return 1;
				}
				return ((object)awningSensor2.LogicalId.ProductMacAddress == null) ? (-1) : awningSensor1.LogicalId.ProductMacAddress!.CompareTo(awningSensor2.LogicalId.ProductMacAddress);
			});
			if (list != null && list.Count > 1)
			{
				TaggedLog.Warning("LogicalDeviceRelayHBridgeMomentaryType2", $"{list.Count} associated awning sensors found for momentary relay, only 1 is expected.");
			}
			if (list == null)
			{
				return null;
			}
			return Enumerable.FirstOrDefault(list);
		}

		public DeviceAssociation GetAwningSensorAssociation(ILogicalDeviceAwningSensor awningSensor)
		{
			if (awningSensor == null)
			{
				return DeviceAssociation.Unknown;
			}
			uint num = awningSensor.TryGetLinkId();
			if (num == 0)
			{
				return DeviceAssociation.Unknown;
			}
			if (num == (uint)base.CircuitId.Value)
			{
				return DeviceAssociation.AssociatedToSelf;
			}
			return DeviceAssociation.AssociatedToOther;
		}

		public AwningProtectionState GetAwningAutoRetractProtectionState()
		{
			if (ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				return AwningProtectionState.Offline;
			}
			if (!base.DeviceCapability.IsAwningSensorSupported)
			{
				return AwningProtectionState.NotSupported;
			}
			if (DeviceStatus.WindProtectionLevel == AwningWindStrength.None)
			{
				return AwningProtectionState.WindProtectionOff;
			}
			if (DeviceStatus.WindProtectionLevel == AwningWindStrength.Unknown)
			{
				return AwningProtectionState.NotSupported;
			}
			ILogicalDeviceAwningSensor associatedAwningSensor = GetAssociatedAwningSensor();
			if (associatedAwningSensor == null)
			{
				return AwningProtectionState.NotConfigured;
			}
			if (associatedAwningSensor.ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				return AwningProtectionState.CommunicationError;
			}
			if (base.InTransitLockout == InTransitLockoutStatus.OnEnforced)
			{
				return AwningProtectionState.OperationSafetyLockout;
			}
			if (base.UserMessageDtc == DTC_ID.WIND_EVENT_AUTO_OPERATION_COMPLETE)
			{
				return AwningProtectionState.AutoProtected;
			}
			if (HasWindSensorUserMessageDtcErrors())
			{
				return AwningProtectionState.CommunicationError;
			}
			return AwningProtectionState.AutoProtectReady;
		}

		public AwningWindStrength GetAwningWindProtectionLevel()
		{
			return DeviceStatus.WindProtectionLevel;
		}

		public async Task<AwningWindStrength> TryGetAwningWindProtectionLevelAsync(CancellationToken cancellationToken)
		{
			if (ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				return AwningWindStrength.Unknown;
			}
			if (!base.DeviceCapability.IsAwningSensorSupported)
			{
				return AwningWindStrength.None;
			}
			try
			{
				byte b = await AwningWindEventSettingPid.ReadAsync(cancellationToken);
				switch (b)
				{
				case 0:
					return AwningWindStrength.None;
				case 1:
					return AwningWindStrength.Low;
				case 2:
					return AwningWindStrength.Medium;
				case 3:
					return AwningWindStrength.High;
				case byte.MaxValue:
					return AwningWindStrength.Unknown;
				default:
					TaggedLog.Warning("LogicalDeviceRelayHBridgeMomentaryType2", string.Format("{0} received undefined wind strength 0x{1:X}", "TryGetAwningWindProtectionLevelAsync", b));
					return AwningWindStrength.Unknown;
				}
			}
			catch (Exception arg)
			{
				TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", $"Exception getting wind event setting: {arg}");
				return AwningWindStrength.Unknown;
			}
		}

		public Task SetAwningAutoRetractWindProtectionLevelAsync(AwningWindStrength windSetting, CancellationToken cancellationToken)
		{
			return AwningWindEventSettingPid.WriteAsync((byte)windSetting, cancellationToken);
		}

		public override void OnDeviceCapabilityChanged()
		{
			base.OnDeviceCapabilityChanged();
			NotifyPropertyChanged("AutoForwardAllowed");
			NotifyPropertyChanged("AutoReverseAllowed");
		}

		public override void OnInTransitLockoutChanged()
		{
			base.OnInTransitLockoutChanged();
			NotifyPropertyChanged("AutoForwardAllowed");
			NotifyPropertyChanged("AutoReverseAllowed");
		}

		public override void OnDeviceStatusChanged()
		{
			base.OnDeviceStatusChanged();
			NotifyPropertyChanged("AutoForwardAllowed");
			NotifyPropertyChanged("AutoReverseAllowed");
			NotifyPropertyChanged("IsAutoOperationInProgress");
			NotifyPropertyChanged("IsWindSensorAutoOperationInProgress");
		}

		public override Task<CommandResult> TryAutoReverseAsync(CancellationToken cancellationToken)
		{
			return TryPerformAutoOperationAsync(RelayHBridgeDirection.Reverse, cancellationToken);
		}

		public override Task<CommandResult> TryAutoForwardAsync(CancellationToken cancellationToken)
		{
			return TryPerformAutoOperationAsync(RelayHBridgeDirection.Forward, cancellationToken);
		}

		private async Task<CommandResult> TryPerformAutoOperationAsync(RelayHBridgeDirection requestedDirection, CancellationToken cancellationToken)
		{
			int commandRetries = 0;
			bool moving = false;
			int operationRetries = 0;
			bool inProgress = false;
			if ((requestedDirection == RelayHBridgeDirection.Forward && !AutoForwardAllowed) || (requestedDirection == RelayHBridgeDirection.Reverse && !AutoReverseAllowed))
			{
				TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync failed because requested direction was not allowed.");
				return CommandResult.ErrorCommandNotAllowed;
			}
			try
			{
				await ActivateSession(cancellationToken);
			}
			catch
			{
				TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync failed to activate a session.");
				return CommandResult.ErrorNoSession;
			}
			CancellationTokenSource exclusiveOperationCts = new CancellationTokenSource();
			CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, exclusiveOperationCts.Token);
			CancellationToken linkedToken = cancellationTokenSource.Token;
			LogicalDeviceExclusiveOperation exclusiveOperation = DeviceService.GetExclusiveOperation<IRelayAutoOperations>();
			IDisposable startedExclusiveOperation = exclusiveOperation.Start(delegate
			{
				exclusiveOperationCts.TryCancelAndDispose();
			});
			if (startedExclusiveOperation == null)
			{
				TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync failed to start an exclusive operation.");
				return CommandResult.ErrorOther;
			}
			try
			{
				int num;
				_ = num - 1;
				_ = 2;
				CommandResult result;
				try
				{
					while (true)
					{
						if (!linkedToken.IsCancellationRequested)
						{
							if (ActiveConnection == LogicalDeviceActiveConnection.Remote || ActiveConnection == LogicalDeviceActiveConnection.Cloud)
							{
								TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync failed because ActiveConnection is remote.");
								result = CommandResult.ErrorRemoteOperationNotSupported;
								break;
							}
							if (ActiveConnection == LogicalDeviceActiveConnection.Offline)
							{
								TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync failed because ActiveConnection is offline.");
								result = CommandResult.ErrorDeviceOffline;
								break;
							}
							HBridgeCommand hBridgeCommand = ((requestedDirection == RelayHBridgeDirection.Forward) ? HBridgeCommand.AutoForward : HBridgeCommand.AutoReverse);
							CommandDirectionLastSetTimer.Restart();
							if (linkedToken.IsCancellationRequested)
							{
								TaggedLog.Information("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync failed because a cancellation was requested.");
								result = CommandResult.Canceled;
								break;
							}
							CommandResult commandResult = await SendDirectionCommand(hBridgeCommand, linkedToken);
							switch (commandResult)
							{
							case CommandResult.Completed:
								commandRetries = 0;
								if (!(await TaskExtension.TryDelay(100, linkedToken)))
								{
									TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync failed because TryDelay failed.");
									result = CommandResult.Canceled;
								}
								else if (!moving)
								{
									if (base.RelayEnergized.ConvertToDirection(base.LogicalId) == requestedDirection)
									{
										operationRetries = 0;
										moving = true;
										continue;
									}
									int num2 = operationRetries + 1;
									operationRetries = num2;
									if (num2 <= 10)
									{
										continue;
									}
									TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync failed because MaxConsecutiveOperationRetries was reached and relay was not moving yet.");
									result = CommandResult.ErrorAssumed;
								}
								else if (!inProgress)
								{
									if (base.UserMessageDtc == DTC_ID.USER_AUTO_OPERATION_IN_PROGRESS)
									{
										operationRetries = 0;
										inProgress = true;
										continue;
									}
									int num2 = operationRetries + 1;
									operationRetries = num2;
									if (num2 <= 10)
									{
										continue;
									}
									TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync failed because MaxConsecutiveOperationRetries was reached and auto operation was not in progress yet.");
									result = CommandResult.ErrorAssumed;
								}
								else if (base.UserMessageDtc == DTC_ID.USER_AUTO_OPERATION_ERROR)
								{
									TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync failed because UserMessageDtc was USER_AUTO_OPERATION_ERROR.");
									result = CommandResult.ErrorAssumed;
								}
								else if (base.UserMessageDtc == DTC_ID.USER_AUTO_OPERATION_COMPLETE)
								{
									TaggedLog.Information("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync finished with UserMessageDtc USER_AUTO_OPERATION_COMPLETE.");
									result = CommandResult.Completed;
								}
								else
								{
									if (base.UserMessageDtc == DTC_ID.USER_AUTO_OPERATION_IN_PROGRESS || base.UserMessageDtc == DTC_ID.USER_AUTO_OPERATION_ERROR || base.UserMessageDtc == DTC_ID.USER_AUTO_OPERATION_COMPLETE)
									{
										continue;
									}
									TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", $"TryPerformAutoOperationAsync failed because UserMessageDtc was an unexpected value: {base.UserMessageDtc}.");
									result = CommandResult.ErrorAssumed;
								}
								break;
							case CommandResult.Canceled:
								TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync failed because SendDirectionCommand result was canceled.");
								result = commandResult;
								break;
							default:
							{
								int num2 = commandRetries + 1;
								commandRetries = num2;
								if (num2 > 4)
								{
									TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", $"TryPerformAutoOperationAsync failed because MaxConsecutiveCommandRetries was reached, commandResult: {commandResult}.");
									result = commandResult;
									break;
								}
								if (await TaskExtension.TryDelay(500, linkedToken))
								{
									continue;
								}
								TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync failed because TryDelay failed.");
								result = CommandResult.Canceled;
								break;
							}
							}
						}
						else
						{
							TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", "TryPerformAutoOperationAsync failed because the operation was canceled.");
							result = CommandResult.Canceled;
						}
						break;
					}
				}
				catch (Exception ex)
				{
					TaggedLog.Error("LogicalDeviceRelayHBridgeMomentaryType2", $"Unable to complete {requestedDirection} auto-operation for {DeviceName} - {ex.GetType().Name}: {ex.Message}");
					result = CommandResult.ErrorOther;
				}
				return result;
			}
			finally
			{
				for (int i = 0; i < 5; i++)
				{
					if (base.RelayEnergized.ConvertToDirection(base.LogicalId) == RelayHBridgeDirection.Stop)
					{
						break;
					}
					await SendDirectionCommand(HBridgeCommand.Stop, CancellationToken.None);
					await Task.Delay(100, CancellationToken.None);
				}
				DeactivateSession();
				startedExclusiveOperation?.Dispose();
			}
		}

		private bool HasWindSensorUserMessageDtcErrors()
		{
			if (!base.DeviceCapability.IsAwningSensorSupported)
			{
				return false;
			}
			DTC_ID userMessageDtc = base.UserMessageDtc;
			if (userMessageDtc - 1738 <= DTC_ID.TOUCH_PAD_COMM_FAILURE || userMessageDtc - 1833 <= DTC_ID.TOUCH_PAD_COMM_FAILURE)
			{
				return true;
			}
			return false;
		}
	}
}
