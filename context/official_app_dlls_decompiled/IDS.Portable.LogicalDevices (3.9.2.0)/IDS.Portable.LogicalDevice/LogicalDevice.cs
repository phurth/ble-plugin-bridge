using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice.LogicalDeviceSource;
using IDS.Portable.LogicalDevices.Extensions;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDevice<TCapability> : CommonDisposableNotifyPropertyChanged, ILogicalDeviceWithCapability<TCapability>, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged where TCapability : ILogicalDeviceCapability
	{
		private const string LogTag = "LogicalDevice";

		protected readonly CompositeDisposable Disposables = new CompositeDisposable();

		private NETWORK_STATUS _lastReceivedNetworkStatus = (byte)0;

		private ILogicalDevicePidEnum<PidLockoutType> _lockoutTypePid;

		private InTransitLockoutStatus _lastKnowInTransitLockout;

		private readonly LogicalDeviceSnapshotMetaData _customSnapshotData = new LogicalDeviceSnapshotMetaData();

		protected Dictionary<Type, ILogicalDeviceEx> LogicalDeviceExDict = new Dictionary<Type, ILogicalDeviceEx>();

		protected IRemoteChannelCollection RemoteChannels = new RemoteChannelCollection();

		public ILogicalDeviceId LogicalId { get; }

		public virtual ILogicalDeviceService DeviceService { get; }

		public virtual bool IsRemoteAccessAvailable => false;

		public ILogicalDeviceProduct? Product => DeviceService?.ProductManager?.FindProduct(LogicalId.ProductId, LogicalId.ProductMacAddress);

		public ILogicalDeviceCircuitId CircuitId { get; }

		public ConcurrentDictionary<string, object> CustomData { get; } = new ConcurrentDictionary<string, object>();


		public string ImmutableUniqueId { get; }

		public TCapability DeviceCapability { get; }

		public ILogicalDeviceCapability DeviceCapabilityBasic => DeviceCapability;

		public virtual string DeviceName => DeviceService.MakeDeviceName(this);

		public virtual string DeviceNameShort => DeviceService.MakeDeviceNameShort(this);

		public virtual string DeviceNameShortAbbreviated => DeviceService.MakeDeviceNameShortAbbreviated(this);

		public virtual LogicalDeviceActiveConnection ActiveConnection
		{
			get
			{
				if (base.IsDisposed)
				{
					return LogicalDeviceActiveConnection.Offline;
				}
				ILogicalDeviceSourceDirect? obj = DeviceService?.GetPrimaryDeviceSourceDirect(this);
				bool flag = obj?.IsLogicalDeviceOnline(this) ?? false;
				if (obj is ILogicalDeviceSourceCloudConnection && flag)
				{
					return LogicalDeviceActiveConnection.Cloud;
				}
				if (flag)
				{
					return LogicalDeviceActiveConnection.Direct;
				}
				if (IsRemoteAccessAvailable)
				{
					return LogicalDeviceActiveConnection.Remote;
				}
				return LogicalDeviceActiveConnection.Offline;
			}
		}

		public virtual bool ActiveSession
		{
			get
			{
				try
				{
					if (base.IsDisposed)
					{
						return false;
					}
					return SessionManager.IsSessionActive(LogicalDeviceSessionType.RemoteControl, this);
				}
				catch (SessionManagerNotAvailableException)
				{
					return false;
				}
				catch (Exception ex2)
				{
					TaggedLog.Debug("LogicalDevice", "ActiveSession unexpected exception " + ex2.Message);
					return false;
				}
			}
		}

		public ILogicalDeviceSessionManager SessionManager => (DeviceService?.GetPrimaryDeviceSourceDirect(this) as ILogicalDeviceSourceDirectConnection)?.SessionManager ?? throw new SessionManagerNotAvailableException("LogicalDevice", this);

		public NETWORK_STATUS LastReceivedNetworkStatus => _lastReceivedNetworkStatus;

		public IDS_CAN_VERSION_NUMBER CanVersion { get; private set; } = IDS_CAN_VERSION_NUMBER.UNKNOWN;


		public InTransitLockoutStatus InTransitLockout
		{
			get
			{
				switch (ActiveConnection)
				{
				case LogicalDeviceActiveConnection.Offline:
					return InTransitLockoutStatus.Unknown;
				case LogicalDeviceActiveConnection.Remote:
					if (!(this is ILogicalDeviceRemote logicalDeviceRemote))
					{
						return InTransitLockoutStatus.Unknown;
					}
					return logicalDeviceRemote.RemoteOnlineChannel?.RemoteOnlineStatus switch
					{
						RemoteOnlineStatus.Online => InTransitLockoutStatus.Off, 
						RemoteOnlineStatus.Locked => InTransitLockoutStatus.OnRemote, 
						_ => InTransitLockoutStatus.Unknown, 
					};
				case LogicalDeviceActiveConnection.Direct:
				case LogicalDeviceActiveConnection.Cloud:
					switch ((byte)EffectiveInMotionLockoutLevel)
					{
					case 0:
						return InTransitLockoutStatus.Off;
					case 1:
						switch (HazardousDuringInTransitLockout)
						{
						case HazardousStatus.Safe:
						{
							ILogicalDeviceService deviceService2 = DeviceService;
							if (deviceService2 == null || !deviceService2.Options.HasFlag(LogicalDeviceServiceOptions.AllowHazardousOperationAtLockoutLevel1))
							{
								return InTransitLockoutStatus.OnSomeOperationsAllowed;
							}
							return InTransitLockoutStatus.OnIgnored;
						}
						case HazardousStatus.HazardousDuringLockout:
						{
							ILogicalDeviceService deviceService = DeviceService;
							if (deviceService == null || !deviceService.Options.HasFlag(LogicalDeviceServiceOptions.AllowHazardousOperationAtLockoutLevel1))
							{
								return InTransitLockoutStatus.OnEnforced;
							}
							return InTransitLockoutStatus.OnIgnored;
						}
						default:
							return InTransitLockoutStatus.Unknown;
						}
					case 2:
					case 3:
						return HazardousDuringInTransitLockout switch
						{
							HazardousStatus.Safe => InTransitLockoutStatus.OnSomeOperationsAllowed, 
							HazardousStatus.HazardousDuringLockout => InTransitLockoutStatus.OnEnforced, 
							_ => InTransitLockoutStatus.Unknown, 
						};
					default:
						return InTransitLockoutStatus.Unknown;
					}
				default:
					TaggedLog.Error("LogicalDevice", $"InTransitLockout invalid ActiveConnection of {ActiveConnection}");
					return InTransitLockoutStatus.Unknown;
				}
			}
		}

		private IN_MOTION_LOCKOUT_LEVEL EffectiveInMotionLockoutLevel
		{
			get
			{
				switch (ActiveConnection)
				{
				case LogicalDeviceActiveConnection.Offline:
					return (byte)0;
				case LogicalDeviceActiveConnection.Direct:
				case LogicalDeviceActiveConnection.Cloud:
				{
					IN_MOTION_LOCKOUT_LEVEL iN_MOTION_LOCKOUT_LEVEL = DeviceService.GetPrimaryDeviceSourceDirect(this)?.GetLogicalDeviceInTransitLockoutLevel(this) ?? ((IN_MOTION_LOCKOUT_LEVEL)(byte)0);
					IN_MOTION_LOCKOUT_LEVEL inMotionLockoutLevel = DeviceService.InMotionLockoutLevel;
					IN_MOTION_LOCKOUT_LEVEL iN_MOTION_LOCKOUT_LEVEL2 = (CanVersion.IsInMotionLockoutSupported() ? iN_MOTION_LOCKOUT_LEVEL : inMotionLockoutLevel);
					return Math.Max(inMotionLockoutLevel, iN_MOTION_LOCKOUT_LEVEL2);
				}
				case LogicalDeviceActiveConnection.Remote:
					return (byte)(InTransitLockout.IsInLockout() ? 3 : 0);
				default:
					return (byte)0;
				}
			}
		}

		public virtual bool ShouldAutoClearInTransitLockout
		{
			get
			{
				if (ActiveConnection != LogicalDeviceActiveConnection.Direct)
				{
					return false;
				}
				ILogicalDeviceService deviceService = DeviceService;
				if (deviceService == null || !deviceService.Options.HasFlag(LogicalDeviceServiceOptions.AutoInTransitClear))
				{
					return false;
				}
				return (byte)EffectiveInMotionLockoutLevel != 0;
			}
		}

		protected HazardousStatus HazardousDuringInTransitLockout
		{
			get
			{
				switch (ActiveConnection)
				{
				case LogicalDeviceActiveConnection.Offline:
					return HazardousStatus.Unknown;
				case LogicalDeviceActiveConnection.Remote:
					if (!(this is ILogicalDeviceRemote logicalDeviceRemote))
					{
						return HazardousStatus.Unknown;
					}
					return logicalDeviceRemote.RemoteOnlineChannel?.RemoteOnlineStatus switch
					{
						RemoteOnlineStatus.Online => HazardousStatus.Safe, 
						RemoteOnlineStatus.Locked => HazardousStatus.HazardousDuringLockout, 
						_ => HazardousStatus.Unknown, 
					};
				case LogicalDeviceActiveConnection.Direct:
				case LogicalDeviceActiveConnection.Cloud:
				{
					ILogicalDeviceSourceDirect? primaryDeviceSourceDirect = DeviceService.GetPrimaryDeviceSourceDirect(this);
					if (primaryDeviceSourceDirect == null || !primaryDeviceSourceDirect!.IsLogicalDeviceHazardous(this))
					{
						return HazardousStatus.Safe;
					}
					return HazardousStatus.HazardousDuringLockout;
				}
				default:
					TaggedLog.Error("LogicalDevice", $"Hazardous invalid ActiveConnection of {ActiveConnection}");
					return HazardousStatus.Unknown;
				}
			}
		}

		public virtual bool IsLegacyDeviceHazardous => true;

		public virtual bool AllowHazardousOperationAtLockoutLevel1
		{
			get
			{
				ILogicalDeviceService deviceService = DeviceService;
				if (deviceService == null)
				{
					return false;
				}
				return deviceService.Options.HasFlag(LogicalDeviceServiceOptions.AllowHazardousOperationAtLockoutLevel1);
			}
		}

		public bool IsFunctionClassChangeable { get; }

		public LogicalDeviceSnapshotMetaDataReadOnly CustomSnapshotData => _customSnapshotData.ToReadOnly();

		public virtual Version ProtocolVersion
		{
			get
			{
				try
				{
					if (!(DeviceService?.GetPrimaryDeviceSourceDirect(this) is ILogicalDeviceSourceDirectMetadata logicalDeviceSourceDirectMetadata))
					{
						throw new LogicalDeviceSourceDirectException("Device isn't associated with direct manager ILogicalDeviceSourceDirectMetadata");
					}
					_customSnapshotData.ProtocolVersion = logicalDeviceSourceDirectMetadata.GetDeviceProtocolVersion(this) ?? LogicalDeviceConstant.VersionUnknown;
					DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
					return _customSnapshotData.ProtocolVersion;
				}
				catch (Exception ex)
				{
					if (_customSnapshotData.ProtocolVersion != null)
					{
						return _customSnapshotData.ProtocolVersion;
					}
					TaggedLog.Information("LogicalDevice", "Unable to get software part number " + ex.Message);
					return LogicalDeviceConstant.VersionUnknown;
				}
			}
		}

		public string? CustomDeviceName
		{
			get
			{
				return _customSnapshotData.CustomDeviceName;
			}
			set
			{
				if (!(value == _customSnapshotData.CustomDeviceName))
				{
					_customSnapshotData.CustomDeviceName = value;
					NotifyPropertyChanged("CustomDeviceName");
					NotifyPropertyChanged("DeviceName");
					DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
				}
			}
		}

		public string? CustomDeviceNameShort
		{
			get
			{
				return _customSnapshotData.CustomDeviceNameShort;
			}
			set
			{
				if (!(value == _customSnapshotData.CustomDeviceNameShort))
				{
					_customSnapshotData.CustomDeviceNameShort = value;
					NotifyPropertyChanged("CustomDeviceNameShort");
					NotifyPropertyChanged("DeviceNameShort");
					DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
				}
			}
		}

		public string? CustomDeviceNameShortAbbreviated
		{
			get
			{
				return _customSnapshotData.CustomDeviceNameShortAbbreviated;
			}
			set
			{
				if (!(value == _customSnapshotData.CustomDeviceNameShortAbbreviated))
				{
					_customSnapshotData.CustomDeviceNameShortAbbreviated = value;
					NotifyPropertyChanged("CustomDeviceNameShortAbbreviated");
					NotifyPropertyChanged("DeviceNameShortAbbreviated");
					DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
				}
			}
		}

		public event LogicalDeviceChangedEventHandler? DeviceCapabilityChanged;

		public LogicalDevice(ILogicalDeviceId logicalDeviceId, TCapability deviceCapability, ILogicalDeviceService deviceService, bool isFunctionClassChangeable = false)
		{
			ImmutableUniqueId = logicalDeviceId.MakeImmutableUniqueId(isFunctionClassChangeable);
			LogicalId = logicalDeviceId;
			DeviceCapability = deviceCapability;
			DeviceService = deviceService;
			IsFunctionClassChangeable = isFunctionClassChangeable;
			TCapability deviceCapability2 = DeviceCapability;
			deviceCapability2.DeviceCapabilityChangedEvent += OnDeviceCapabilityChanged;
			CircuitId = new LogicalDeviceCircuitId(this);
			CircuitId.PropertyChanged += CircuitIdOnPropertyChanged;
			_lockoutTypePid = new LogicalDevicePidEnum<PidLockoutType>(this, Pid.InMotionLockoutBehavior.ConvertToPid(), LogicalDeviceSessionType.Diagnostic);
		}

		~LogicalDevice()
		{
			Dispose();
		}

		public virtual void UpdateDeviceOnline(bool online)
		{
			OnDeviceOnlineChanged();
		}

		public void UpdateDeviceOnline()
		{
			OnDeviceOnlineChanged();
		}

		public virtual void OnDeviceOnlineChanged()
		{
			UpdateInTransitLockout();
			PerformLogicalDeviceExAction(delegate(ILogicalDeviceExOnline logicalDeviceEx)
			{
				logicalDeviceEx.LogicalDeviceOnlineChanged(this);
			});
			NotifyPropertyChanged("DeviceName");
			NotifyPropertyChanged("DeviceNameShort");
			NotifyPropertyChanged("DeviceNameShortAbbreviated");
			NotifyPropertyChanged("ActiveConnection");
			NotifyPropertyChanged("ActiveSession");
		}

		public void UpdateCircuitId(CIRCUIT_ID circuitId)
		{
			if (!base.IsDisposed && CircuitId != null && ((uint)CircuitId.Value != (uint)circuitId || !CircuitId.HasValueBeenLoaded))
			{
				TaggedLog.Information("LogicalDevice", $"Update circuitId to {circuitId} for {this}");
				CircuitId.UpdateValue(circuitId);
			}
		}

		private void CircuitIdOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			if (propertyChangedEventArgs.PropertyName == "Value")
			{
				OnCircuitIdChanged();
			}
			NotifyPropertyChanged("CircuitId");
		}

		public virtual void OnCircuitIdChanged()
		{
			MainThread.RequestMainThreadAction(delegate
			{
				DeviceService?.DeviceManager?.ContainerDataSourceNotify(this, new LogicalDeviceCircuitIdChangedEventArgs((uint)CircuitId.LastValue));
			});
			PerformLogicalDeviceExAction(delegate(ILogicalDeviceExCircuit logicalDeviceEx)
			{
				logicalDeviceEx.LogicalDeviceCircuitIdChanged(this);
			});
		}

		public void UpdateDeviceCapability(byte? rawDeviceCapability)
		{
			DeviceCapability.UpdateDeviceCapability(rawDeviceCapability);
		}

		public virtual void OnDeviceCapabilityChanged()
		{
			this.DeviceCapabilityChanged?.Invoke(this);
			MainThread.RequestMainThreadAction(delegate
			{
				DeviceService?.DeviceManager?.ContainerDataSourceNotify(this, new LogicalDeviceCapabilityChangedEventArgs());
			});
			PerformLogicalDeviceExAction(delegate(ILogicalDeviceExCapability logicalDeviceEx)
			{
				logicalDeviceEx.LogicalDeviceCapabilityChanged(this);
			});
			NotifyPropertyChanged("DeviceCapabilityBasic");
			NotifyPropertyChanged("DeviceCapability");
		}

		public override string ToString()
		{
			return $"{LogicalId}";
		}

		public void UpdateSessionChanged(SESSION_ID sessionId)
		{
			NotifyPropertyChanged("ActiveSession");
		}

		public void UpdateNetworkStatus(NETWORK_STATUS networkStatus)
		{
			if ((byte)_lastReceivedNetworkStatus != (byte)networkStatus)
			{
				NETWORK_STATUS lastReceivedNetworkStatus = _lastReceivedNetworkStatus;
				_lastReceivedNetworkStatus = networkStatus;
				UpdateInTransitLockout();
				OnNetworkStatusChanged(lastReceivedNetworkStatus, _lastReceivedNetworkStatus);
			}
		}

		public virtual void OnNetworkStatusChanged(NETWORK_STATUS oldNetworkStatus, NETWORK_STATUS newNetworkStatus)
		{
		}

		public void UpdateCanVersion(IDS_CAN_VERSION_NUMBER canVersion)
		{
			if (CanVersion != canVersion)
			{
				IDS_CAN_VERSION_NUMBER canVersion2 = CanVersion;
				CanVersion = canVersion;
				UpdateInTransitLockout();
				OnCanVersionChanged(canVersion2, canVersion);
			}
		}

		public virtual void OnCanVersionChanged(IDS_CAN_VERSION_NUMBER oldCanVersion, IDS_CAN_VERSION_NUMBER newCanVersion)
		{
		}

		public async Task<PidLockoutType> GetInMotionLockoutBehaviorAsync(CancellationToken cancellationToken)
		{
			PidLockoutType pidLockoutType = await _lockoutTypePid.ReadAsync(cancellationToken);
			if (Enum.IsDefined(typeof(PidLockoutType), (int)pidLockoutType))
			{
				return pidLockoutType;
			}
			throw new LogicalDeviceException($"InMotionLockoutBehavior Pid returned a value that is not defined, value: {pidLockoutType}");
		}

		public virtual void UpdateInTransitLockout()
		{
			InTransitLockoutStatus inTransitLockout = InTransitLockout;
			if (_lastKnowInTransitLockout != inTransitLockout)
			{
				_lastKnowInTransitLockout = inTransitLockout;
				OnInTransitLockoutChanged();
			}
			NotifyPropertyChanged("EffectiveInMotionLockoutLevel");
			NotifyPropertyChanged("InTransitLockout");
			NotifyPropertyChanged("HazardousDuringInTransitLockout");
			NotifyPropertyChanged("ShouldAutoClearInTransitLockout");
		}

		public virtual void OnInTransitLockoutChanged()
		{
			PerformLogicalDeviceExAction(delegate(ILogicalDeviceExInTransitLockout logicalDeviceEx)
			{
				logicalDeviceEx.LogicalDeviceInTransitLockoutChanged(this);
			});
		}

		public virtual bool Rename(FUNCTION_NAME newFunctionName, int newFunctionInstance)
		{
			try
			{
				string text = "";
				FUNCTION_CLASS preferredFunctionClass = LogicalId.DeviceType.GetPreferredFunctionClass(newFunctionName);
				if (LogicalId.FunctionName == newFunctionName && LogicalId.FunctionInstance == newFunctionInstance && LogicalId.FunctionClass == preferredFunctionClass)
				{
					TaggedLog.Debug("LogicalDevice", $"{this} Device is already named {newFunctionName}:{newFunctionInstance}");
					return true;
				}
				if (LogicalId.FunctionClass != preferredFunctionClass)
				{
					text = "FUNCTION_CLASS CHANGED";
					if (!IsFunctionClassChangeable)
					{
						TaggedLog.Debug("LogicalDevice", $"{this} Can't change function class from {LogicalId.FunctionClass} to {preferredFunctionClass} because FUNCTION_CLASS would change.");
						return false;
					}
				}
				ILogicalDeviceId logicalDeviceId = LogicalId.Clone();
				bool flag = LogicalId.Rename(newFunctionName, newFunctionInstance);
				TaggedLog.Debug("LogicalDevice", $"{this} Renamed device from {logicalDeviceId.FunctionName}:{logicalDeviceId.FunctionInstance} to {newFunctionName}:{newFunctionInstance} success={flag} {text}");
				NotifyPropertyChanged("DeviceName");
				NotifyPropertyChanged("DeviceNameShort");
				NotifyPropertyChanged("DeviceNameShortAbbreviated");
				NotifyPropertyChanged("LogicalId");
				if (flag)
				{
					DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
					OnLogicalIdChanged();
				}
				return flag;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDevice", $"{this} RenameLogicalDevice {ex.Message}");
				return false;
			}
		}

		public virtual void OnLogicalIdChanged()
		{
		}

		public async Task<bool> TryWaitForRenameAsync(FUNCTION_NAME functionName, int functionInstance, int timeoutMs, CancellationToken cancellationToken)
		{
			FUNCTION_NAME functionName2 = functionName;
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			try
			{
				base.PropertyChanged += NameChangeHandler;
				NameChangeHandler(this, new PropertyChangedEventArgs("LogicalId"));
				if (await Task.WhenAny(new Task[2]
				{
					tcs.Task,
					Task.Delay(timeoutMs, cancellationToken)
				}) != tcs.Task)
				{
					return false;
				}
				return tcs.Task.Result;
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("LogicalDevice", "Unable to wait for rename to complete: " + ex.Message);
				return false;
			}
			finally
			{
				try
				{
					base.PropertyChanged -= NameChangeHandler;
				}
				catch
				{
				}
			}
			void NameChangeHandler(object sender, PropertyChangedEventArgs eventArgs)
			{
				if (base.IsDisposed)
				{
					tcs.SetResult(false);
				}
				if (cancellationToken.IsCancellationRequested)
				{
					tcs.TrySetResult(false);
				}
				if (LogicalId.FunctionName == functionName2 && LogicalId.FunctionInstance == functionInstance)
				{
					tcs.TrySetResult(true);
				}
			}
		}

		public virtual IPidDetail GetPidDetail(Pid pid)
		{
			return Product?.GetPidDetail(pid) ?? pid.GetPidDetailDefault();
		}

		public UInt48? GetCachedPidRawValue(Pid pid)
		{
			if (!_customSnapshotData.PidValueSnapshotDict.TryGetValue(pid, out var logicalDevicePidSnapshot))
			{
				return null;
			}
			return logicalDevicePidSnapshot.Value;
		}

		[IteratorStateMachine(typeof(LogicalDevice<>._003CGetCachedPids_003Ed__89))]
		public IEnumerable<(Pid Pid, UInt48 Value)> GetCachedPids()
		{
			//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
			return new _003CGetCachedPids_003Ed__89(-2)
			{
				_003C_003E4__this = this
			};
		}

		public void SetCachedPidRawValue(Pid pid, UInt48 value)
		{
			_customSnapshotData.AddOrUpdatePidValue(pid, value);
			DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
		}

		public void CustomSnapshotDataUpdate(LogicalDeviceSnapshotMetaDataReadOnly snapshotData)
		{
			lock (_customSnapshotData)
			{
				_customSnapshotData.UpdateWithNewMetaData(snapshotData);
				DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
			}
		}

		public virtual async Task<string> GetSoftwarePartNumberAsync(CancellationToken cancelToken)
		{
			try
			{
				string text = await ((DeviceService?.GetPrimaryDeviceSourceDirect(this) as ILogicalDeviceSourceDirectMetadata) ?? throw new LogicalDeviceSourceDirectException("Device isn't associated with direct manager ILogicalDeviceSourceDirectMetadata")).GetSoftwarePartNumberAsync(this, cancelToken);
				SetCachedSnapshotSoftwarePartNumber(text);
				return text;
			}
			catch (Exception ex)
			{
				if (_customSnapshotData.SoftwarePartNumber != null)
				{
					return _customSnapshotData.SoftwarePartNumber;
				}
				TaggedLog.Information("LogicalDevice", $"Unable to get software part number {ex.Message} for {this}");
				return string.Empty;
			}
		}

		protected void SetCachedSnapshotSoftwarePartNumber(string softwarePartNumber)
		{
			if (!string.Equals(_customSnapshotData.SoftwarePartNumber, softwarePartNumber))
			{
				_customSnapshotData.SoftwarePartNumber = softwarePartNumber;
				DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
			}
		}

		public bool AddDeviceSource(ILogicalDeviceSource deviceSource)
		{
			_customSnapshotData.AddDeviceSource(deviceSource);
			DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
			return true;
		}

		public bool RemoveDeviceSource(ILogicalDeviceSource deviceSource)
		{
			foreach (ILogicalDeviceTag item in deviceSource.MakeDeviceSourceTags(this))
			{
				DeviceService.DeviceManager!.TagManager.RemoveTag(item, this);
			}
			bool result = _customSnapshotData.RemoveDeviceSourceToken(deviceSource.DeviceSourceToken);
			ILogicalDeviceManager? deviceManager = DeviceService.DeviceManager;
			if (deviceManager != null)
			{
				deviceManager!.ContainerDataSourceSync(batchRequest: true);
				return result;
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsAssociatedWithDeviceSource(string? deviceSourceToken)
		{
			if (deviceSourceToken != null)
			{
				return _customSnapshotData.HasDeviceSourceToken(deviceSourceToken);
			}
			return false;
		}

		public bool IsAssociatedWithDeviceSource(ILogicalDeviceSource deviceSource)
		{
			return IsAssociatedWithDeviceSource(deviceSource?.DeviceSourceToken);
		}

		public bool IsAssociatedWithDeviceSource(IEnumerable<ILogicalDeviceSource> deviceSources)
		{
			if (deviceSources == null)
			{
				return false;
			}
			foreach (ILogicalDeviceSource deviceSource in deviceSources)
			{
				if (_customSnapshotData.HasDeviceSourceToken(deviceSource.DeviceSourceToken))
				{
					return true;
				}
			}
			return false;
		}

		public bool IsAssociatedWithDeviceSourceToken(string deviceSourceToken)
		{
			return _customSnapshotData.HasDeviceSourceToken(deviceSourceToken);
		}

		public TLogicalDeviceEx? GetLogicalDeviceEx<TLogicalDeviceEx>() where TLogicalDeviceEx : class, ILogicalDeviceEx
		{
			lock (LogicalDeviceExDict)
			{
				LogicalDeviceExDict.TryGetValue(typeof(TLogicalDeviceEx), out var value);
				return value as TLogicalDeviceEx;
			}
		}

		public void AddLogicalDeviceEx(ILogicalDeviceEx logicalDeviceEx, bool replaceExisting = false)
		{
			lock (LogicalDeviceExDict)
			{
				if (logicalDeviceEx == null || base.IsDisposed)
				{
					return;
				}
				Type type = logicalDeviceEx.GetType();
				if (LogicalDeviceExDict.TryGetValue(type, out var _))
				{
					if (!replaceExisting)
					{
						return;
					}
					TaggedLog.Warning("LogicalDevice", $"Replacing existing LogicalDeviceEx with {logicalDeviceEx}");
					RemoveLogicalDeviceEx(logicalDeviceEx);
				}
				LogicalDeviceExDict[type] = logicalDeviceEx;
				logicalDeviceEx.LogicalDeviceAttached(this);
			}
		}

		public void RemoveLogicalDeviceEx(ILogicalDeviceEx logicalDeviceEx)
		{
			lock (LogicalDeviceExDict)
			{
				if (logicalDeviceEx != null)
				{
					Type type = logicalDeviceEx.GetType();
					if (LogicalDeviceExDict.ContainsKey(type))
					{
						LogicalDeviceExDict.Remove(type);
						logicalDeviceEx.LogicalDeviceDetached(this);
					}
				}
			}
		}

		protected void PerformLogicalDeviceExAction<TLogicalDeviceEx>(Action<TLogicalDeviceEx> action) where TLogicalDeviceEx : ILogicalDeviceEx
		{
			lock (LogicalDeviceExDict)
			{
				foreach (ILogicalDeviceEx value in LogicalDeviceExDict.Values)
				{
					try
					{
						if (value is TLogicalDeviceEx)
						{
							TLogicalDeviceEx obj = (TLogicalDeviceEx)value;
							action(obj);
						}
					}
					catch (Exception ex)
					{
						TaggedLog.Error("LogicalDevice", $"LogicalDeviceEx Action {value} Through Unexpected Extension: {ex.Message}");
					}
				}
			}
		}

		public virtual void SnapshotLoaded(LogicalDeviceSnapshot snapshot)
		{
			LogicalDeviceSnapshot snapshot2 = snapshot;
			PerformLogicalDeviceExAction(delegate(ILogicalDeviceExSnapshot logicalDeviceEx)
			{
				logicalDeviceEx.LogicalDeviceSnapshotLoaded(this, snapshot2);
			});
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Disposables.TryDispose();
			NotifyPropertyChanged("ActiveConnection");
			NotifyPropertyChanged("EffectiveInMotionLockoutLevel");
			NotifyPropertyChanged("ActiveSession");
			try
			{
				CircuitId.PropertyChanged -= CircuitIdOnPropertyChanged;
			}
			catch
			{
			}
			CircuitId?.TryDispose();
			lock (LogicalDeviceExDict)
			{
				foreach (ILogicalDeviceEx item in Enumerable.ToList(LogicalDeviceExDict.Values))
				{
					RemoveLogicalDeviceEx(item);
				}
				LogicalDeviceExDict.Clear();
			}
			this.DeviceCapabilityChanged = null;
			RemoteChannels?.TryClearAndDisposeChannels();
		}

		public int CompareTo(object obj)
		{
			if (obj != null && this == obj)
			{
				return 0;
			}
			if (obj == null)
			{
				return 1;
			}
			if (obj is ILogicalDevice logicalDevice)
			{
				return LogicalId.CompareTo(logicalDevice.LogicalId);
			}
			throw new ArgumentException("Object is not a ILogicalDevice");
		}

		public int CompareTo(ILogicalDevice obj)
		{
			if (obj != null && this == obj)
			{
				return 0;
			}
			if (obj == null)
			{
				return 1;
			}
			return LogicalId.CompareTo(obj.LogicalId);
		}

		public bool Equals(ILogicalDevice other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			return LogicalId.Equals(other.LogicalId);
		}

		public override int GetHashCode()
		{
			return LogicalId.GetHashCode();
		}

		public TRemoteChannelDef GetRemoteChannelForChannelId<TRemoteChannelDef>(string channelId) where TRemoteChannelDef : IRemoteChannelDef
		{
			return RemoteChannels.GetRemoteChannelForChannelId<TRemoteChannelDef>(channelId);
		}

		public virtual void UpdateRemoteAccessAvailable()
		{
			UpdateInTransitLockout();
			NotifyPropertyChanged("IsRemoteAccessAvailable");
			NotifyPropertyChanged("DeviceName");
			NotifyPropertyChanged("DeviceNameShort");
			NotifyPropertyChanged("DeviceNameShortAbbreviated");
			NotifyPropertyChanged("ActiveConnection");
		}
	}
	public class LogicalDevice<TDeviceStatus, TCapability> : LogicalDevice<TCapability>, ILogicalDeviceWithStatus<TDeviceStatus>, ILogicalDeviceWithStatus, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged, ILogicalDeviceWithStatusUpdate<TDeviceStatus> where TDeviceStatus : IDeviceDataPacketMutable where TCapability : ILogicalDeviceCapability
	{
		private const string LogTag = "LogicalDevice";

		private readonly TaskCompletionSource<bool> _tcsDeviceStatusHasData = new TaskCompletionSource<bool>();

		public virtual TDeviceStatus DeviceStatus { get; protected set; }

		public IDeviceDataPacketMutable RawDeviceStatus => DeviceStatus;

		public DateTime LastUpdatedTimestamp { get; private set; } = DateTime.MinValue;


		public event LogicalDeviceChangedEventHandler? DeviceStatusChanged;

		public LogicalDevice(ILogicalDeviceId logicalDeviceId, TDeviceStatus status, TCapability deviceCapability, ILogicalDeviceService deviceService, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, deviceCapability, deviceService, isFunctionClassChangeable)
		{
			DeviceStatus = status;
		}

		public override string ToString()
		{
			return $"{base.LogicalId} Status = {DeviceStatus}";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool UpdateDeviceStatus(IReadOnlyList<byte> statusData, uint dataLength)
		{
			return ((ILogicalDeviceWithStatus)this).UpdateDeviceStatusInternal(statusData, dataLength, DateTime.Now);
		}

		bool ILogicalDeviceWithStatus.UpdateDeviceStatusInternal(IReadOnlyList<byte> statusData, uint dataLength, DateTime timeUpdated)
		{
			bool result = false;
			try
			{
				byte[] data = DeviceStatus.Data;
				int matchingDataLength = DeviceStatus.Update(statusData, (int)dataLength);
				LastUpdatedTimestamp = timeUpdated;
				UpdateDeviceStatusCompleted(data, statusData, (int)dataLength, matchingDataLength);
				if (ShouldNotifyDeviceStatusChanged(data, statusData, (int)dataLength, matchingDataLength))
				{
					DebugUpdateDeviceStatusChanged(data, statusData, dataLength);
					NotifyPropertyChanged("DeviceStatus");
					NotifyPropertyChanged("RawDeviceStatus");
					OnDeviceStatusChanged();
					result = true;
				}
				if (DeviceStatus.HasData)
				{
					_tcsDeviceStatusHasData.TrySetResult(true);
					return result;
				}
				return result;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDevice", $"{this} - Exception updating status {ex}: {ex.StackTrace}");
				return result;
			}
		}

		public bool UpdateDeviceStatus(TDeviceStatus status)
		{
			if (base.IsDisposed || status == null || !status.HasData || status.Size == 0)
			{
				return false;
			}
			return UpdateDeviceStatus(status.Data, status.Size);
		}

		protected virtual void UpdateDeviceStatusCompleted(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, int dataLength, int matchingDataLength)
		{
		}

		protected virtual bool ShouldNotifyDeviceStatusChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, int dataLength, int matchingDataLength)
		{
			return dataLength != matchingDataLength;
		}

		protected virtual void DebugUpdateDeviceStatusChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, uint dataLength, string optionalText = "")
		{
			TaggedLog.Information("LogicalDevice", $"{this} - Status changed from {oldStatusData.DebugDump(0, (int)dataLength)} to {statusData.DebugDump(0, (int)dataLength)}{optionalText}");
		}

		public virtual void OnDeviceStatusChanged()
		{
			try
			{
				this.DeviceStatusChanged?.Invoke(this);
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("LogicalDevice", $"Error Invoking Status Changed {ex}\n{ex.StackTrace}");
			}
			try
			{
				PerformLogicalDeviceExAction(delegate(ILogicalDeviceExStatus logicalDeviceEx)
				{
					logicalDeviceEx.LogicalDeviceStatusChanged(this);
				});
			}
			catch (Exception ex2)
			{
				TaggedLog.Warning("LogicalDevice", $"Error Invoking Status Changed Extension {ex2}\n{ex2.StackTrace}");
			}
		}

		public async Task WaitForDeviceStatusToHaveDataAsync(int timeout, CancellationToken cancelToken)
		{
			if (ActiveConnection == LogicalDeviceActiveConnection.Offline)
			{
				throw new TimeoutException();
			}
			if (!DeviceStatus.HasData)
			{
				await _tcsDeviceStatusHasData.WaitAsync(cancelToken, timeout);
			}
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			this.DeviceStatusChanged = null;
		}

		protected void UpdateAlertDefaultImpl(string alertName, bool isActive, int? count, ConcurrentDictionary<string, ILogicalDeviceAlert> alertDict)
		{
			if (!alertDict.TryGetValue(alertName, out var existingAlert))
			{
				if (alertDict.TryAdd(alertName, new LogicalDeviceAlert(alertName, isActive, count)))
				{
					return;
				}
				TaggedLog.Debug("LogicalDevice", $"Unable to update alert as it was added by someone else {alertName} for {this}");
			}
			if (!count.HasValue || (isActive == existingAlert.IsActive && count == existingAlert.Count))
			{
				return;
			}
			LogicalDeviceAlert newAlert = new LogicalDeviceAlert(alertName, isActive, count);
			alertDict[alertName] = newAlert;
			if (newAlert.IsActive || newAlert.Count != existingAlert.Count.GetValueOrDefault())
			{
				TaggedLog.Information("LogicalDevice", $"Logical Device Notify Alert {existingAlert} to {newAlert} for {this}");
				PerformLogicalDeviceExAction(delegate(ILogicalDeviceExAlertChanged logicalDeviceEx)
				{
					logicalDeviceEx.LogicalDeviceAlertChanged(this, existingAlert, newAlert);
				});
			}
			DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
		}
	}
	public class LogicalDevice<TDeviceStatus, TDeviceStatusExtended, TCapability> : LogicalDevice<TDeviceStatus, TCapability>, ILogicalDeviceWithStatusExtended<TDeviceStatusExtended>, ILogicalDeviceWithStatusExtended, ILogicalDevice, IComparable, IEquatable<ILogicalDevice>, IComparable<ILogicalDevice>, ICommonDisposable, IDisposable, IDevicesCommon, INotifyPropertyChanged where TDeviceStatus : IDeviceDataPacketMutable where TDeviceStatusExtended : IDeviceDataPacketMutableExtended where TCapability : ILogicalDeviceCapability
	{
		private const string LogTag = "LogicalDevice";

		private readonly TaskCompletionSource<bool> _tcsDeviceStatusExtendedHasData = new TaskCompletionSource<bool>();

		public TDeviceStatusExtended DeviceStatusExtended { get; protected set; }

		public IDeviceDataPacketMutableExtended RawDeviceStatusExtended => DeviceStatusExtended;

		public event LogicalDeviceChangedEventHandler? DeviceStatusExtendedChanged;

		public LogicalDevice(ILogicalDeviceId logicalDeviceId, TDeviceStatus status, TDeviceStatusExtended statusExtended, TCapability deviceCapability, ILogicalDeviceService deviceService, bool isFunctionClassChangeable = false)
			: base(logicalDeviceId, status, deviceCapability, deviceService, isFunctionClassChangeable)
		{
			DeviceStatusExtended = statusExtended;
		}

		public override string ToString()
		{
			return $"{base.LogicalId} Status = {DeviceStatus} StatusExtended = {DeviceStatusExtended}";
		}

		public virtual Dictionary<byte, byte[]> CopyRawDeviceStatusExtendedAsDictionary()
		{
			return new Dictionary<byte, byte[]> { [RawDeviceStatusExtended.ExtendedByte] = RawDeviceStatusExtended.CopyCurrentData() };
		}

		public bool UpdateDeviceStatusExtended(TDeviceStatusExtended extendedStatus, DateTime? timeUpdated = null)
		{
			if (base.IsDisposed || extendedStatus == null || !extendedStatus.HasData || extendedStatus.Size == 0)
			{
				TaggedLog.Error("LogicalDevice", $"Extended status returned false {base.IsDisposed} {extendedStatus == null} {!extendedStatus.HasData} {extendedStatus.Size == 0}");
				return false;
			}
			return UpdateDeviceStatusExtended(extendedStatus.Data, extendedStatus.Size, extendedStatus.ExtendedByte, timeUpdated);
		}

		public bool UpdateDeviceStatusExtended(IReadOnlyDictionary<byte, byte[]> statusData, Dictionary<byte, DateTime>? timeUpdatedByExtendedData, bool updateOnlyIfNewer)
		{
			bool flag = false;
			foreach (KeyValuePair<byte, byte[]> statusDatum in statusData)
			{
				if (statusDatum.Value != null && statusDatum.Value.Length != 0)
				{
					DateTime? dateTime = timeUpdatedByExtendedData?.TryGetValue(statusDatum.Key);
					if (dateTime.HasValue && updateOnlyIfNewer && DeviceStatusExtended.HasData && DeviceStatusExtended.LastUpdatedTimestamp > dateTime)
					{
						TaggedLog.Information("LogicalDevice", $"Device Status Extended data not applied for [{statusDatum.Key}]/{statusDatum.Value.DebugDump()} because it's older then the current data in the buffer: {this}");
					}
					else
					{
						flag |= UpdateDeviceStatusExtended(statusDatum.Value, (uint)statusDatum.Value.Length, statusDatum.Key, dateTime);
					}
				}
			}
			return flag;
		}

		public virtual bool UpdateDeviceStatusExtended(IReadOnlyList<byte> statusExtendedData, uint dataLength, byte extendedByte, DateTime? timeUpdated = null)
		{
			bool num = UpdateDeviceStatusExtended(statusExtendedData, dataLength, extendedByte, DeviceStatusExtended, timeUpdated);
			if (num)
			{
				NotifyPropertyChanged("DeviceStatusExtended");
			}
			return num;
		}

		protected bool UpdateDeviceStatusExtended(IReadOnlyList<byte> statusExtendedData, uint dataLength, byte extendedByte, TDeviceStatusExtended statusExtended, DateTime? timeUpdated = null)
		{
			bool flag = false;
			try
			{
				byte[] data = statusExtended.Data;
				byte extendedByte2 = statusExtended.ExtendedByte;
				flag = statusExtended.Update(statusExtendedData, dataLength, extendedByte, timeUpdated);
				if (flag)
				{
					DebugUpdateDeviceStatusExtendedChanged(data, statusExtendedData, dataLength, extendedByte2, extendedByte);
					OnDeviceStatusExtendedChanged(statusExtended);
				}
				if (statusExtended.HasData)
				{
					_tcsDeviceStatusExtendedHasData.TrySetResult(true);
					return flag;
				}
				return flag;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("LogicalDevice", $"{this} - Exception updating status extended {ex}: {ex.StackTrace}");
				return flag;
			}
		}

		protected virtual void DebugUpdateDeviceStatusExtendedChanged(IReadOnlyList<byte> oldStatusData, IReadOnlyList<byte> statusData, uint dataLength, byte oldExtendedByte, byte extendedByte, string optionalText = "")
		{
			TaggedLog.Debug("LogicalDevice", $"{this} - Status Extended changed from ({oldExtendedByte}):{oldStatusData.DebugDump(0, (int)dataLength)} to ({extendedByte}):{statusData.DebugDump(0, (int)dataLength)}{optionalText}");
		}

		public virtual void OnDeviceStatusExtendedChanged(IDeviceDataPacketMutableExtended dataChanged)
		{
			this.DeviceStatusExtendedChanged?.Invoke(this);
		}

		public async Task WaitForDeviceStatusExtendedToHaveDataAsync(int timeout, CancellationToken cancelToken)
		{
			if (!DeviceStatus.HasData)
			{
				await _tcsDeviceStatusExtendedHasData.WaitAsync(cancelToken, timeout);
			}
		}

		public override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			this.DeviceStatusExtendedChanged = null;
		}
	}
}
