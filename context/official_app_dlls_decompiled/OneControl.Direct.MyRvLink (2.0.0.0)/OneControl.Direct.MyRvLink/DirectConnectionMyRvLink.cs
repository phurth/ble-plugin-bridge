using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.Collections;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using ids.portable.common;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using ids.portable.common.Metrics;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.BlockTransfer;
using IDS.Portable.LogicalDevice.FirmwareUpdate;
using IDS.Portable.LogicalDevice.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDeviceSource;
using OneControl.Devices;
using OneControl.Devices.AccessoryGateway;
using OneControl.Devices.BootLoader;
using OneControl.Devices.Leveler.Type5;
using OneControl.Devices.LightRgb;
using OneControl.Direct.MyRvLink.Cache;
using OneControl.Direct.MyRvLink.Events;

namespace OneControl.Direct.MyRvLink
{
	public abstract class DirectConnectionMyRvLink : ILogicalDeviceSourceDirectBlockTransfer, ILogicalDeviceSourceDirect, ILogicalDeviceSource, IBlockTransfer, IDirectCommandLeveler1, IDirectCommandLeveler3, IDirectCommandLeveler4, IDirectConnectionMyRvLink, ILogicalDeviceSourceDirectConnectionMyRvLink, ILogicalDeviceSourceDirectConnection, ILogicalDeviceSourceConnection, ILogicalDeviceSourceDirectAccessoryGateway, ILogicalDeviceSourceDirectPid, IDirectCommandClimateZone, IDirectCommandGeneratorGenie, IDirectCommandLeveler5, IDirectCommandLightDimmable, IDirectCommandLightRgb, ILogicalDeviceSourceDirectPidList, ILogicalDeviceSourceDirectMetadata, ILogicalDeviceSourceDirectDtc, ILogicalDeviceSourceDirectFirmwareUpdateDevice, IFirmwareUpdateDevice, IDirectConnectionMyRvLinkMetrics, IDirectCommandMovement, ILogicalDeviceSourceDirectRealTimeClock, ILogicalDeviceSourceDirectRemoveOfflineDevices, ILogicalDeviceSourceDirectRename, IDirectManagerMyRvLinkRvStatus, ILogicalDeviceSourceDirectVoltage, ILogicalDeviceSourceDirectSoftwareUpdateAuthorization, IDirectCommandSwitch, ILogicalDeviceSourceDirectSwitchMasterControllable
	{
		public class BlockWriteTimeTracker
		{
			public enum TrackId
			{
				None,
				ProgressAck,
				BufferCopy,
				UpdateAndSendCommand,
				WaitingForResponse,
				WaitingForResponsePollDelay,
				ProcessResponse,
				Finish
			}

			private readonly Dictionary<TrackId, Stopwatch> _timeTracking;

			private TrackId _currentlyTracking;

			private Stopwatch _totalTime;

			public BlockWriteTimeTracker()
			{
				_totalTime = Stopwatch.StartNew();
				_timeTracking = new Dictionary<TrackId, Stopwatch>();
				_currentlyTracking = TrackId.None;
				foreach (TrackId value in EnumExtensions.GetValues<TrackId>())
				{
					_timeTracking.Add(value, new Stopwatch());
				}
			}

			public void SwitchTrackingTo(TrackId track)
			{
				if (track != _currentlyTracking)
				{
					_timeTracking[_currentlyTracking].Stop();
					_currentlyTracking = track;
					_timeTracking[_currentlyTracking].Start();
				}
			}

			public void Stop()
			{
				_timeTracking[_currentlyTracking].Stop();
				_currentlyTracking = TrackId.None;
			}

			public override string ToString()
			{
				StringBuilder stringBuilder = new StringBuilder();
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler appendInterpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(23, 1, stringBuilder2);
				appendInterpolatedStringHandler.AppendLiteral("    TotalTime: ");
				appendInterpolatedStringHandler.AppendFormatted((float)_totalTime.ElapsedMilliseconds / 1000f, "F2");
				appendInterpolatedStringHandler.AppendLiteral(" seconds");
				stringBuilder3.AppendLine(ref appendInterpolatedStringHandler);
				foreach (TrackId value in EnumExtensions.GetValues<TrackId>())
				{
					stringBuilder2 = stringBuilder;
					StringBuilder stringBuilder4 = stringBuilder2;
					appendInterpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(16, 3, stringBuilder2);
					appendInterpolatedStringHandler.AppendLiteral("    ");
					appendInterpolatedStringHandler.AppendFormatted(value);
					appendInterpolatedStringHandler.AppendLiteral(": ");
					appendInterpolatedStringHandler.AppendFormatted((float)_timeTracking[value].ElapsedMilliseconds / 1000f, "F2");
					appendInterpolatedStringHandler.AppendLiteral(" seconds ");
					appendInterpolatedStringHandler.AppendFormatted((double)((float)_timeTracking[value].ElapsedMilliseconds / (float)_totalTime.ElapsedMilliseconds) * 100.0, "F2");
					appendInterpolatedStringHandler.AppendLiteral("%");
					stringBuilder4.AppendLine(ref appendInterpolatedStringHandler);
				}
				return stringBuilder.ToString();
			}
		}

		private const int MaxDataBlockSize = 128;

		private const int MillisecondsPerSecond = 1000;

		private const int ErrorDelayMilliseconds = 200;

		private const uint WriteFinishedAddressOffset = uint.MaxValue;

		private bool _firmwareUpdateInProgress;

		private const int CommandLeveler1ResendTimeoutMs = 1000;

		private byte[] _lastSentLeveler1CommandData = new byte[0];

		private (ushort CommandId, LogicalDeviceLevelerCommandType1 command, long SentTimestampMs) _lastSentLeveler1Command = (0, null, 0L);

		private const int Leveler3CommandTimeout = 2500;

		public const int Leveler4CommandTimeout = 2500;

		private const string LogTag = "DirectConnectionMyRvLink";

		public const byte DeviceIdUnknown = byte.MaxValue;

		private readonly object _lock = new object();

		private MyRvLinkVersionTracker? _versionTracker;

		private MyRvLinkDeviceTracker? _deviceTracker;

		private readonly TimeSpan _reloadDevicesCheckTime = TimeSpan.FromMilliseconds(10000.0);

		private readonly TimeSpan _takeDevicesOfflineCheckTime = TimeSpan.FromMilliseconds(4000.0);

		private readonly TimeSpan _failureTimeout = TimeSpan.FromMilliseconds(500.0);

		private Timer? _takeDevicesOfflineTimer;

		private DateTime? _realTimeClock;

		private MyRvLinkGatewayInformation? _gatewayInfo;

		protected bool IsStarted;

		public const int CommandQueueLimit = 20;

		public const int CommandTimeoutMs = 8000;

		public const int CommandTimeoutExtendedMs = 16000;

		public const int CommandCompletedCacheSize = 100;

		public const ushort CommandIdInvalid = 0;

		public const ushort CommandIdStart = 1;

		public const ushort CommandIdNoResponse = ushort.MaxValue;

		private readonly Dictionary<int, MyRvLinkCommandTracker> _commandActiveDict = new Dictionary<int, MyRvLinkCommandTracker>();

		private readonly FixedSizedConcurrentQueue<MyRvLinkCommandTracker> _commandCompletedQueue = new FixedSizedConcurrentQueue<MyRvLinkCommandTracker>(100);

		private readonly MyRvLinkCommandResponseSuccessNoResponse _responseSuccessNoResponse = new MyRvLinkCommandResponseSuccessNoResponse();

		private ushort _nextCommandId = 1;

		private ushort _currentCommandId = 1;

		private bool _isConnectionOpened;

		private const int Leveler5CommandTimeout = 2500;

		private readonly TaskSerialQueue _pidSerialQueue = new TaskSerialQueue(200);

		private const int PidOperationDelayMs = 100;

		private Stopwatch _pidLastOperationTimer = Stopwatch.StartNew();

		private readonly TaskSerialQueue _dtcSerialQueue = new TaskSerialQueue(100);

		private readonly Stopwatch _dtcThrottleStopwatch = new Stopwatch();

		private const int DtcThrottleTimeMs = 500;

		private readonly FrequencyMetrics _receivedEventMetrics = new FrequencyMetrics();

		public const int MaxWaitTimeForFlashToCompleteMs = 30000;

		public const int WaitTimeForFlashToCompleteMs = 1000;

		public const int WaitForRebootIntoBootLoaderDelayMs = 1000;

		public const int WaitForRebootIntoBootloaderAttempts = 20;

		public const int DefaultJumpToBootMs = 10000;

		private readonly Dictionary<MyRvLinkCommandType, FrequencyMetrics> _metricsForCommandSends = new Dictionary<MyRvLinkCommandType, FrequencyMetrics>();

		private readonly Dictionary<MyRvLinkCommandType, FrequencyMetrics> _metricsForCommandFailures = new Dictionary<MyRvLinkCommandType, FrequencyMetrics>();

		private readonly Dictionary<MyRvLinkEventType, FrequencyMetrics> _metricsForEvents = new Dictionary<MyRvLinkEventType, FrequencyMetrics>();

		private const int RelayMovementCommandTimeout = 2500;

		private float? _voltage;

		private float? _temperature;

		public string LogPrefix { get; }

		public ILogicalDeviceService DeviceService { get; }

		public DeviceTableIdCache DeviceTableIdCache { get; }

		public IReadOnlyList<ILogicalDeviceTag> ConnectionTagList { get; }

		public ConcurrentHashSet<ILogicalDeviceSourceCommandMonitor> CommandMonitors { get; } = new ConcurrentHashSet<ILogicalDeviceSourceCommandMonitor>();


		public DateTime? RealTimeClock
		{
			get
			{
				if (!IsConnected || !IsStarted)
				{
					return null;
				}
				return _realTimeClock;
			}
			internal set
			{
				_realTimeClock = value;
			}
		}

		public MyRvLinkGatewayInformation? GatewayInfo
		{
			get
			{
				return _gatewayInfo;
			}
			private set
			{
				lock (_lock)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
					if (value != null)
					{
						HasMinimumExpectedProtocolVersion = GatewayVersionSupportLevelExtension.IsMinimumRequiredVersion(value!.ProtocolVersionMajor);
						if (!HasMinimumExpectedProtocolVersion)
						{
							defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 3);
							defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
							defaultInterpolatedStringHandler.AppendLiteral(" Gateway Minimum Protocol Version is ");
							defaultInterpolatedStringHandler.AppendFormatted(MyRvLinkProtocolVersionMajor.Version5);
							defaultInterpolatedStringHandler.AppendLiteral(" but received ");
							defaultInterpolatedStringHandler.AppendFormatted(value!.ProtocolVersionMajor);
							TaggedLog.Error("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
							value = null;
						}
						else
						{
							MyRvLinkVersionTracker versionTracker = _versionTracker;
							if (versionTracker == null || !versionTracker.IsVersionSupported || !versionTracker.IsGatewayVersionValid(value))
							{
								value = null;
							}
						}
					}
					if (object.Equals(_gatewayInfo, value))
					{
						return;
					}
					if (_gatewayInfo != null)
					{
						_ = _gatewayInfo!.DeviceTableId;
						_ = _gatewayInfo!.DeviceTableCrc;
					}
					_gatewayInfo = value;
					if (value == null)
					{
						HasMinimumExpectedProtocolVersion = false;
						_deviceTracker?.TryDispose();
						return;
					}
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(14, 2);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" GatewayInfo: ");
					defaultInterpolatedStringHandler.AppendFormatted(value);
					TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
					_deviceTracker?.UpdateDeviceIdIfNeeded(value!.DeviceTableId, value!.DeviceTableCrc);
					if (_deviceTracker == null || _deviceTracker!.IsDisposed || _deviceTracker!.DeviceTableId != value!.DeviceTableId || _deviceTracker!.DeviceTableCrc != value!.DeviceTableCrc)
					{
						_deviceTracker?.TryDispose();
						_deviceTracker = new MyRvLinkDeviceTracker(this, value!.DeviceTableId, value!.DeviceTableCrc);
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 2);
						defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
						defaultInterpolatedStringHandler.AppendLiteral(" Created new Device Tracker ");
						defaultInterpolatedStringHandler.AppendFormatted(_deviceTracker);
						TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					_deviceTracker!.UpdateMetadataIfNeeded(value!.DeviceMetadataTableCrc);
				}
			}
		}

		public bool IsFirmwareVersionSupported
		{
			get
			{
				if (HasMinimumExpectedProtocolVersion)
				{
					return _versionTracker?.IsVersionSupported ?? false;
				}
				return false;
			}
		}

		public bool HasMinimumExpectedProtocolVersion { get; private set; }

		protected Func<int, IMyRvLinkCommand?> GetPendingCommand { get; }

		public IN_MOTION_LOCKOUT_LEVEL InTransitLockoutLevel => _deviceTracker?.CachedInMotionLockoutLevel ?? ((IN_MOTION_LOCKOUT_LEVEL)(byte)0);

		public ILogicalDeviceSessionManager? SessionManager { get; }

		public bool IsConnected
		{
			get
			{
				return _isConnectionOpened;
			}
			protected set
			{
				lock (_lock)
				{
					if (_isConnectionOpened == value)
					{
						return;
					}
					_isConnectionOpened = value;
					if (_isConnectionOpened)
					{
						lock (_lock)
						{
							_metricsForCommandSends.Clear();
							_metricsForCommandFailures.Clear();
							_metricsForEvents.Clear();
							_receivedEventMetrics.Clear();
						}
					}
					else
					{
						RealTimeClock = null;
					}
					AbortAllPendingCommands();
					TaggedLog.Debug("DirectConnectionMyRvLink", LogPrefix + " Connection Status Changed to " + (value ? "Opened" : "Closed") + ", cleared any pending commands");
				}
			}
		}

		public abstract string DeviceSourceToken { get; }

		public abstract IEnumerable<ILogicalDeviceTag> DeviceSourceTags { get; }

		public virtual bool AllowAutoOfflineLogicalDeviceRemoval => true;

		public virtual bool IsDeviceSourceActive => IsConnected;

		public DateTime GetRealTimeClockTime => RealTimeClock ?? DateTime.MinValue;

		public abstract event Action<ILogicalDeviceSourceDirectConnection>? DidConnectEvent;

		public abstract event Action<ILogicalDeviceSourceDirectConnection>? DidDisconnectEvent;

		public abstract event UpdateDeviceSourceReachabilityEventHandler? UpdateDeviceSourceReachabilityEvent;

		public async Task<IReadOnlyList<BlockTransferBlockId>> GetDeviceBlockListAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(" is offline.");
				throw new LogicalDeviceException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			(byte, byte)? myRvDeviceFromLogicalDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvDeviceFromLogicalDevice.HasValue)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
				defaultInterpolatedStringHandler.AppendLiteral("No matching RvLink device for logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			MyRvLinkCommandGetDeviceBlockList command = new MyRvLinkCommandGetDeviceBlockList(GetNextCommandId(), myRvDeviceFromLogicalDevice.Value.Item1, myRvDeviceFromLogicalDevice.Value.Item2);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
			if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
			{
				if (myRvLinkCommandResponse is MyRvLinkGetDeviceBlockListCommandResponse myRvLinkGetDeviceBlockListCommandResponse)
				{
					return myRvLinkGetDeviceBlockListCommandResponse.BlockIds;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to Get Block IDs from ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(": Unknown result");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			throw new MyRvLinkCommandResponseFailureException(failure);
		}

		public async Task<BlockTransferPropertyFlags> GetDeviceBlockPropertyFlagsAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(" is offline.");
				throw new LogicalDeviceException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			(byte, byte)? myRvDeviceFromLogicalDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvDeviceFromLogicalDevice.HasValue)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
				defaultInterpolatedStringHandler.AppendLiteral("No matching RvLink device for logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			MyRvLinkCommandGetDeviceBlockProperties command = new MyRvLinkCommandGetDeviceBlockProperties(GetNextCommandId(), myRvDeviceFromLogicalDevice.Value.Item1, myRvDeviceFromLogicalDevice.Value.Item2, blockId, BlockTransferPropertyId.Flags);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
			if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
			{
				if (myRvLinkCommandResponse is MyRvLinkGetDeviceBlockPropertyCommandResponse myRvLinkGetDeviceBlockPropertyCommandResponse)
				{
					return myRvLinkGetDeviceBlockPropertyCommandResponse.Flags;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to Get Block IDs from ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(": Unknown result");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			throw new MyRvLinkCommandResponseFailureException(failure);
		}

		public async Task<LogicalDeviceSessionType> GetDeviceBlockPropertyReadSessionIdAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(" is offline.");
				throw new LogicalDeviceException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			(byte, byte)? myRvDeviceFromLogicalDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvDeviceFromLogicalDevice.HasValue)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
				defaultInterpolatedStringHandler.AppendLiteral("No matching RvLink device for logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			MyRvLinkCommandGetDeviceBlockProperties command = new MyRvLinkCommandGetDeviceBlockProperties(GetNextCommandId(), myRvDeviceFromLogicalDevice.Value.Item1, myRvDeviceFromLogicalDevice.Value.Item2, blockId, BlockTransferPropertyId.ReadSessionId);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
			if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
			{
				if (myRvLinkCommandResponse is MyRvLinkGetDeviceBlockPropertyCommandResponse myRvLinkGetDeviceBlockPropertyCommandResponse)
				{
					return myRvLinkGetDeviceBlockPropertyCommandResponse.ReadSessionId;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to Get Block IDs from ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(": Unknown result");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			throw new MyRvLinkCommandResponseFailureException(failure);
		}

		public async Task<LogicalDeviceSessionType> GetDeviceBlockPropertyWriteSessionIdAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(" is offline.");
				throw new LogicalDeviceException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			(byte, byte)? myRvDeviceFromLogicalDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvDeviceFromLogicalDevice.HasValue)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
				defaultInterpolatedStringHandler.AppendLiteral("No matching RvLink device for logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			MyRvLinkCommandGetDeviceBlockProperties command = new MyRvLinkCommandGetDeviceBlockProperties(GetNextCommandId(), myRvDeviceFromLogicalDevice.Value.Item1, myRvDeviceFromLogicalDevice.Value.Item2, blockId, BlockTransferPropertyId.WriteSessionId);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
			if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
			{
				if (myRvLinkCommandResponse is MyRvLinkGetDeviceBlockPropertyCommandResponse myRvLinkGetDeviceBlockPropertyCommandResponse)
				{
					return myRvLinkGetDeviceBlockPropertyCommandResponse.WriteSessionId;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to Get Block IDs from ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(": Unknown result");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			throw new MyRvLinkCommandResponseFailureException(failure);
		}

		public async Task<ulong> GetDeviceBlockCapacityAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(" is offline.");
				throw new LogicalDeviceException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			(byte, byte)? myRvDeviceFromLogicalDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvDeviceFromLogicalDevice.HasValue)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
				defaultInterpolatedStringHandler.AppendLiteral("No matching RvLink device for logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			MyRvLinkCommandGetDeviceBlockProperties command = new MyRvLinkCommandGetDeviceBlockProperties(GetNextCommandId(), myRvDeviceFromLogicalDevice.Value.Item1, myRvDeviceFromLogicalDevice.Value.Item2, blockId, BlockTransferPropertyId.Capacity);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
			if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
			{
				if (myRvLinkCommandResponse is MyRvLinkGetDeviceBlockPropertyCommandResponse myRvLinkGetDeviceBlockPropertyCommandResponse)
				{
					return myRvLinkGetDeviceBlockPropertyCommandResponse.BlockCapacity;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to Get Block IDs from ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(": Unknown result");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			throw new MyRvLinkCommandResponseFailureException(failure);
		}

		public async Task<ulong> GetDeviceBlockSizeAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(" is offline.");
				throw new LogicalDeviceException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			(byte, byte)? myRvDeviceFromLogicalDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvDeviceFromLogicalDevice.HasValue)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
				defaultInterpolatedStringHandler.AppendLiteral("No matching RvLink device for logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			MyRvLinkCommandGetDeviceBlockProperties command = new MyRvLinkCommandGetDeviceBlockProperties(GetNextCommandId(), myRvDeviceFromLogicalDevice.Value.Item1, myRvDeviceFromLogicalDevice.Value.Item2, blockId, BlockTransferPropertyId.CurrentSize);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
			if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
			{
				if (myRvLinkCommandResponse is MyRvLinkGetDeviceBlockPropertyCommandResponse myRvLinkGetDeviceBlockPropertyCommandResponse)
				{
					return myRvLinkGetDeviceBlockPropertyCommandResponse.CurrentBlockSize;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to Get Block IDs from ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(": Unknown result");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			throw new MyRvLinkCommandResponseFailureException(failure);
		}

		public async Task<uint> GetDeviceBlockCrcAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, bool recalculate, CancellationToken cancellationToken)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(" is offline.");
				throw new LogicalDeviceException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			(byte, byte)? myRvDeviceFromLogicalDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvDeviceFromLogicalDevice.HasValue)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
				defaultInterpolatedStringHandler.AppendLiteral("No matching RvLink device for logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			ushort nextCommandId = GetNextCommandId();
			MyRvLinkCommandGetDeviceBlockProperties command = ((!recalculate) ? new MyRvLinkCommandGetDeviceBlockProperties(nextCommandId, myRvDeviceFromLogicalDevice.Value.Item1, myRvDeviceFromLogicalDevice.Value.Item2, blockId, BlockTransferPropertyId.CrcCached) : new MyRvLinkCommandGetDeviceBlockProperties(nextCommandId, myRvDeviceFromLogicalDevice.Value.Item1, myRvDeviceFromLogicalDevice.Value.Item2, blockId, BlockTransferPropertyId.CrcComputed));
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
			if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
			{
				if (myRvLinkCommandResponse is MyRvLinkGetDeviceBlockPropertyCommandResponse myRvLinkGetDeviceBlockPropertyCommandResponse)
				{
					return myRvLinkGetDeviceBlockPropertyCommandResponse.Crc;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to Get Block IDs from ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(": Unknown result");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			throw new MyRvLinkCommandResponseFailureException(failure);
		}

		public async Task<uint> GetDeviceBlockStartAddressAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(" is offline.");
				throw new LogicalDeviceException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			(byte, byte)? myRvDeviceFromLogicalDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvDeviceFromLogicalDevice.HasValue)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
				defaultInterpolatedStringHandler.AppendLiteral("No matching RvLink device for logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			MyRvLinkCommandGetDeviceBlockProperties command = new MyRvLinkCommandGetDeviceBlockProperties(GetNextCommandId(), myRvDeviceFromLogicalDevice.Value.Item1, myRvDeviceFromLogicalDevice.Value.Item2, blockId, BlockTransferPropertyId.StartAddress);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
			if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
			{
				if (myRvLinkCommandResponse is MyRvLinkGetDeviceBlockPropertyCommandResponse myRvLinkGetDeviceBlockPropertyCommandResponse)
				{
					return myRvLinkGetDeviceBlockPropertyCommandResponse.BlockStartAddress;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to Get Block IDs from ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(": Unknown result");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			throw new MyRvLinkCommandResponseFailureException(failure);
		}

		public async Task StartDeviceBlockTransferAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, BlockTransferStartOptions options, CancellationToken cancellationToken, uint? startAddress = null, uint? size = null)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(" is offline.");
				throw new LogicalDeviceException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			(byte, byte)? myRvDeviceFromLogicalDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvDeviceFromLogicalDevice.HasValue)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
				defaultInterpolatedStringHandler.AppendLiteral("No matching RvLink device for logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			MyRvLinkCommandStartDeviceBlockTransfer command = new MyRvLinkCommandStartDeviceBlockTransfer(GetNextCommandId(), myRvDeviceFromLogicalDevice.Value.Item1, myRvDeviceFromLogicalDevice.Value.Item2, blockId, options, startAddress, size);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken, MyRvLinkSendCommandOption.ExtendedWait);
			if (myRvLinkCommandResponse.CommandResult != 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(47, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to start block transfer, CommandResult: ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse.CommandResult);
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		public async Task StopDeviceBlockTransferAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, BlockTransferStopOptions options, CancellationToken cancellationToken)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(" is offline.");
				throw new LogicalDeviceException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			(byte, byte)? myRvDeviceFromLogicalDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvDeviceFromLogicalDevice.HasValue)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
				defaultInterpolatedStringHandler.AppendLiteral("No matching RvLink device for logical device ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(".");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			MyRvLinkCommandStopDeviceBlockTransfer command = new MyRvLinkCommandStopDeviceBlockTransfer(GetNextCommandId(), myRvDeviceFromLogicalDevice.Value.Item1, myRvDeviceFromLogicalDevice.Value.Item2, blockId, options);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken, MyRvLinkSendCommandOption.ExtendedWait);
			if (myRvLinkCommandResponse.CommandResult != 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(47, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to start block transfer, CommandResult: ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse.CommandResult);
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		public Task<IReadOnlyList<byte>> DeviceBlockReadAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public async Task DeviceBlockWriteAsync(ILogicalDevice logicalDevice, BlockTransferBlockId blockId, IReadOnlyList<byte> data, Func<ILogicalDeviceTransferProgress, bool> progressAck, CancellationToken cancellationToken)
		{
			_ = 6;
			try
			{
				_firmwareUpdateInProgress = true;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
				if (!IsLogicalDeviceOnline(logicalDevice))
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Logical device ");
					defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
					defaultInterpolatedStringHandler.AppendLiteral(" is offline.");
					throw new LogicalDeviceException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				(byte DeviceTableId, byte DeviceId)? myRvLinkDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
				if (!myRvLinkDevice.HasValue)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
					defaultInterpolatedStringHandler.AppendLiteral("No matching RvLink device for logical device ");
					defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
					defaultInterpolatedStringHandler.AppendLiteral(".");
					throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				ushort commandId = GetNextCommandId();
				cancellationToken.ThrowIfCancellationRequested();
				Stopwatch timer = Stopwatch.StartNew();
				int totalRetryAmount = 0;
				int currentCommandRetryAmount = 0;
				byte[] sendDataChunk = new byte[128];
				bool firstCommand = true;
				MyRvLinkCommandDeviceBlockWriteData commandData = null;
				ConcurrentQueue<IMyRvLinkCommandResponse> queue = new ConcurrentQueue<IMyRvLinkCommandResponse>();
				int bytesSent = 0;
				uint calculatedCrc32 = uint.MaxValue;
				BlockWriteTimeTracker timeTracker = new BlockWriteTimeTracker();
				int iterations = 0;
				while (bytesSent < data.Count && !cancellationToken.IsCancellationRequested)
				{
					iterations++;
					if (iterations % 100 == 0)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(7, 4);
						defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
						defaultInterpolatedStringHandler.AppendLiteral(" ");
						defaultInterpolatedStringHandler.AppendFormatted("DeviceBlockWriteAsync");
						defaultInterpolatedStringHandler.AppendLiteral(" Stats");
						defaultInterpolatedStringHandler.AppendFormatted(Environment.NewLine);
						defaultInterpolatedStringHandler.AppendFormatted(timeTracker);
						TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.ProgressAck);
					if (!progressAck(new LogicalDeviceTransferProgress((UInt48)bytesSent, (UInt48)totalRetryAmount, (UInt48)currentCommandRetryAmount, TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds))))
					{
						throw new OperationCanceledException();
					}
					timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.BufferCopy);
					int sendDataSize = ((data.Count - bytesSent < 128) ? (data.Count - bytesSent) : 128);
					sendDataChunk.Clear();
					data.ToExistingArray(bytesSent, sendDataChunk, 0, sendDataSize);
					timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.UpdateAndSendCommand);
					if (firstCommand)
					{
						commandData = new MyRvLinkCommandDeviceBlockWriteData(commandId, myRvLinkDevice.Value.DeviceTableId, myRvLinkDevice.Value.DeviceId, blockId, (uint)bytesSent, (byte)sendDataSize, sendDataChunk);
						firstCommand = false;
						IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(commandData, cancellationToken, MyRvLinkSendCommandOption.DontWaitForResponse, delegate(IMyRvLinkCommandResponse response)
						{
							queue.Enqueue(response);
						});
						queue.Enqueue(myRvLinkCommandResponse);
					}
					else
					{
						commandData.UpdateCommand((uint)bytesSent, (byte)sendDataSize, sendDataChunk);
						if (!(await ResendRunningCommandAsync(commandId, cancellationToken)))
						{
							commandId = GetNextCommandId();
							totalRetryAmount++;
							await TaskExtension.TryDelay(200, cancellationToken);
							continue;
						}
					}
					bool waitForNewMessage = true;
					while (!cancellationToken.IsCancellationRequested)
					{
						timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.WaitingForResponse);
						if (!queue.TryDequeue(out var myRvLinkCommandResponse2))
						{
							if (!waitForNewMessage || cancellationToken.IsCancellationRequested)
							{
								break;
							}
							timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.WaitingForResponsePollDelay);
							await Task.Delay(5, cancellationToken);
							continue;
						}
						cancellationToken.ThrowIfCancellationRequested();
						timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.ProcessResponse);
						TaggedLog.Debug("DirectConnectionMyRvLink", "Processing Response");
						waitForNewMessage = false;
						if (!(myRvLinkCommandResponse2 is MyRvLinkCommandResponseSuccessNoWait))
						{
							if (!(myRvLinkCommandResponse2 is IMyRvLinkCommandResponseFailure myRvLinkCommandResponseFailure))
							{
								if (!(myRvLinkCommandResponse2 is MyRvLinkDeviceBlockWriteDataCommandResponse myRvLinkDeviceBlockWriteDataCommandResponse))
								{
									defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(47, 1);
									defaultInterpolatedStringHandler.AppendLiteral("DeviceBlockWrite received unexpected response: ");
									defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse2);
									TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
									throw new BlockTransferWriteFailedException(logicalDevice, blockId, new LogicalDeviceTransferProgress((UInt48)bytesSent, (UInt48)totalRetryAmount, (UInt48)currentCommandRetryAmount, TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds)));
								}
								uint num = Crc32Le.Calculate(calculatedCrc32, sendDataChunk, sendDataSize, 0u);
								if (num != myRvLinkDeviceBlockWriteDataCommandResponse.Crc32)
								{
									defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(123, 4);
									defaultInterpolatedStringHandler.AppendLiteral("DeviceBlockWrite received success, but Crc was bad. Calculated Crc: ");
									defaultInterpolatedStringHandler.AppendFormatted(num);
									defaultInterpolatedStringHandler.AppendLiteral(" Received Crc: ");
									defaultInterpolatedStringHandler.AppendFormatted(myRvLinkDeviceBlockWriteDataCommandResponse.Crc32);
									defaultInterpolatedStringHandler.AppendLiteral(" CurrentRetryAmount: ");
									defaultInterpolatedStringHandler.AppendFormatted(currentCommandRetryAmount);
									defaultInterpolatedStringHandler.AppendLiteral(" totalRetryAmount: ");
									defaultInterpolatedStringHandler.AppendFormatted(totalRetryAmount);
									TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
									currentCommandRetryAmount++;
									totalRetryAmount++;
								}
								else
								{
									calculatedCrc32 = num;
									defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(119, 4);
									defaultInterpolatedStringHandler.AppendLiteral("DeviceBlockWrite received success, Crc is GOOD. Calculated Crc: ");
									defaultInterpolatedStringHandler.AppendFormatted(calculatedCrc32);
									defaultInterpolatedStringHandler.AppendLiteral(" Received Crc: ");
									defaultInterpolatedStringHandler.AppendFormatted(myRvLinkDeviceBlockWriteDataCommandResponse.Crc32);
									defaultInterpolatedStringHandler.AppendLiteral(" CurrentRetryAmount: ");
									defaultInterpolatedStringHandler.AppendFormatted(currentCommandRetryAmount);
									defaultInterpolatedStringHandler.AppendLiteral(" totalRetryAmount: ");
									defaultInterpolatedStringHandler.AppendFormatted(totalRetryAmount);
									TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
									currentCommandRetryAmount = 0;
									bytesSent += 128;
								}
							}
							else
							{
								defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(76, 3);
								defaultInterpolatedStringHandler.AppendLiteral("DeviceBlockWrite received failure: ");
								defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseFailure);
								defaultInterpolatedStringHandler.AppendLiteral(". CurrentRetryAmount: ");
								defaultInterpolatedStringHandler.AppendFormatted(currentCommandRetryAmount);
								defaultInterpolatedStringHandler.AppendLiteral(" totalRetryAmount: ");
								defaultInterpolatedStringHandler.AppendFormatted(totalRetryAmount);
								TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
								if (myRvLinkCommandResponseFailure.IsCommandCompleted)
								{
									throw new BlockTransferWriteFailedException(logicalDevice, blockId, new LogicalDeviceTransferProgress((UInt48)bytesSent, (UInt48)totalRetryAmount, (UInt48)currentCommandRetryAmount, TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds)));
								}
								await TaskExtension.TryDelay(200, cancellationToken);
								currentCommandRetryAmount++;
								totalRetryAmount++;
							}
						}
						else
						{
							defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(81, 2);
							defaultInterpolatedStringHandler.AppendLiteral("Starting block transfer (1st block sent). CurrentRetryAmount: ");
							defaultInterpolatedStringHandler.AppendFormatted(currentCommandRetryAmount);
							defaultInterpolatedStringHandler.AppendLiteral(" totalRetryAmount: ");
							defaultInterpolatedStringHandler.AppendFormatted(totalRetryAmount);
							TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
							waitForNewMessage = true;
						}
					}
				}
				cancellationToken.ThrowIfCancellationRequested();
				timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.Finish);
				Task<IMyRvLinkCommandResponse> finishResultTask = WaitForRunningCommandToComplete(commandData.ClientCommandId, cancellationToken);
				commandData.UpdateCommand(uint.MaxValue, 0);
				if (!(await ResendRunningCommandAsync(commandId, cancellationToken)))
				{
					throw new BlockTransferWriteFailedException(logicalDevice, blockId, new LogicalDeviceTransferProgress((UInt48)bytesSent, (UInt48)totalRetryAmount, (UInt48)currentCommandRetryAmount, TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds)));
				}
				cancellationToken.ThrowIfCancellationRequested();
				TaggedLog.Debug("DirectConnectionMyRvLink", LogPrefix + " Waiting for Response to finish the write");
				IMyRvLinkCommandResponse myRvLinkCommandResponse3 = await finishResultTask;
				if (myRvLinkCommandResponse3.CommandResult != 0 || myRvLinkCommandResponse3 is IMyRvLinkCommandResponseFailure)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(73, 1);
					defaultInterpolatedStringHandler.AppendLiteral("DeviceBlockWrite received a failure on the finish write command. Result: ");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse3);
					TaggedLog.Error("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
					throw new BlockTransferWriteFailedException(logicalDevice, blockId, new LogicalDeviceTransferProgress((UInt48)data.Count, (UInt48)totalRetryAmount, (UInt48)currentCommandRetryAmount, TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds)));
				}
				timeTracker.Stop();
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(7, 4);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendFormatted("DeviceBlockWriteAsync");
				defaultInterpolatedStringHandler.AppendLiteral(" Stats");
				defaultInterpolatedStringHandler.AppendFormatted(Environment.NewLine);
				defaultInterpolatedStringHandler.AppendFormatted(timeTracker);
				TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.ProgressAck);
				if (!progressAck(new LogicalDeviceTransferProgress((UInt48)bytesSent, (UInt48)totalRetryAmount, (UInt48)currentCommandRetryAmount, TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds))))
				{
					throw new OperationCanceledException();
				}
			}
			finally
			{
				_firmwareUpdateInProgress = false;
			}
		}

		public async Task<CommandResult> SendDirectCommandLeveler1(ILogicalDeviceLevelerType1 logicalDevice, LogicalDeviceLevelerCommandType1 command, CancellationToken cancelToken)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				return CommandResult.ErrorDeviceOffline;
			}
			(byte DeviceTableId, byte DeviceId)? myRvLinkDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvLinkDevice.HasValue)
			{
				return CommandResult.ErrorDeviceOffline;
			}
			long nowTimestampMs = LogicalDeviceFreeRunningTimer.ElapsedMilliseconds;
			try
			{
				(ushort, LogicalDeviceLevelerCommandType1, long) lastSentLeveler1Command = _lastSentLeveler1Command;
				long num = nowTimestampMs - lastSentLeveler1Command.Item3;
				if (lastSentLeveler1Command.Item1 != 0 && lastSentLeveler1Command.Item2 == command && num < 1000 && ArrayCommon.ArraysEqual(_lastSentLeveler1CommandData, command.CopyCurrentData()))
				{
					_lastSentLeveler1Command = (lastSentLeveler1Command.Item1, command, nowTimestampMs);
					_lastSentLeveler1CommandData = command.CopyCurrentData();
					if (await ResendRunningCommandAsync(lastSentLeveler1Command.Item1, cancelToken))
					{
						return CommandResult.Completed;
					}
				}
				ushort nextCommandId = GetNextCommandId();
				_lastSentLeveler1Command = (nextCommandId, command, nowTimestampMs);
				_lastSentLeveler1CommandData = command.CopyCurrentData();
				MyRvLinkCommandLeveler1ButtonCommand command2 = new MyRvLinkCommandLeveler1ButtonCommand(nextCommandId, myRvLinkDevice.Value.DeviceTableId, myRvLinkDevice.Value.DeviceId, command);
				return (await SendCommandAsync(command2, cancelToken, MyRvLinkSendCommandOption.DontWaitForResponse)).CommandResult;
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", "Sending command failed " + ex.Message);
				return CommandResult.ErrorOther;
			}
		}

		public async Task<CommandResult> SendDirectCommandLeveler3(ILogicalDeviceLevelerType3 logicalDevice, LogicalDeviceLevelerCommandType3 command, CancellationToken cancelToken)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				return CommandResult.ErrorDeviceOffline;
			}
			(byte DeviceTableId, byte DeviceId)? myRvLinkDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvLinkDevice.HasValue)
			{
				return CommandResult.ErrorDeviceOffline;
			}
			try
			{
				MyRvLinkCommandContext<LogicalDeviceLevelerButtonType3> commandContext;
				lock (this)
				{
					if (logicalDevice.CustomData.TryGetValue("DirectConnectionMyRvLink.IDirectCommandLeveler3", out var obj) && obj is MyRvLinkCommandContext<LogicalDeviceLevelerButtonType3> myRvLinkCommandContext)
					{
						commandContext = myRvLinkCommandContext;
					}
					else
					{
						logicalDevice.CustomData["DirectConnectionMyRvLink.IDirectCommandLeveler3"] = (commandContext = new MyRvLinkCommandContext<LogicalDeviceLevelerButtonType3>());
					}
				}
				if (commandContext.LastSentCommandReceivedError)
				{
					TaggedLog.Warning("DirectConnectionMyRvLink", "Leveler 3 last sent command received an error!");
					commandContext.ClearLastSentCommandReceivedError();
					return commandContext.ActiveFailure?.CommandResult ?? CommandResult.ErrorOther;
				}
				bool flag = commandContext.CanResendCommand(command.ButtonsPressed, command);
				if (flag)
				{
					flag = await ResendRunningCommandAsync(commandContext.SentCommandId, cancelToken);
				}
				if (flag)
				{
					commandContext.SentCommand(commandContext.SentCommandId, command.ButtonsPressed, command);
					return commandContext.ActiveFailure?.CommandResult ?? CommandResult.Completed;
				}
				ushort nextCommandId = GetNextCommandId();
				MyRvLinkCommandLeveler3ButtonCommand command2 = new MyRvLinkCommandLeveler3ButtonCommand(nextCommandId, myRvLinkDevice.Value.DeviceTableId, myRvLinkDevice.Value.DeviceId, command);
				SendCommandAsync(command2, cancelToken, TimeSpan.FromMilliseconds(2500.0), MyRvLinkSendCommandOption.DontWaitForResponse, delegate(IMyRvLinkCommandResponse response)
				{
					commandContext.ProcessResponse(response);
				});
				commandContext.SentCommand(nextCommandId, command.ButtonsPressed, command);
				return commandContext.ActiveFailure?.CommandResult ?? CommandResult.Completed;
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", "Sending command failed " + ex.Message);
				return CommandResult.ErrorOther;
			}
		}

		public async Task<CommandResult> SendDirectCommandLeveler4(ILogicalDeviceLevelerType4 logicalDevice, ILogicalDeviceLevelerCommandType4 command, CancellationToken cancelToken)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				return CommandResult.ErrorDeviceOffline;
			}
			(byte DeviceTableId, byte DeviceId)? myRvLinkDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvLinkDevice.HasValue)
			{
				return CommandResult.ErrorDeviceOffline;
			}
			try
			{
				MyRvLinkCommandContext<LogicalDeviceLevelerCommandType4.LevelerCommandCode> commandContext;
				lock (this)
				{
					if (logicalDevice.CustomData.TryGetValue("DirectConnectionMyRvLink.IDirectCommandLeveler4", out var obj) && obj is MyRvLinkCommandContext<LogicalDeviceLevelerCommandType4.LevelerCommandCode> myRvLinkCommandContext)
					{
						commandContext = myRvLinkCommandContext;
					}
					else
					{
						logicalDevice.CustomData["DirectConnectionMyRvLink.IDirectCommandLeveler4"] = (commandContext = new MyRvLinkCommandContext<LogicalDeviceLevelerCommandType4.LevelerCommandCode>());
					}
				}
				if (commandContext.LastSentCommandReceivedError)
				{
					TaggedLog.Warning("DirectConnectionMyRvLink", "Leveler 4 last sent command received an error!");
					commandContext.ClearLastSentCommandReceivedError();
					return commandContext.ActiveFailure?.CommandResult ?? CommandResult.ErrorOther;
				}
				bool flag = commandContext.CanResendCommand(command.Command, command);
				if (flag)
				{
					flag = await ResendRunningCommandAsync(commandContext.SentCommandId, cancelToken);
				}
				if (flag)
				{
					commandContext.SentCommand(commandContext.SentCommandId, command.Command, command);
					return commandContext.ActiveFailure?.CommandResult ?? CommandResult.Completed;
				}
				ushort nextCommandId = GetNextCommandId();
				MyRvLinkCommandLeveler4ButtonCommand command2 = new MyRvLinkCommandLeveler4ButtonCommand(nextCommandId, myRvLinkDevice.Value.DeviceTableId, myRvLinkDevice.Value.DeviceId, command);
				SendCommandAsync(command2, cancelToken, TimeSpan.FromMilliseconds(2500.0), MyRvLinkSendCommandOption.DontWaitForResponse, delegate(IMyRvLinkCommandResponse response)
				{
					commandContext.ProcessResponse(response);
				});
				commandContext.SentCommand(nextCommandId, command.Command, command);
				return commandContext.ActiveFailure?.CommandResult ?? CommandResult.Completed;
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", "Sending command failed " + ex.Message);
				return CommandResult.ErrorOther;
			}
		}

		protected DirectConnectionMyRvLink(ILogicalDeviceService deviceService, string logPrefix, List<ILogicalDeviceTag> gatewayTagList)
		{
			LogPrefix = "[" + logPrefix + "]";
			DeviceService = deviceService ?? throw new ArgumentNullException("deviceService");
			ConnectionTagList = ((gatewayTagList != null) ? Enumerable.ToList(gatewayTagList) : null) ?? new List<ILogicalDeviceTag>();
			SessionManager = new MyRvLinkSessionManager(this);
			DeviceTableIdCache = new DeviceTableIdCache(this);
			GetPendingCommand = delegate(int commandId)
			{
				lock (_lock)
				{
					if (!_commandActiveDict.TryGetValue(commandId, out var value))
					{
						return null;
					}
					return value.Command;
				}
			};
		}

		public bool IsLogicalDeviceOnline(ILogicalDevice? logicalDevice)
		{
			return _deviceTracker?.IsLogicalDeviceOnline(logicalDevice) ?? false;
		}

		public IN_MOTION_LOCKOUT_LEVEL GetLogicalDeviceInTransitLockoutLevel(ILogicalDevice? logicalDevice)
		{
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				return (byte)0;
			}
			return _deviceTracker?.GetLogicalDeviceInTransitLockoutLevel(logicalDevice) ?? ((IN_MOTION_LOCKOUT_LEVEL)(byte)0);
		}

		public bool IsLogicalDeviceHazardous(ILogicalDevice? logicalDevice)
		{
			return (byte)GetLogicalDeviceInTransitLockoutLevel(logicalDevice) != 0;
		}

		public bool IsLogicalDeviceSupported(ILogicalDevice? logicalDevice)
		{
			if (!(logicalDevice is ILogicalDeviceSimulated))
			{
				if (logicalDevice is ILogicalDeviceMyRvLink)
				{
					return true;
				}
				return false;
			}
			return false;
		}

		public virtual void Start()
		{
			lock (_lock)
			{
				if (!IsStarted)
				{
					_deviceTracker?.UpdateOnlineStatus(null);
				}
				_takeDevicesOfflineTimer?.TryDispose();
				_takeDevicesOfflineTimer = new Timer(TakeDevicesOfflineCheck, null, _takeDevicesOfflineCheckTime, _takeDevicesOfflineCheckTime);
				_versionTracker?.TryDispose();
				_versionTracker = new MyRvLinkVersionTracker(this);
				IsStarted = true;
			}
		}

		public virtual void Stop()
		{
			lock (_lock)
			{
				_takeDevicesOfflineTimer?.TryDispose();
				_takeDevicesOfflineTimer = null;
				IsStarted = false;
				RealTimeClock = null;
				AbortAllPendingCommands();
				_deviceTracker?.UpdateOnlineStatus(null);
				_versionTracker?.TryDispose();
				_versionTracker = null;
			}
		}

		private void TakeDevicesOfflineCheck(object state)
		{
			if (IsStarted)
			{
				_deviceTracker?.TakeDevicesOfflineIfNeeded();
			}
		}

		public ushort GetNextCommandId()
		{
			lock (_lock)
			{
				ushort nextCommandId = _nextCommandId;
				ushort nextCommandId2;
				do
				{
					nextCommandId2 = _nextCommandId;
					_nextCommandId++;
				}
				while (_commandActiveDict.ContainsKey(nextCommandId2) && _nextCommandId != nextCommandId);
				if (_nextCommandId == nextCommandId)
				{
					throw new MyRvLinkException("GetNextCommandId failed because no Command Id's are available");
				}
				if (_nextCommandId == ushort.MaxValue)
				{
					_nextCommandId = 1;
				}
				if (_nextCommandId == 0)
				{
					_nextCommandId = 1;
				}
				_currentCommandId = nextCommandId2;
				return nextCommandId2;
			}
		}

		public Task<IMyRvLinkCommandResponse> SendCommandAsync(IMyRvLinkCommand command, CancellationToken cancellationToken, MyRvLinkSendCommandOption commandOption = MyRvLinkSendCommandOption.None, Action<IMyRvLinkCommandResponse>? responseAction = null)
		{
			return SendCommandAsync(command, cancellationToken, TimeSpan.FromMilliseconds(8000.0), commandOption, responseAction);
		}

		public async Task<IMyRvLinkCommandResponse> SendCommandAsync(IMyRvLinkCommand command, CancellationToken cancellationToken, TimeSpan commandTimeout, MyRvLinkSendCommandOption commandOption = MyRvLinkSendCommandOption.None, Action<IMyRvLinkCommandResponse>? responseAction = null)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 2);
			defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
			defaultInterpolatedStringHandler.AppendLiteral(" Adapter SendCommand ");
			defaultInterpolatedStringHandler.AppendFormatted(command);
			TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
			MyRvLinkCommandTracker commandTracker = null;
			if (command == null)
			{
				TaggedLog.Debug("DirectConnectionMyRvLink", LogPrefix + " SendCommandAsync Failed because given command was NULL.");
				return new MyRvLinkCommandResponseFailure(0, MyRvLinkCommandResponseFailureCode.InvalidCommand);
			}
			if (!IsStarted || !IsConnected)
			{
				return new MyRvLinkCommandResponseFailure(command.ClientCommandId, MyRvLinkCommandResponseFailureCode.Offline);
			}
			if (command.ClientCommandId == ushort.MaxValue)
			{
				try
				{
					UpdateFrequencyMetricForCommandSend(command.CommandType);
					await SendCommandRawAsync(command, cancellationToken).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 3);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" SendCommandRawAsync Failed ");
					defaultInterpolatedStringHandler.AppendFormatted(command);
					defaultInterpolatedStringHandler.AppendLiteral(": ");
					defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
					TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				return _responseSuccessNoResponse;
			}
			lock (_lock)
			{
				FlushCompletedCommands();
				if (_commandActiveDict.ContainsKey(command.ClientCommandId))
				{
					TaggedLog.Debug("DirectConnectionMyRvLink", LogPrefix + " SendCommandAsync failed because it is current running use ResendRunningCommandAsync to resend a command.");
					return new MyRvLinkCommandResponseFailure(command.ClientCommandId, MyRvLinkCommandResponseFailureCode.CommandAlreadyRunning);
				}
				if (_commandActiveDict.Count + 1 >= 20)
				{
					return new MyRvLinkCommandResponseFailure(command.ClientCommandId, MyRvLinkCommandResponseFailureCode.Offline);
				}
				int num = (int)commandTimeout.TotalMilliseconds;
				if (commandOption.HasFlag(MyRvLinkSendCommandOption.ExtendedWait))
				{
					num = Math.Max(num, 16000);
				}
				commandTracker = new MyRvLinkCommandTracker(command, cancellationToken, num, responseAction);
				_commandActiveDict[command.ClientCommandId] = commandTracker;
			}
			BackgroundOperation keepAliveBackgroundOperation = null;
			try
			{
				UpdateFrequencyMetricForCommandSend(command.CommandType);
				await SendCommandRawAsync(command, cancellationToken).ConfigureAwait(false);
				if (commandOption.HasFlag(MyRvLinkSendCommandOption.DontWaitForResponse))
				{
					return new MyRvLinkCommandResponseSuccessNoWait(command.ClientCommandId);
				}
				if (commandOption.HasFlag(MyRvLinkSendCommandOption.WaitForAnyResponse))
				{
					return await commandTracker.WaitForAnyResponse();
				}
				return await commandTracker.WaitAsync();
			}
			catch (TimeoutException)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 2);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" SendCommandAsync Timeout ");
				defaultInterpolatedStringHandler.AppendFormatted(command);
				TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				return commandTracker.TrySetFailure(MyRvLinkCommandResponseFailureCode.CommandTimeout);
			}
			catch (OperationCanceledException)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 2);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" SendCommandAsync Canceled ");
				defaultInterpolatedStringHandler.AppendFormatted(command);
				TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				return commandTracker.TrySetFailure(MyRvLinkCommandResponseFailureCode.CommandAborted);
			}
			catch (Exception ex4)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 3);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" SendCommandAsync Failure ");
				defaultInterpolatedStringHandler.AppendFormatted(command);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(ex4.Message);
				TaggedLog.Warning("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				return commandTracker.TrySetFailure(MyRvLinkCommandResponseFailureCode.Other);
			}
			finally
			{
				keepAliveBackgroundOperation?.Stop();
			}
		}

		public async Task<CommandResult> SendCommandAsync(ILogicalDevice logicalDevice, Func<(byte DeviceTableId, byte DeviceId), IMyRvLinkCommand> commandFactory, CancellationToken cancellationToken, MyRvLinkSendCommandOption commandOption = MyRvLinkSendCommandOption.None, Action<IMyRvLinkCommandResponse>? responseAction = null)
		{
			try
			{
				if (!(_deviceTracker?.IsLogicalDeviceOnline(logicalDevice) ?? false))
				{
					return CommandResult.ErrorDeviceOffline;
				}
				(byte, byte)? myRvDeviceFromLogicalDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
				if (!myRvDeviceFromLogicalDevice.HasValue)
				{
					return CommandResult.ErrorDeviceOffline;
				}
				IMyRvLinkCommand myRvLinkCommand = commandFactory(myRvDeviceFromLogicalDevice.Value);
				IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(myRvLinkCommand, cancellationToken, commandOption, responseAction);
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 3);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" Sent command ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommand);
				defaultInterpolatedStringHandler.AppendLiteral(" received response ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse);
				TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				return myRvLinkCommandResponse.CommandResult;
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", LogPrefix + " Sending command failed " + ex.Message);
				return CommandResult.ErrorOther;
			}
		}

		public async Task<bool> ResendRunningCommandAsync(ushort commandId, CancellationToken cancellationToken)
		{
			if (commandId == 0 || commandId == ushort.MaxValue)
			{
				return false;
			}
			if (commandId != _currentCommandId)
			{
				return false;
			}
			MyRvLinkCommandTracker myRvLinkCommandTracker;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			lock (_lock)
			{
				FlushCompletedCommands();
				myRvLinkCommandTracker = _commandActiveDict.TryGetValue(commandId);
				if (myRvLinkCommandTracker == null)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 3);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" Unable to ");
					defaultInterpolatedStringHandler.AppendFormatted("ResendRunningCommandAsync");
					defaultInterpolatedStringHandler.AppendLiteral(" because command tracker for ");
					defaultInterpolatedStringHandler.AppendFormatted(commandId);
					defaultInterpolatedStringHandler.AppendLiteral(" not found.");
					TaggedLog.Warning("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
					return false;
				}
				myRvLinkCommandTracker.ResetTimer();
			}
			if (myRvLinkCommandTracker.IsCompleted)
			{
				return false;
			}
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 2);
			defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
			defaultInterpolatedStringHandler.AppendLiteral(" Resend command ");
			defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandTracker.Command);
			TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
			UpdateFrequencyMetricForCommandSend(myRvLinkCommandTracker.Command.CommandType);
			await SendCommandRawAsync(myRvLinkCommandTracker.Command, cancellationToken);
			return true;
		}

		public async Task<IMyRvLinkCommandResponse?> ResendRunningCommandWaitForResponseAsync(ushort commandId, CancellationToken cancellationToken)
		{
			if (commandId == 0 || commandId == ushort.MaxValue)
			{
				return null;
			}
			if (commandId != _currentCommandId)
			{
				return null;
			}
			MyRvLinkCommandTracker commandTracker;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			lock (_lock)
			{
				FlushCompletedCommands();
				commandTracker = _commandActiveDict.TryGetValue(commandId);
				if (commandTracker == null)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 3);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" Unable to ");
					defaultInterpolatedStringHandler.AppendFormatted("ResendRunningCommandAsync");
					defaultInterpolatedStringHandler.AppendLiteral(" because command tracker for ");
					defaultInterpolatedStringHandler.AppendFormatted(commandId);
					defaultInterpolatedStringHandler.AppendLiteral(" not found.");
					TaggedLog.Warning("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
					return null;
				}
				commandTracker.ResetTimer();
			}
			if (commandTracker.IsCompleted)
			{
				return null;
			}
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 2);
			defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
			defaultInterpolatedStringHandler.AppendLiteral(" Resend command ");
			defaultInterpolatedStringHandler.AppendFormatted(commandTracker.Command);
			TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
			try
			{
				UpdateFrequencyMetricForCommandSend(commandTracker.Command.CommandType);
				await SendCommandRawAsync(commandTracker.Command, cancellationToken).ConfigureAwait(false);
				IMyRvLinkCommandResponse obj = await commandTracker.WaitForAnyResponse();
				if (obj is IMyRvLinkCommandResponseFailure myRvLinkCommandResponseFailure)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(64, 2);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" ResendRunningCommandWaitForResponseAsync Failed with response: ");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseFailure);
					TaggedLog.Warning("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				return obj;
			}
			catch (TimeoutException)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(50, 2);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" ResendRunningCommandWaitForResponseAsync Timeout ");
				defaultInterpolatedStringHandler.AppendFormatted(commandTracker.Command);
				TaggedLog.Warning("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				return commandTracker.TrySetFailure(MyRvLinkCommandResponseFailureCode.CommandTimeout);
			}
			catch (OperationCanceledException)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 2);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" ResendRunningCommandWaitForResponseAsync Canceled ");
				defaultInterpolatedStringHandler.AppendFormatted(commandTracker.Command);
				TaggedLog.Warning("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				return commandTracker.TrySetFailure(MyRvLinkCommandResponseFailureCode.CommandAborted);
			}
			catch (Exception ex3)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(52, 3);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" ResendRunningCommandWaitForResponseAsync Failure ");
				defaultInterpolatedStringHandler.AppendFormatted(commandTracker.Command);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(ex3.Message);
				TaggedLog.Warning("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				return commandTracker.TrySetFailure(MyRvLinkCommandResponseFailureCode.Other);
			}
		}

		public async Task<IMyRvLinkCommandResponse> WaitForRunningCommandToComplete(ushort commandId, CancellationToken cancellationToken)
		{
			if (commandId == 0 || commandId == ushort.MaxValue)
			{
				return new MyRvLinkCommandResponseFailure(commandId, MyRvLinkCommandResponseFailureCode.InvalidCommand);
			}
			MyRvLinkCommandTracker commandTracker;
			lock (_lock)
			{
				FlushCompletedCommands();
				commandTracker = _commandActiveDict.TryGetValue(commandId);
				if (commandTracker == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 3);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" Unable to ");
					defaultInterpolatedStringHandler.AppendFormatted("WaitForRunningCommandToComplete");
					defaultInterpolatedStringHandler.AppendLiteral(" because command tracker for ");
					defaultInterpolatedStringHandler.AppendFormatted(commandId);
					defaultInterpolatedStringHandler.AppendLiteral(" not found.");
					TaggedLog.Warning("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
					return new MyRvLinkCommandResponseFailure(commandId, MyRvLinkCommandResponseFailureCode.InvalidCommand);
				}
				commandTracker.ResetTimer();
			}
			try
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 2);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" Wait for command to complete ");
				defaultInterpolatedStringHandler.AppendFormatted(commandTracker.Command);
				TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				return await commandTracker.WaitAsync();
			}
			catch (TimeoutException)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 2);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" SendCommandAsync Timeout ");
				defaultInterpolatedStringHandler.AppendFormatted(commandTracker.Command);
				TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				return commandTracker.TrySetFailure(MyRvLinkCommandResponseFailureCode.CommandTimeout);
			}
			catch (OperationCanceledException)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 2);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" SendCommandAsync Canceled ");
				defaultInterpolatedStringHandler.AppendFormatted(commandTracker.Command);
				TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				return commandTracker.TrySetFailure(MyRvLinkCommandResponseFailureCode.CommandAborted);
			}
			catch (Exception ex3)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 3);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" SendCommandAsync Failure ");
				defaultInterpolatedStringHandler.AppendFormatted(commandTracker.Command);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(ex3.Message);
				TaggedLog.Warning("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				return commandTracker.TrySetFailure(MyRvLinkCommandResponseFailureCode.Other);
			}
		}

		protected abstract Task SendCommandRawAsync(IMyRvLinkCommand command, CancellationToken cancellationToken);

		private void FlushCompletedCommands()
		{
			lock (_lock)
			{
				Enumerable.ToList(Enumerable.Where<KeyValuePair<int, MyRvLinkCommandTracker>>(_commandActiveDict, (KeyValuePair<int, MyRvLinkCommandTracker> commandTrackerEntry) => commandTrackerEntry.Value.IsCompleted)).ForEach(delegate(KeyValuePair<int, MyRvLinkCommandTracker> commandTrackerEntry)
				{
					commandTrackerEntry.Value.TryDispose();
					_commandActiveDict.Remove(commandTrackerEntry.Key);
					_commandCompletedQueue.TryAdd(commandTrackerEntry.Value);
				});
			}
		}

		private void AbortAllPendingCommands()
		{
			lock (_lock)
			{
				MyRvLinkDeviceTracker? deviceTracker = _deviceTracker;
				if (deviceTracker != null && deviceTracker!.DeviceList.Count == 0)
				{
					_deviceTracker!.Dispose();
					_deviceTracker = null;
				}
				foreach (KeyValuePair<int, MyRvLinkCommandTracker> item in _commandActiveDict)
				{
					if (!item.Value.IsCompleted)
					{
						item.Value.TrySetFailure(MyRvLinkCommandResponseFailureCode.CommandAborted);
					}
				}
				_commandActiveDict.Clear();
			}
		}

		public void DebugDumpRunningCommands()
		{
			lock (_lock)
			{
				if (_commandActiveDict.Count == 0)
				{
					TaggedLog.Information("DirectConnectionMyRvLink", LogPrefix + " No Commands");
					return;
				}
				foreach (KeyValuePair<int, MyRvLinkCommandTracker> item in _commandActiveDict)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 3);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					defaultInterpolatedStringHandler.AppendFormatted(item.Key);
					defaultInterpolatedStringHandler.AppendLiteral(": ");
					defaultInterpolatedStringHandler.AppendFormatted(item.Value);
					TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
		}

		public (byte DeviceTableId, byte DeviceId)? GetMyRvDeviceFromLogicalDevice(ILogicalDevice logicalDevice)
		{
			MyRvLinkDeviceTracker deviceTracker = _deviceTracker;
			byte? b = deviceTracker?.GetMyRvDeviceIdFromLogicalDevice(logicalDevice);
			if (!b.HasValue)
			{
				return null;
			}
			return (deviceTracker.DeviceTableId, b.Value);
		}

		public ILogicalDevice? GetLogicalDeviceFromMyRvDevice(byte deviceTableId, byte deviceId)
		{
			return _deviceTracker?.GetLogicalDeviceFromMyRvDevice(deviceTableId, deviceId);
		}

		public IEnumerable<ILogicalDeviceTag> MakeDeviceSourceTags(ILogicalDevice? logicalDevice)
		{
			return DeviceSourceTags;
		}

		public virtual LogicalDeviceReachability DeviceSourceReachability(ILogicalDevice logicalDevice)
		{
			if (!logicalDevice.IsAssociatedWithDeviceSource(this))
			{
				return LogicalDeviceReachability.Unknown;
			}
			if (!IsDeviceSourceActive)
			{
				return LogicalDeviceReachability.Unreachable;
			}
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				return LogicalDeviceReachability.Unreachable;
			}
			return LogicalDeviceReachability.Reachable;
		}

		public override string ToString()
		{
			return "DirectConnectionMyRvLink " + LogPrefix;
		}

		public Task<CommandResult> SendDirectCommandAccessoryGatewayAsync(ILogicalDeviceAccessoryGateway logicalDevice, LogicalDeviceAccessoryGatewayCommand command, CancellationToken cancelToken)
		{
			LogicalDeviceAccessoryGatewayCommand command2 = command;
			return SendCommandAsync(logicalDevice, ((byte DeviceTableId, byte DeviceId) myRvLinkDevice) => new MyRvLinkCommandActionAccessoryGateway(GetNextCommandId(), myRvLinkDevice.DeviceTableId, myRvLinkDevice.DeviceId, command2), cancelToken);
		}

		public Task<CommandResult> SendDirectCommandClimateZoneAsync(ILogicalDeviceClimateZone logicalDevice, LogicalDeviceClimateZoneCommand command, CancellationToken cancelToken)
		{
			LogicalDeviceClimateZoneCommand command2 = command;
			return SendCommandAsync(logicalDevice, ((byte DeviceTableId, byte DeviceId) myRvLinkDevice) => new MyRvLinkCommandActionHvac(GetNextCommandId(), myRvLinkDevice.DeviceTableId, myRvLinkDevice.DeviceId, command2), cancelToken);
		}

		public Task<CommandResult> SendDirectCommandGeneratorGenie(ILogicalDeviceGeneratorGenie logicalDevice, GeneratorGenieCommand command, CancellationToken cancelToken)
		{
			return SendCommandAsync(logicalDevice, ((byte DeviceTableId, byte DeviceId) myRvLinkDevice) => new MyRvLinkCommandActionGeneratorGenie(GetNextCommandId(), myRvLinkDevice.DeviceTableId, myRvLinkDevice.DeviceId, command), cancelToken);
		}

		public async Task<LevelerCommandResultType5> SendDirectCommandLeveler5Async(ILogicalDeviceLevelerType5 logicalDevice, ILogicalDeviceLevelerCommandType5 command, CancellationToken cancelToken)
		{
			if (!(_deviceTracker?.IsLogicalDeviceOnline(logicalDevice) ?? false))
			{
				return new LevelerCommandResultType5(CommandResult.ErrorDeviceOffline);
			}
			(byte DeviceTableId, byte DeviceId)? myRvLinkDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvLinkDevice.HasValue)
			{
				return new LevelerCommandResultType5(CommandResult.ErrorDeviceOffline);
			}
			try
			{
				MyRvLinkCommandContext<LogicalDeviceLevelerCommandType5.LevelerCommandCode> commandContext;
				lock (this)
				{
					if (logicalDevice.CustomData.TryGetValue("DirectConnectionMyRvLink.IDirectCommandLeveler5", out var obj) && obj is MyRvLinkCommandContext<LogicalDeviceLevelerCommandType5.LevelerCommandCode> myRvLinkCommandContext)
					{
						commandContext = myRvLinkCommandContext;
					}
					else
					{
						ConcurrentDictionary<string, object> customData = logicalDevice.CustomData;
						MyRvLinkCommandContext<LogicalDeviceLevelerCommandType5.LevelerCommandCode> myRvLinkCommandContext2;
						commandContext = (myRvLinkCommandContext2 = new MyRvLinkCommandContext<LogicalDeviceLevelerCommandType5.LevelerCommandCode>());
						customData["DirectConnectionMyRvLink.IDirectCommandLeveler5"] = myRvLinkCommandContext2;
					}
				}
				if (commandContext.LastSentCommandReceivedError)
				{
					commandContext.ClearLastSentCommandReceivedError();
				}
				bool flag = commandContext.CanResendCommand(command.Command, command);
				IMyRvLinkCommandResponse myRvLinkCommandResponse = default(IMyRvLinkCommandResponse);
				if (flag)
				{
					myRvLinkCommandResponse = await ResendRunningCommandWaitForResponseAsync(commandContext.SentCommandId, cancelToken).ConfigureAwait(false);
					flag = myRvLinkCommandResponse != null;
				}
				if (flag)
				{
					commandContext.SentCommand(commandContext.SentCommandId, command.Command, command);
					return (myRvLinkCommandResponse is IMyRvLinkCommandResponseSuccess) ? new LevelerCommandResultType5(CommandResult.Completed) : ((myRvLinkCommandResponse is MyRvLinkCommandLeveler5ResponseFailure myRvLinkCommandLeveler5ResponseFailure) ? new LevelerCommandResultType5(CommandResult.ErrorOther, myRvLinkCommandLeveler5ResponseFailure.LevelerFault) : ((!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure)) ? new LevelerCommandResultType5(CommandResult.ErrorOther) : new LevelerCommandResultType5(CommandResult.ErrorOther)));
				}
				ushort commandId = GetNextCommandId();
				MyRvLinkCommandLeveler5 command2 = new MyRvLinkCommandLeveler5(commandId, myRvLinkDevice.Value.DeviceTableId, myRvLinkDevice.Value.DeviceId, command);
				commandContext.SentCommand(commandId, command.Command, command);
				IMyRvLinkCommandResponse myRvLinkCommandResponse2 = await SendCommandAsync(command2, cancelToken, TimeSpan.FromMilliseconds(2500.0), MyRvLinkSendCommandOption.WaitForAnyResponse).ConfigureAwait(false);
				if (!(myRvLinkCommandResponse2 is IMyRvLinkCommandResponseSuccess))
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
					if (!(myRvLinkCommandResponse2 is MyRvLinkCommandLeveler5ResponseFailure myRvLinkCommandLeveler5ResponseFailure2))
					{
						if (myRvLinkCommandResponse2 is IMyRvLinkCommandResponseFailure myRvLinkCommandResponseFailure)
						{
							commandContext.ProcessResponse(myRvLinkCommandResponse2);
							defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(37, 2);
							defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
							defaultInterpolatedStringHandler.AppendLiteral(" - Leveler 5 command failure generic ");
							defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseFailure);
							TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
							return new LevelerCommandResultType5(CommandResult.ErrorOther);
						}
						commandContext.ProcessResponse(new MyRvLinkCommandResponseFailure(commandId, MyRvLinkCommandResponseFailureCode.CommandFailed));
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(37, 2);
						defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
						defaultInterpolatedStringHandler.AppendLiteral(" - Leveler 5 command failure unknown ");
						defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse2);
						TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
						return new LevelerCommandResultType5(CommandResult.ErrorOther);
					}
					commandContext.ProcessResponse(myRvLinkCommandResponse2);
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 2);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" - Leveler 5 command failure ");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandLeveler5ResponseFailure2);
					TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
					return new LevelerCommandResultType5(CommandResult.ErrorOther, myRvLinkCommandLeveler5ResponseFailure2.LevelerFault);
				}
				commandContext.ProcessResponse(myRvLinkCommandResponse2);
				return new LevelerCommandResultType5(CommandResult.Completed);
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", LogPrefix + " Sending command failed " + ex.Message);
				return new LevelerCommandResultType5(CommandResult.ErrorOther);
			}
		}

		public Task<CommandResult> SendDirectCommandLightDimmable(ILogicalDeviceLightDimmable logicalDevice, LogicalDeviceLightDimmableCommand command, CancellationToken cancelToken)
		{
			LogicalDeviceLightDimmableCommand command2 = command;
			return SendCommandAsync(logicalDevice, ((byte DeviceTableId, byte DeviceId) myRvLinkDevice) => new MyRvLinkCommandActionDimmable(GetNextCommandId(), myRvLinkDevice.DeviceTableId, myRvLinkDevice.DeviceId, command2), cancelToken);
		}

		public Task<CommandResult> SendDirectCommandLightRgb(ILogicalDeviceLightRgb logicalDevice, LogicalDeviceLightRgbCommand command, CancellationToken cancelToken)
		{
			LogicalDeviceLightRgbCommand command2 = command;
			return SendCommandAsync(logicalDevice, ((byte DeviceTableId, byte DeviceId) myRvLinkDevice) => new MyRvLinkCommandActionRgb(GetNextCommandId(), myRvLinkDevice.DeviceTableId, myRvLinkDevice.DeviceId, command2), cancelToken);
		}

		public async Task<UInt48> PidReadAsync(ILogicalDevice logicalDevice, Pid pid, Action<float, string> readProgress, CancellationToken cancellationToken)
		{
			if (!IsStarted)
			{
				DirectConnectionMyRvLink myRvLink = this;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to Read PID value ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				throw new MyRvLinkDeviceServiceNotStartedException(myRvLink, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (!IsConnected)
			{
				DirectConnectionMyRvLink myRvLink2 = this;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to Read PID value ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				throw new MyRvLinkDeviceServiceNotConnectedException(myRvLink2, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				throw new MyRvLinkDeviceOfflineException(this, logicalDevice);
			}
			if (_firmwareUpdateInProgress)
			{
				throw new MyRvLinkPidReadException("Can't perform Pid reads while a firmware update is in progress!");
			}
			(byte DeviceTableId, byte DeviceId)? myRvLinkDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvLinkDevice.HasValue)
			{
				throw new MyRvLinkDeviceNotFoundException(this, logicalDevice);
			}
			using (await _pidSerialQueue.GetLock(cancellationToken))
			{
				int num = 100 - (int)_pidLastOperationTimer.Elapsed.TotalMilliseconds;
				if (num > 0)
				{
					await TaskExtension.TryDelay(num, cancellationToken);
				}
				cancellationToken.ThrowIfCancellationRequested();
				MyRvLinkCommandGetDevicePid command = new MyRvLinkCommandGetDevicePid(GetNextCommandId(), myRvLinkDevice.Value.DeviceTableId, myRvLinkDevice.Value.DeviceId, pid);
				IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 3);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" PidReadAsync Response ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse);
				defaultInterpolatedStringHandler.AppendLiteral(" Last operation was performed ");
				defaultInterpolatedStringHandler.AppendFormatted(_pidLastOperationTimer.ElapsedMilliseconds);
				defaultInterpolatedStringHandler.AppendLiteral("ms ago");
				TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				_pidLastOperationTimer.Restart();
				if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
				{
					if (myRvLinkCommandResponse is MyRvLinkCommandGetDevicePidResponseCompleted myRvLinkCommandGetDevicePidResponseCompleted)
					{
						if (pid.IsAutoCacheingPid())
						{
							logicalDevice.SetCachedPidRawValue(pid, myRvLinkCommandGetDevicePidResponseCompleted.PidValue);
						}
						return myRvLinkCommandGetDevicePidResponseCompleted.PidValue;
					}
					throw new MyRvLinkCommandResponseFailureException(new MyRvLinkCommandResponseFailure(command.ClientCommandId, MyRvLinkCommandResponseFailureCode.InvalidResponse));
				}
				throw new MyRvLinkCommandResponseFailureException(failure);
			}
		}

		public async Task PidWriteAsync(ILogicalDevice logicalDevice, Pid pid, UInt48 pidValue, LogicalDeviceSessionType pidWriteAccess, Action<float, string> writeProgress, CancellationToken cancellationToken)
		{
			if (!IsStarted)
			{
				DirectConnectionMyRvLink myRvLink = this;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to Write PID value ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				throw new MyRvLinkDeviceServiceNotStartedException(myRvLink, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (!IsConnected)
			{
				DirectConnectionMyRvLink myRvLink2 = this;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to Write PID value ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				throw new MyRvLinkDeviceServiceNotConnectedException(myRvLink2, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				throw new MyRvLinkDeviceOfflineException(this, logicalDevice);
			}
			if (_firmwareUpdateInProgress)
			{
				throw new MyRvLinkPidWriteException("Can't perform Pid writes while a firmware update is in progress!");
			}
			(byte DeviceTableId, byte DeviceId)? myRvLinkDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvLinkDevice.HasValue)
			{
				throw new MyRvLinkDeviceNotFoundException(this, logicalDevice);
			}
			using (await _pidSerialQueue.GetLockAsync(cancellationToken))
			{
				int num = 100 - (int)_pidLastOperationTimer.Elapsed.TotalMilliseconds;
				if (num > 0)
				{
					await TaskExtension.TryDelay(num, cancellationToken);
				}
				cancellationToken.ThrowIfCancellationRequested();
				SESSION_ID sessionId = pidWriteAccess.ToIdsCanSessionId();
				MyRvLinkCommandSetDevicePid command = new MyRvLinkCommandSetDevicePid(GetNextCommandId(), myRvLinkDevice.Value.DeviceTableId, myRvLinkDevice.Value.DeviceId, pid, sessionId, pidValue, pidWriteAccess);
				IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(60, 3);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" PidWriteAsync Response ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse);
				defaultInterpolatedStringHandler.AppendLiteral(" Last operation was performed ");
				defaultInterpolatedStringHandler.AppendFormatted(_pidLastOperationTimer.ElapsedMilliseconds);
				defaultInterpolatedStringHandler.AppendLiteral("ms ago");
				TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				_pidLastOperationTimer.Restart();
				if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
				{
					if (myRvLinkCommandResponse is MyRvLinkCommandSetDevicePidResponseCompleted myRvLinkCommandSetDevicePidResponseCompleted)
					{
						if (!myRvLinkCommandSetDevicePidResponseCompleted.IsCommandCompleted)
						{
							throw new MyRvLinkCommandResponseFailureException(new MyRvLinkCommandResponseFailure(command.ClientCommandId, MyRvLinkCommandResponseFailureCode.InvalidResponse));
						}
						if (pid.IsAutoCacheingPid())
						{
							logicalDevice.SetCachedPidRawValue(pid, myRvLinkCommandSetDevicePidResponseCompleted.PidValue);
						}
						return;
					}
					throw new MyRvLinkCommandResponseFailureException(new MyRvLinkCommandResponseFailure(command.ClientCommandId, MyRvLinkCommandResponseFailureCode.InvalidResponse));
				}
				throw new MyRvLinkCommandResponseFailureException(failure);
			}
		}

		public async Task<uint> PidReadAsync(ILogicalDevice logicalDevice, Pid pid, ushort address, Action<float, string> readProgress, CancellationToken cancellationToken)
		{
			if (!IsStarted)
			{
				DirectConnectionMyRvLink myRvLink = this;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(39, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to Read PID value ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				defaultInterpolatedStringHandler.AppendLiteral(" with Address ");
				defaultInterpolatedStringHandler.AppendFormatted(address);
				throw new MyRvLinkDeviceServiceNotStartedException(myRvLink, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (!IsConnected)
			{
				DirectConnectionMyRvLink myRvLink2 = this;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(39, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to Read PID value ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				defaultInterpolatedStringHandler.AppendLiteral(" with Address ");
				defaultInterpolatedStringHandler.AppendFormatted(address);
				throw new MyRvLinkDeviceServiceNotConnectedException(myRvLink2, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				throw new MyRvLinkDeviceOfflineException(this, logicalDevice);
			}
			if (_firmwareUpdateInProgress)
			{
				throw new MyRvLinkPidReadException("Can't perform Pid reads while a firmware update is in progress!");
			}
			(byte DeviceTableId, byte DeviceId)? myRvLinkDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvLinkDevice.HasValue)
			{
				throw new MyRvLinkDeviceNotFoundException(this, logicalDevice);
			}
			using (await _pidSerialQueue.GetLock(cancellationToken))
			{
				int num = 100 - (int)_pidLastOperationTimer.Elapsed.TotalMilliseconds;
				if (num > 0)
				{
					await TaskExtension.TryDelay(num, cancellationToken);
				}
				cancellationToken.ThrowIfCancellationRequested();
				MyRvLinkCommandGetDevicePidWithAddress command = new MyRvLinkCommandGetDevicePidWithAddress(GetNextCommandId(), myRvLinkDevice.Value.DeviceTableId, myRvLinkDevice.Value.DeviceId, pid, address);
				IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 3);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" PidReadAsync Response ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse);
				defaultInterpolatedStringHandler.AppendLiteral(" Last operation was performed ");
				defaultInterpolatedStringHandler.AppendFormatted(_pidLastOperationTimer.ElapsedMilliseconds);
				defaultInterpolatedStringHandler.AppendLiteral("ms ago");
				TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				_pidLastOperationTimer.Restart();
				if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
				{
					if (myRvLinkCommandResponse is MyRvLinkCommandGetDevicePidWithAddressResponseCompleted myRvLinkCommandGetDevicePidWithAddressResponseCompleted)
					{
						return myRvLinkCommandGetDevicePidWithAddressResponseCompleted.PidValue;
					}
					throw new MyRvLinkCommandResponseFailureException(new MyRvLinkCommandResponseFailure(command.ClientCommandId, MyRvLinkCommandResponseFailureCode.InvalidResponse));
				}
				throw new MyRvLinkCommandResponseFailureException(failure);
			}
		}

		public async Task PidWriteAsync(ILogicalDevice logicalDevice, Pid pid, ushort address, uint pidValue, LogicalDeviceSessionType pidWriteAccess, Action<float, string> writeProgress, CancellationToken cancellationToken)
		{
			if (!IsStarted)
			{
				DirectConnectionMyRvLink myRvLink = this;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to Write PID value ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				defaultInterpolatedStringHandler.AppendLiteral(" with Address ");
				defaultInterpolatedStringHandler.AppendFormatted(address);
				throw new MyRvLinkDeviceServiceNotStartedException(myRvLink, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (!IsConnected)
			{
				DirectConnectionMyRvLink myRvLink2 = this;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to Write PID value ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				defaultInterpolatedStringHandler.AppendLiteral(" with Address ");
				defaultInterpolatedStringHandler.AppendFormatted(address);
				throw new MyRvLinkDeviceServiceNotConnectedException(myRvLink2, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				throw new MyRvLinkDeviceOfflineException(this, logicalDevice);
			}
			if (_firmwareUpdateInProgress)
			{
				throw new MyRvLinkPidWriteException("Can't perform Pid writes while a firmware update is in progress!");
			}
			(byte DeviceTableId, byte DeviceId)? myRvLinkDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvLinkDevice.HasValue)
			{
				throw new MyRvLinkDeviceNotFoundException(this, logicalDevice);
			}
			using (await _pidSerialQueue.GetLock(cancellationToken))
			{
				int num = 100 - (int)_pidLastOperationTimer.Elapsed.TotalMilliseconds;
				if (num > 0)
				{
					await TaskExtension.TryDelay(num, cancellationToken);
				}
				cancellationToken.ThrowIfCancellationRequested();
				SESSION_ID sessionId = pidWriteAccess.ToIdsCanSessionId();
				MyRvLinkCommandSetDevicePidWithAddress command = new MyRvLinkCommandSetDevicePidWithAddress(GetNextCommandId(), myRvLinkDevice.Value.DeviceTableId, myRvLinkDevice.Value.DeviceId, pid, sessionId, address, pidValue, pidWriteAccess);
				IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(60, 3);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" PidWriteAsync Response ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse);
				defaultInterpolatedStringHandler.AppendLiteral(" Last operation was performed ");
				defaultInterpolatedStringHandler.AppendFormatted(_pidLastOperationTimer.ElapsedMilliseconds);
				defaultInterpolatedStringHandler.AppendLiteral("ms ago");
				TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				_pidLastOperationTimer.Restart();
				if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
				{
					if (myRvLinkCommandResponse is MyRvLinkCommandSetDevicePidWithAddressResponseCompleted myRvLinkCommandSetDevicePidWithAddressResponseCompleted)
					{
						if (myRvLinkCommandSetDevicePidWithAddressResponseCompleted.IsCommandCompleted)
						{
							return;
						}
						throw new MyRvLinkCommandResponseFailureException(new MyRvLinkCommandResponseFailure(command.ClientCommandId, MyRvLinkCommandResponseFailureCode.InvalidResponse));
					}
					throw new MyRvLinkCommandResponseFailureException(new MyRvLinkCommandResponseFailure(command.ClientCommandId, MyRvLinkCommandResponseFailureCode.InvalidResponse));
				}
				throw new MyRvLinkCommandResponseFailureException(failure);
			}
		}

		public async Task<IReadOnlyDictionary<Pid, PidAccess>> GetDevicePidListAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken, Pid startPidId = Pid.Unknown, Pid endPidId = Pid.Unknown)
		{
			if (!IsStarted)
			{
				throw new MyRvLinkDeviceServiceNotStartedException(this, "Unable to Get PID List");
			}
			if (!IsConnected)
			{
				throw new MyRvLinkDeviceServiceNotConnectedException(this, "Unable to Get PID List");
			}
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				throw new MyRvLinkDeviceOfflineException(this, logicalDevice);
			}
			(byte, byte)? myRvDeviceFromLogicalDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvDeviceFromLogicalDevice.HasValue)
			{
				throw new MyRvLinkDeviceNotFoundException(this, logicalDevice);
			}
			MyRvLinkCommandGetDevicePidList command = new MyRvLinkCommandGetDevicePidList(GetNextCommandId(), myRvDeviceFromLogicalDevice.Value.Item1, myRvDeviceFromLogicalDevice.Value.Item2, startPidId, endPidId);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
			if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
			{
				if (myRvLinkCommandResponse is MyRvLinkCommandGetDevicePidListResponseCompleted)
				{
					return command.PidDict;
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to Get PID Values from ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(": Unknown result");
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			throw new MyRvLinkCommandResponseFailureException(failure);
		}

		public Task<string> GetSoftwarePartNumberAsync(ILogicalDevice logicalDevice, CancellationToken cancelToken)
		{
			IMyRvLinkDeviceForLogicalDevice myRvLinkDeviceForLogicalDevice = _deviceTracker?.GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!(myRvLinkDeviceForLogicalDevice is MyRvLinkDeviceHost myRvLinkDeviceHost))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
				if (myRvLinkDeviceForLogicalDevice is MyRvLinkDeviceIdsCan myRvLinkDeviceIdsCan)
				{
					string obj = myRvLinkDeviceIdsCan.MetaData?.SoftwarePartNumber;
					if (obj == null)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Metadata not loaded for ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
						throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					return Task.FromResult(obj);
				}
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 1);
				defaultInterpolatedStringHandler.AppendLiteral("IDS CAN device not found for ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			string obj2 = myRvLinkDeviceHost.MetaData?.SoftwarePartNumber;
			if (obj2 == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Metadata not loaded for ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return Task.FromResult(obj2);
		}

		public Version? GetDeviceProtocolVersion(ILogicalDevice logicalDevice)
		{
			IMyRvLinkDeviceForLogicalDevice myRvLinkDeviceForLogicalDevice = _deviceTracker?.GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!(myRvLinkDeviceForLogicalDevice is MyRvLinkDeviceHost myRvLinkDeviceHost))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
				if (myRvLinkDeviceForLogicalDevice is MyRvLinkDeviceIdsCan myRvLinkDeviceIdsCan)
				{
					MyRvLinkDeviceIdsCanMetadata? metaData = myRvLinkDeviceIdsCan.MetaData;
					if (metaData == null)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Metadata not loaded for ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
						throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					IDS_CAN_VERSION_NUMBER iDS_CAN_VERSION_NUMBER = metaData!.IdsCanVersion;
					return new Version(iDS_CAN_VERSION_NUMBER.Major, iDS_CAN_VERSION_NUMBER.Minor);
				}
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 1);
				defaultInterpolatedStringHandler.AppendLiteral("IDS CAN device not found for ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			MyRvLinkDeviceHostMetadata? metaData2 = myRvLinkDeviceHost.MetaData;
			if (metaData2 == null)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Metadata not loaded for ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			IDS_CAN_VERSION_NUMBER iDS_CAN_VERSION_NUMBER2 = metaData2!.IdsCanVersion;
			return new Version(iDS_CAN_VERSION_NUMBER2.Major, iDS_CAN_VERSION_NUMBER2.Minor);
		}

		public async Task<IReadOnlyDictionary<DTC_ID, DtcValue>> GetDtcValuesAsync(ILogicalDevice logicalDevice, LogicalDeviceDtcFilter dtcFilter, DTC_ID startDtcId, DTC_ID endDtcId, CancellationToken cancellationToken)
		{
			if (!IsStarted)
			{
				DirectConnectionMyRvLink myRvLink = this;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to Get DTC value ");
				defaultInterpolatedStringHandler.AppendFormatted(startDtcId);
				defaultInterpolatedStringHandler.AppendLiteral(" - ");
				defaultInterpolatedStringHandler.AppendFormatted(endDtcId);
				throw new MyRvLinkDeviceServiceNotStartedException(myRvLink, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (!IsConnected)
			{
				DirectConnectionMyRvLink myRvLink2 = this;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to Get DTC value ");
				defaultInterpolatedStringHandler.AppendFormatted(startDtcId);
				defaultInterpolatedStringHandler.AppendLiteral(" - ");
				defaultInterpolatedStringHandler.AppendFormatted(endDtcId);
				throw new MyRvLinkDeviceServiceNotConnectedException(myRvLink2, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				throw new MyRvLinkDeviceOfflineException(this, logicalDevice);
			}
			(byte DeviceTableId, byte DeviceId)? myRvLinkDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvLinkDevice.HasValue)
			{
				throw new MyRvLinkDeviceNotFoundException(this, logicalDevice);
			}
			using (await _dtcSerialQueue.GetLock(cancellationToken))
			{
				if (_dtcThrottleStopwatch.IsRunning && _dtcThrottleStopwatch.ElapsedMilliseconds < 500)
				{
					long num = 500 - _dtcThrottleStopwatch.ElapsedMilliseconds;
					if (num > 0)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(39, 2);
						defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
						defaultInterpolatedStringHandler.AppendLiteral(" DTC Get Value Request Throttled for ");
						defaultInterpolatedStringHandler.AppendFormatted(num);
						defaultInterpolatedStringHandler.AppendLiteral("ms");
						TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
						await Task.Delay((int)num, cancellationToken);
					}
				}
				_dtcThrottleStopwatch.Stop();
				MyRvLinkCommandGetProductDtcValues command = new MyRvLinkCommandGetProductDtcValues(GetNextCommandId(), myRvLinkDevice.Value.DeviceTableId, myRvLinkDevice.Value.DeviceId, dtcFilter, startDtcId, endDtcId);
				IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
				if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure myRvLinkCommandResponseFailure))
				{
					if (myRvLinkCommandResponse is MyRvLinkCommandGetProductDtcValuesResponseCompleted)
					{
						return command.DtcDict;
					}
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Failed to Get DTC Values from ");
					defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
					defaultInterpolatedStringHandler.AppendLiteral(": Unknown result");
					throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				if (myRvLinkCommandResponseFailure.FailureCode == MyRvLinkCommandResponseFailureCode.TooManyCommandsRunning)
				{
					_dtcThrottleStopwatch.Restart();
				}
				throw new MyRvLinkCommandResponseFailureException(myRvLinkCommandResponseFailure);
			}
		}

		protected void OnReceivedEvent(IMyRvLinkEvent myRvLinkEvent)
		{
			if (myRvLinkEvent == null)
			{
				throw new ArgumentNullException("myRvLinkEvent");
			}
			if (!IsStarted)
			{
				throw new MyRvLinkDeviceServiceNotStartedException(this, "OnReceivedEvent called when service is stopped");
			}
			if (!IsConnected)
			{
				throw new MyRvLinkDeviceServiceNotConnectedException(this, "OnReceivedEvent called when connection isn't open");
			}
			_receivedEventMetrics.Update();
			UpdateFrequencyMetricForEvent(myRvLinkEvent.EventType);
			MyRvLinkVersionTracker versionTracker = _versionTracker;
			if (versionTracker == null)
			{
				throw new MyRvLinkException("OnReceivedEvent No Version Tracker Setup!");
			}
			versionTracker.GetVersionIfNeeded();
			if (!(myRvLinkEvent is MyRvLinkGatewayInformation gatewayInfo))
			{
				if (!(myRvLinkEvent is MyRvLinkRealTimeClock myRvLinkRealTimeClock))
				{
					if (!(myRvLinkEvent is MyRvLinkRvStatus rvStatus))
					{
						if (!(myRvLinkEvent is MyRvLinkHostDebug))
						{
							if (myRvLinkEvent is IMyRvLinkCommandEvent myRvLinkCommandEvent)
							{
								if (!HasMinimumExpectedProtocolVersion)
								{
									return;
								}
								ushort commandId = myRvLinkCommandEvent.ClientCommandId;
								if (!_commandActiveDict.TryGetValue(commandId, out var value))
								{
									MyRvLinkCommandTracker myRvLinkCommandTracker = Enumerable.FirstOrDefault(_commandCompletedQueue, (MyRvLinkCommandTracker commandTracker) => commandTracker.Command.ClientCommandId == commandId);
									if (myRvLinkCommandTracker != null)
									{
										DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(98, 6);
										defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
										defaultInterpolatedStringHandler.AppendLiteral(" Received event for command 0x");
										defaultInterpolatedStringHandler.AppendFormatted(commandId, "X4");
										defaultInterpolatedStringHandler.AppendLiteral(" that no longer exists.  Discarding event");
										defaultInterpolatedStringHandler.AppendFormatted(Environment.NewLine);
										defaultInterpolatedStringHandler.AppendLiteral("Command: ");
										defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandTracker);
										defaultInterpolatedStringHandler.AppendFormatted(Environment.NewLine);
										defaultInterpolatedStringHandler.AppendLiteral(" Event Discarded: ");
										defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandEvent);
										TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
									}
									else
									{
										DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(91, 4);
										defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
										defaultInterpolatedStringHandler.AppendLiteral(" Received event for command 0x");
										defaultInterpolatedStringHandler.AppendFormatted(commandId, "X4");
										defaultInterpolatedStringHandler.AppendLiteral(" that no longer exists.  Discarding event for UNKNOWN COMMAND");
										defaultInterpolatedStringHandler.AppendFormatted(Environment.NewLine);
										defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandEvent);
										TaggedLog.Warning("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
									}
								}
								else if (myRvLinkCommandEvent is IMyRvLinkCommandResponse myRvLinkCommandResponse)
								{
									if (value.IsCompleted)
									{
										DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(81, 4);
										defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
										defaultInterpolatedStringHandler.AppendLiteral(" Received event for command 0x");
										defaultInterpolatedStringHandler.AppendFormatted(commandId, "X");
										defaultInterpolatedStringHandler.AppendLiteral(" that has already been completed.  Discarding event");
										defaultInterpolatedStringHandler.AppendFormatted(Environment.NewLine);
										defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse);
										TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
										return;
									}
									if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure))
									{
										if (!(myRvLinkCommandResponse is MyRvLinkCommandGetProductDtcValuesResponse) && !(myRvLinkCommandResponse is MyRvLinkCommandGetDevicePidResponseCompleted))
										{
											if (myRvLinkCommandResponse is IMyRvLinkCommandResponseSuccess myRvLinkCommandResponseSuccess)
											{
												DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(49, 6);
												defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
												defaultInterpolatedStringHandler.AppendLiteral(" Event Received Success: Command(0x");
												defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse.ClientCommandId, "X4");
												defaultInterpolatedStringHandler.AppendLiteral(") ");
												defaultInterpolatedStringHandler.AppendFormatted(myRvLinkEvent.GetType().Name);
												defaultInterpolatedStringHandler.AppendLiteral(" Completed: ");
												defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse.IsCommandCompleted);
												defaultInterpolatedStringHandler.AppendFormatted(Environment.NewLine);
												defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponseSuccess);
												TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
											}
											else
											{
												TaggedLog.Error("DirectConnectionMyRvLink", LogPrefix + " Event Received Other: " + myRvLinkEvent.GetType().Name);
											}
										}
									}
									else
									{
										DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 2);
										defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
										defaultInterpolatedStringHandler.AppendLiteral(" Event Received FAILURE: ");
										defaultInterpolatedStringHandler.AppendFormatted(myRvLinkEvent);
										TaggedLog.Warning("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
										UpdateFrequencyMetricForCommandFailure(value.Command.CommandType);
									}
									bool flag = value.Command.ProcessResponse(myRvLinkCommandResponse);
									value.ProcessResponse(myRvLinkCommandResponse, flag);
									if (flag || myRvLinkCommandEvent.IsCommandCompleted)
									{
										FlushCompletedCommands();
									}
								}
								else
								{
									DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 3);
									defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
									defaultInterpolatedStringHandler.AppendLiteral(" Event Received for unknown Command Event ");
									defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandEvent);
									defaultInterpolatedStringHandler.AppendLiteral(": ");
									defaultInterpolatedStringHandler.AppendFormatted(myRvLinkEvent);
									TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
								}
							}
							else
							{
								if (!versionTracker.IsVersionSupported)
								{
									return;
								}
								MyRvLinkDeviceTracker deviceTracker = _deviceTracker;
								if (deviceTracker == null || !deviceTracker.IsDeviceLoadComplete || !deviceTracker.IsActive || myRvLinkEvent is MyRvLinkGatewayInformation || myRvLinkEvent is IMyRvLinkCommandEvent || myRvLinkEvent is MyRvLinkRealTimeClock || myRvLinkEvent is MyRvLinkRvStatus || myRvLinkEvent is MyRvLinkHostDebug)
								{
									return;
								}
								if (!(myRvLinkEvent is MyRvLinkDeviceOnlineStatus deviceOnlineStatus))
								{
									if (!(myRvLinkEvent is MyRvLinkDeviceLockStatus deviceLockStatus))
									{
										if (!(myRvLinkEvent is MyRvLinkRelayBasicLatchingStatusType1 latchingRelayStatus))
										{
											if (!(myRvLinkEvent is MyRvLinkRelayBasicLatchingStatusType2 latchingRelayStatus2))
											{
												if (!(myRvLinkEvent is MyRvLinkRelayHBridgeMomentaryStatusType1 momentaryRelayStatus))
												{
													if (!(myRvLinkEvent is MyRvLinkRelayHBridgeMomentaryStatusType2 momentaryRelayStatus2))
													{
														if (!(myRvLinkEvent is MyRvLinkTankSensorStatus tankSensorStatus))
														{
															if (!(myRvLinkEvent is MyRvLinkTankSensorStatusV2 tankSensorStatus2))
															{
																if (!(myRvLinkEvent is MyRvLinkDimmableLightStatus dimmableLightStatus))
																{
																	if (!(myRvLinkEvent is MyRvLinkRgbLightStatus rgbLightStatus))
																	{
																		if (!(myRvLinkEvent is MyRvLinkHvacStatus hvacStatus))
																		{
																			if (!(myRvLinkEvent is MyRvLinkHourMeterStatus tankSensorStatus3))
																			{
																				if (!(myRvLinkEvent is MyRvLinkGeneratorGenieStatus generatorGenieStatus))
																				{
																					if (!(myRvLinkEvent is MyRvLinkCloudGatewayStatus cloudGatewayStatus))
																					{
																						if (!(myRvLinkEvent is MyRvLinkLeveler4Status levelerStatus))
																						{
																							if (!(myRvLinkEvent is MyRvLinkLeveler5Status levelerStatus2))
																							{
																								if (!(myRvLinkEvent is MyRvLinkLevelerType5ExtendedStatus progressStatus))
																								{
																									if (!(myRvLinkEvent is MyRvLinkLevelerConsoleText levelerConsoleText))
																									{
																										if (!(myRvLinkEvent is MyRvLinkLeveler3Status levelerStatus3))
																										{
																											if (!(myRvLinkEvent is MyRvLinkLeveler1Status levelerStatus4))
																											{
																												if (!(myRvLinkEvent is MyRvLinkTemperatureSensorStatus temperatureSensorStatus))
																												{
																													if (!(myRvLinkEvent is MyRvLinkJaycoTbbStatus jaycoTbbStatus))
																													{
																														if (!(myRvLinkEvent is MyRvLinkMonitorPanelStatus monitorPanelStatus))
																														{
																															if (!(myRvLinkEvent is MyRvLinkDeviceSessionStatus deviceOnlineStatus2))
																															{
																																if (!(myRvLinkEvent is MyRvLinkAwningSensorStatus awningSensorStatus))
																																{
																																	if (!(myRvLinkEvent is MyRvLinkAccessoryGatewayStatus accessoryGatewayStatus))
																																	{
																																		if (!(myRvLinkEvent is MyRvLinkBrakingSystemStatus absStatus))
																																		{
																																			if (!(myRvLinkEvent is MyRvLinkBatteryMonitorStatus batteryMonitorStatus))
																																			{
																																				if (!(myRvLinkEvent is MyRvLinkBootLoaderStatus bootLoaderStatus))
																																				{
																																					if (!(myRvLinkEvent is MyRvLinkDoorLockStatus doorLockStatus))
																																					{
																																						if (myRvLinkEvent is MyRvLinkDimmableLightStatusExtended dimmableLightStatusExtended)
																																						{
																																							deviceTracker.UpdateDimmableLightExtended(dimmableLightStatusExtended);
																																							return;
																																						}
																																						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 3);
																																						defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
																																						defaultInterpolatedStringHandler.AppendLiteral(" Received Unhandled Event is being ignored");
																																						defaultInterpolatedStringHandler.AppendFormatted(Environment.NewLine);
																																						defaultInterpolatedStringHandler.AppendLiteral(" ");
																																						defaultInterpolatedStringHandler.AppendFormatted(myRvLinkEvent);
																																						TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
																																					}
																																					else
																																					{
																																						deviceTracker.UpdateDoorLockStatus(doorLockStatus);
																																					}
																																				}
																																				else
																																				{
																																					deviceTracker.UpdateBootLoaderStatus(bootLoaderStatus);
																																				}
																																			}
																																			else
																																			{
																																				deviceTracker.UpdateBatteryMonitorStatus(batteryMonitorStatus);
																																			}
																																		}
																																		else
																																		{
																																			deviceTracker.UpdateBrakingSystemStatus(absStatus);
																																		}
																																	}
																																	else
																																	{
																																		deviceTracker.UpdateAccessoryGatewayStatus(accessoryGatewayStatus);
																																	}
																																}
																																else
																																{
																																	deviceTracker.UpdateAwningSensorStatus(awningSensorStatus);
																																}
																															}
																															else
																															{
																																deviceTracker.UpdateSessionStatus(deviceOnlineStatus2);
																															}
																														}
																														else
																														{
																															deviceTracker.UpdateMonitorPanelStatus(monitorPanelStatus);
																														}
																													}
																													else
																													{
																														deviceTracker.UpdateJaycoTbbStatus(jaycoTbbStatus);
																													}
																												}
																												else
																												{
																													deviceTracker.UpdateTemperatureSensorStatus(temperatureSensorStatus);
																												}
																											}
																											else
																											{
																												deviceTracker.UpdateLeveler1Status(levelerStatus4);
																											}
																										}
																										else
																										{
																											deviceTracker.UpdateLeveler3Status(levelerStatus3);
																										}
																									}
																									else
																									{
																										deviceTracker.UpdateLevelerConsoleText(levelerConsoleText);
																									}
																								}
																								else
																								{
																									deviceTracker.UpdateAutoOperationProgressStatus(progressStatus);
																								}
																							}
																							else
																							{
																								deviceTracker.UpdateLeveler5Status(levelerStatus2);
																							}
																						}
																						else
																						{
																							deviceTracker.UpdateLeveler4Status(levelerStatus);
																						}
																					}
																					else
																					{
																						deviceTracker.UpdateCloudGatewayStatus(cloudGatewayStatus);
																					}
																				}
																				else
																				{
																					deviceTracker.UpdateGeneratorGenieStatus(generatorGenieStatus);
																				}
																			}
																			else
																			{
																				deviceTracker.UpdateHourMeterStatus(tankSensorStatus3);
																			}
																		}
																		else
																		{
																			deviceTracker.UpdateHvacStatus(hvacStatus);
																		}
																	}
																	else
																	{
																		deviceTracker.UpdateRgbLightStatus(rgbLightStatus);
																	}
																}
																else
																{
																	_deviceTracker?.UpdateDimmableLightStatus(dimmableLightStatus);
																}
															}
															else
															{
																deviceTracker.UpdateTankSensorStatusV2(tankSensorStatus2);
															}
														}
														else
														{
															deviceTracker.UpdateTankSensorStatus(tankSensorStatus);
														}
													}
													else
													{
														deviceTracker.UpdateRelayHBridgeMomentaryStatus(momentaryRelayStatus2);
													}
												}
												else
												{
													deviceTracker.UpdateRelayHBridgeMomentaryStatus(momentaryRelayStatus);
												}
											}
											else
											{
												deviceTracker.UpdateRelayBasicLatchingStatus(latchingRelayStatus2);
											}
										}
										else
										{
											deviceTracker.UpdateRelayBasicLatchingStatus(latchingRelayStatus);
										}
									}
									else
									{
										deviceTracker.UpdateLockStatus(deviceLockStatus);
									}
								}
								else
								{
									deviceTracker.UpdateOnlineStatus(deviceOnlineStatus);
								}
							}
						}
						else
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 2);
							defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
							defaultInterpolatedStringHandler.AppendLiteral(" Event Received: ");
							defaultInterpolatedStringHandler.AppendFormatted(myRvLinkEvent);
							TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
						}
					}
					else if (HasMinimumExpectedProtocolVersion)
					{
						UpdateRvStatus(rvStatus);
					}
				}
				else if (HasMinimumExpectedProtocolVersion)
				{
					RealTimeClock = myRvLinkRealTimeClock.DateTime;
				}
			}
			else
			{
				GatewayInfo = gatewayInfo;
				_deviceTracker?.GetDevicesIfNeeded();
				_deviceTracker?.GetDevicesMetadataIfNeeded();
			}
		}

		public async Task<FirmwareUpdateSupport> TryGetFirmwareUpdateSupportAsync(ILogicalDevice logicalDevice, CancellationToken cancelToken)
		{
			_ = 1;
			try
			{
				if (!IsStarted)
				{
					throw new MyRvLinkDeviceServiceNotStartedException(this, "Device Source Not Started");
				}
				if (!IsConnected)
				{
					throw new MyRvLinkDeviceServiceNotConnectedException(this, "Device Source Not Connected");
				}
				if (logicalDevice is ILocapOtaAccessoryDevice)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(102, 1);
					defaultInterpolatedStringHandler.AppendLiteral("ILocapOtaAccessory device found, updating not supported through RvLink Device Source. Logical device: ");
					defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
					TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
					return FirmwareUpdateSupport.NotSupported;
				}
				if (!IsLogicalDeviceOnline(logicalDevice))
				{
					return FirmwareUpdateSupport.DeviceOffline;
				}
				if (logicalDevice is ILogicalDeviceJumpToBootloader logicalDeviceJumpToBootloader && logicalDeviceJumpToBootloader.IsJumpToBootRequiredForFirmwareUpdate)
				{
					return FirmwareUpdateSupport.SupportedViaBootloader;
				}
				if (!Enumerable.Contains(await GetDeviceBlockListAsync(logicalDevice, cancelToken), BlockTransferBlockId.Reflash))
				{
					return FirmwareUpdateSupport.NotSupported;
				}
				BlockTransferPropertyFlags blockTransferPropertyFlags = await GetDeviceBlockPropertyFlagsAsync(logicalDevice, BlockTransferBlockId.Reflash, cancelToken);
				if (!blockTransferPropertyFlags.HasFlag(BlockTransferPropertyFlags.RequiresStartAddress))
				{
					return FirmwareUpdateSupport.NotSupported;
				}
				if (!blockTransferPropertyFlags.HasFlag(BlockTransferPropertyFlags.RequiresSize))
				{
					return FirmwareUpdateSupport.NotSupported;
				}
				return FirmwareUpdateSupport.SupportedViaDevice;
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", "Unable to determine if firmware update is supported: " + ex.Message);
				return FirmwareUpdateSupport.Unknown;
			}
		}

		public async Task UpdateFirmwareAsync(ILogicalDeviceFirmwareUpdateSession firmwareUpdateSession, IReadOnlyList<byte> data, Func<ILogicalDeviceTransferProgress, bool> progressAck, CancellationToken cancellationToken, IReadOnlyDictionary<FirmwareUpdateOption, object>? options = null)
		{
			if (!IsStarted)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", "Unable to Update Firmware because Logical Device Service Not Started DirectConnectionMyRvLink");
				throw new MyRvLinkDeviceServiceNotStartedException(this, "Unable to Update Firmware");
			}
			if (!IsConnected)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", "Unable to Update Firmware because Logical Device Service Not Connected DirectConnectionMyRvLink");
				throw new MyRvLinkDeviceServiceNotConnectedException(this, "Unable to Update Firmware");
			}
			if (firmwareUpdateSession.IsDisposed)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", "Unable to Update Firmware because update session is disposed DirectConnectionMyRvLink");
				throw new FirmwareUpdateSessionDisposedException();
			}
			ILogicalDeviceFirmwareUpdateDevice logicalDeviceFirmwareUpdateDevice = firmwareUpdateSession.LogicalDevice;
			if (options == null)
			{
				options = new Dictionary<FirmwareUpdateOption, object>();
			}
			if (logicalDeviceFirmwareUpdateDevice is ILogicalDeviceJumpToBootloader jumpToBootLogicalDevice && jumpToBootLogicalDevice.IsJumpToBootRequiredForFirmwareUpdate)
			{
				if (options.IsDeviceAuthorizationRequired())
				{
					await FirmwareUpdateAuthorizationAsync(logicalDeviceFirmwareUpdateDevice, cancellationToken);
				}
				if (!options.TryGetJumpToBootHoldTime(out var holdTime))
				{
					holdTime = TimeSpan.FromMilliseconds(10000.0);
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(72, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Jump to boot time not specified but required so using a default time of ");
					defaultInterpolatedStringHandler.AppendFormatted(holdTime);
					TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				logicalDeviceFirmwareUpdateDevice = await JumpToBootloaderAsync(jumpToBootLogicalDevice, holdTime, cancellationToken);
			}
			await UpdateFirmwareInternalAsync(logicalDeviceFirmwareUpdateDevice, data, progressAck, cancellationToken, options);
		}

		private async Task UpdateFirmwareInternalAsync(ILogicalDeviceFirmwareUpdateDevice logicalDeviceToReflash, IReadOnlyList<byte> data, Func<ILogicalDeviceTransferProgress, bool> progressAck, CancellationToken cancellationToken, IReadOnlyDictionary<FirmwareUpdateOption, object> options)
		{
			_ = 6;
			try
			{
				if (logicalDeviceToReflash is ILogicalDeviceJumpToBootloader logicalDeviceJumpToBootloader && logicalDeviceJumpToBootloader.IsJumpToBootRequiredForFirmwareUpdate)
				{
					throw new ArgumentException("Given logical device must be in bootloader mode for re-flashing", "logicalDeviceToReflash");
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
				if (logicalDeviceToReflash.LogicalId.ProductId == PRODUCT_ID.CAN_RE_FLASH_BOOTLOADER)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(66, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Device not configured properly as product id is being reported as ");
					defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceToReflash.LogicalId.ProductId);
					throw new FirmwareUpdateBootloaderException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				if (!options.TryGetStartAddress(out var startAddress))
				{
					throw new FirmwareUpdateMissingRequiredOptionException(logicalDeviceToReflash, FirmwareUpdateOption.StartAddress);
				}
				if (!IsLogicalDeviceOnline(logicalDeviceToReflash))
				{
					throw new MyRvLinkDeviceOfflineException(this, logicalDeviceToReflash);
				}
				FirmwareUpdateSupport firmwareUpdateSupport = await TryGetFirmwareUpdateSupportAsync(logicalDeviceToReflash, cancellationToken);
				if (firmwareUpdateSupport != FirmwareUpdateSupport.SupportedViaDevice)
				{
					throw new FirmwareUpdateNotSupportedException(logicalDeviceToReflash, firmwareUpdateSupport);
				}
				if (options.IsDeviceAuthorizationRequired())
				{
					await FirmwareUpdateAuthorizationAsync(logicalDeviceToReflash, cancellationToken);
				}
				BlockTransferStartOptions blockTransferStartOptions = BlockTransferStartOptions.Write | BlockTransferStartOptions.StartAddress | BlockTransferStartOptions.Size | BlockTransferStartOptions.Erase;
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(47, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Block Transfer Starting: ");
				defaultInterpolatedStringHandler.AppendFormatted(3);
				defaultInterpolatedStringHandler.AppendLiteral(", ");
				defaultInterpolatedStringHandler.AppendFormatted(blockTransferStartOptions);
				defaultInterpolatedStringHandler.AppendLiteral(", Address: 0x");
				defaultInterpolatedStringHandler.AppendFormatted(startAddress, "X");
				defaultInterpolatedStringHandler.AppendLiteral(" Size: ");
				defaultInterpolatedStringHandler.AppendFormatted(data.Count);
				TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				await StartDeviceBlockTransferAsync(logicalDeviceToReflash, BlockTransferBlockId.Reflash, blockTransferStartOptions, cancellationToken, startAddress, (uint)data.Count);
				TaggedLog.Information("DirectConnectionMyRvLink", "Block Transfer In Progress");
				await DeviceBlockWriteAsync(logicalDeviceToReflash, BlockTransferBlockId.Reflash, data, progressAck, cancellationToken);
				if (options.IsDeviceAuthorizationRequired())
				{
					await FirmwareUpdateAuthorizationAsync(logicalDeviceToReflash, cancellationToken);
				}
				BlockTransferStopOptions options2 = BlockTransferStopOptions.Write | BlockTransferStopOptions.Reset;
				TaggedLog.Information("DirectConnectionMyRvLink", "Block Transfer Ending");
				await StopDeviceBlockTransferAsync(logicalDeviceToReflash, BlockTransferBlockId.Reflash, options2, cancellationToken);
				if ((byte)logicalDeviceToReflash.LogicalId.DeviceType == 50 && !(await TryRemoveRefreshBootLoaderWhenOfflineAsync(logicalDeviceToReflash, cancellationToken)))
				{
					TaggedLog.Error("DirectConnectionMyRvLink", logicalDeviceToReflash?.DeviceName + " is not removed.");
				}
			}
			catch (BlockTransferNotSupportedException ex)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", "Unable to Update Firmware Block Transfer Not Supported: " + ex.Message);
				throw new FirmwareUpdateNotSupportedException(logicalDeviceToReflash, FirmwareUpdateSupport.Unknown, ex);
			}
			catch (BlockTransferBlockTooSmallException ex2)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", "Unable to Update Firmware Block Transfer Size Too Small: " + ex2.Message);
				throw new FirmwareUpdateTooSmallException(logicalDeviceToReflash, data.Count, ex2);
			}
			catch (BlockTransferBlockTooBigException ex3)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", "Unable to Update Firmware Block Transfer Size Too Big: " + ex3.Message);
				throw new FirmwareUpdateTooBigException(logicalDeviceToReflash, data.Count, ex3);
			}
			catch (BlockTransferWriteFailedException ex4)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", "Unable to Update Firmware Block Transfer Write Failed: " + ex4.Message);
				throw new FirmwareUpdateFailedException(logicalDeviceToReflash, ex4.Progress, ex4);
			}
			catch (FirmwareUpdateNotAuthorizedException ex5)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", "Unable to Update Firmware Block Transfer Not Authorized: " + ex5.Message);
				throw;
			}
			catch (FirmwareUpdateException ex6)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", "Unable to Update Firmware " + ex6.Message);
				throw;
			}
			catch (Exception ex7)
			{
				TaggedLog.Error("DirectConnectionMyRvLink", "Unable to Update Firmware " + ex7.Message);
				throw new BlockTransferException("Unable to Update Firmware", ex7);
			}
		}

		public async Task<ILogicalDeviceReflashBootloader> JumpToBootloaderAsync(ILogicalDeviceJumpToBootloader logicalDevice, TimeSpan holdTime, CancellationToken cancellationToken)
		{
			if (logicalDevice is ILogicalDeviceReflashBootloader result)
			{
				return result;
			}
			ILogicalDeviceReflashBootloader associatedLogicalDeviceBootloader = GetAssociatedLogicalDeviceBootloader(logicalDevice);
			if (associatedLogicalDeviceBootloader != null && associatedLogicalDeviceBootloader.ActiveConnection == LogicalDeviceActiveConnection.Direct)
			{
				return associatedLogicalDeviceBootloader;
			}
			if (!IsLogicalDeviceOnline(logicalDevice))
			{
				throw new MyRvLinkDeviceOfflineException(this, logicalDevice);
			}
			LogicalDeviceJumpToBootState logicalDeviceJumpToBootState = await logicalDevice.JumpToBootPid.ReadJumpToBootStateAsync(cancellationToken);
			switch (logicalDeviceJumpToBootState)
			{
			case LogicalDeviceJumpToBootState.RequestError:
			case LogicalDeviceJumpToBootState.SoftwareError:
			case LogicalDeviceJumpToBootState.MemoryError:
			case LogicalDeviceJumpToBootState.FeatureIdle:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Performing Jump To Boot ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				await logicalDevice.JumpToBootPid.WriteRequestJumpToBoot(holdTime, cancellationToken);
				bool lookForErrorDetails = true;
				for (int attempt = 0; attempt < 20; attempt++)
				{
					cancellationToken.ThrowIfCancellationRequested();
					await Task.Delay(1000, cancellationToken);
					if (lookForErrorDetails)
					{
						try
						{
							logicalDeviceJumpToBootState = await logicalDevice.JumpToBootPid.ReadJumpToBootStateAsync(cancellationToken);
							if (logicalDeviceJumpToBootState <= LogicalDeviceJumpToBootState.RequestBootLoaderWithHoldTime)
							{
								switch (logicalDeviceJumpToBootState)
								{
								case LogicalDeviceJumpToBootState.FeatureIdle:
								case LogicalDeviceJumpToBootState.RequestBootLoaderWithHoldTime:
									goto end_IL_0372;
								}
								goto IL_03d4;
							}
							if ((uint)logicalDeviceJumpToBootState > 2863315899u)
							{
								if (logicalDeviceJumpToBootState != LogicalDeviceJumpToBootState.SoftwareError)
								{
									_ = -286326785;
								}
								goto IL_03d4;
							}
							if (logicalDeviceJumpToBootState != LogicalDeviceJumpToBootState.BootHoldInProgress)
							{
								if (logicalDeviceJumpToBootState == LogicalDeviceJumpToBootState.RequestError)
								{
								}
								goto IL_03d4;
							}
							goto end_IL_0372;
							IL_03d4:
							defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 2);
							defaultInterpolatedStringHandler.AppendLiteral("Failed to enter bootloader mode because ");
							defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceJumpToBootState);
							defaultInterpolatedStringHandler.AppendLiteral(" for ");
							defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
							TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
							defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 1);
							defaultInterpolatedStringHandler.AppendLiteral("Failed to enter bootloader mode because ");
							defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceJumpToBootState);
							throw new FirmwareUpdateBootloaderException(defaultInterpolatedStringHandler.ToStringAndClear());
							end_IL_0372:;
						}
						catch
						{
							lookForErrorDetails = false;
						}
					}
					associatedLogicalDeviceBootloader = GetAssociatedLogicalDeviceBootloader(logicalDevice);
					if (associatedLogicalDeviceBootloader != null && associatedLogicalDeviceBootloader.ActiveConnection == LogicalDeviceActiveConnection.Direct)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Found Bootloader Device ");
						defaultInterpolatedStringHandler.AppendFormatted(associatedLogicalDeviceBootloader);
						TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
						return associatedLogicalDeviceBootloader;
					}
				}
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed to enter/find bootloader for ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				throw new FirmwareUpdateBootloaderException("Unable to find Bootloader Device");
			}
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to request to put device in bootloader mode because of it's current state ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceJumpToBootState);
				defaultInterpolatedStringHandler.AppendLiteral(" for ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(81, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to request to put device in bootloader mode because of it's current state ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceJumpToBootState);
				throw new FirmwareUpdateBootloaderException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			}
		}

		public static ILogicalDeviceReflashBootloader? GetAssociatedLogicalDeviceBootloader(ILogicalDevice logicalDevice)
		{
			ILogicalDevice logicalDevice2 = logicalDevice;
			List<ILogicalDeviceReflashBootloader> list = logicalDevice2.DeviceService.DeviceManager!.FindLogicalDevices((ILogicalDeviceReflashBootloader ld) => ld.LogicalId.ProductMacAddress == logicalDevice2.LogicalId.ProductMacAddress);
			int count = list.Count;
			if (count <= 1)
			{
				if (count == 1)
				{
					return Enumerable.First(list);
				}
				return null;
			}
			throw new LogicalDeviceException("Multiple matching Bootloader's found, there should be only up to 1");
		}

		private async Task FirmwareUpdateAuthorizationAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Requesting OTA Authorization for ");
			defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
			TaggedLog.Information("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
			if (await SendSoftwareUpdateAuthorizationAsync(logicalDevice, cancellationToken) != 0)
			{
				throw new FirmwareUpdateNotAuthorizedException(logicalDevice);
			}
		}

		private async Task<bool> TryRemoveRefreshBootLoaderWhenOfflineAsync(ILogicalDeviceFirmwareUpdateDevice logicalDeviceToReflash, CancellationToken cancellationToken)
		{
			ILogicalDeviceFirmwareUpdateDevice logicalDeviceToReflash2 = logicalDeviceToReflash;
			try
			{
				if ((byte)logicalDeviceToReflash2.LogicalId.DeviceType != 50)
				{
					return false;
				}
				DateTime startTime = DateTime.Now;
				bool isOnline;
				do
				{
					isOnline = IsLogicalDeviceOnline(logicalDeviceToReflash2);
					if (!isOnline)
					{
						break;
					}
					await Task.Delay(1000, cancellationToken);
				}
				while (isOnline || (DateTime.Now - startTime).TotalMilliseconds < 30000.0 || !cancellationToken.IsCancellationRequested);
				if (isOnline || cancellationToken.IsCancellationRequested)
				{
					return false;
				}
				logicalDeviceToReflash2.DeviceService.DeviceManager?.RemoveLogicalDevice((ILogicalDevice d) => d == logicalDeviceToReflash2);
				return true;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("DirectConnectionMyRvLink", "Unable to Remove Device " + logicalDeviceToReflash2?.DeviceName + ". " + ex.Message);
				return false;
			}
		}

		public IFrequencyMetricsReadonly GetFrequencyMetricForCommandSend(MyRvLinkCommandType commandType)
		{
			lock (_lock)
			{
				if (_metricsForCommandSends.TryGetValue(commandType, out var value))
				{
					return value;
				}
				_metricsForCommandSends[commandType] = new FrequencyMetrics();
				return _metricsForCommandSends[commandType];
			}
		}

		public IFrequencyMetricsReadonly GetFrequencyMetricForCommandFailure(MyRvLinkCommandType commandType)
		{
			lock (_lock)
			{
				if (_metricsForCommandFailures.TryGetValue(commandType, out var value))
				{
					return value;
				}
				_metricsForCommandFailures[commandType] = new FrequencyMetrics();
				return _metricsForCommandFailures[commandType];
			}
		}

		public IFrequencyMetricsReadonly GetFrequencyMetricForEvent(MyRvLinkEventType eventType)
		{
			lock (_lock)
			{
				if (_metricsForEvents.TryGetValue(eventType, out var value))
				{
					return value;
				}
				_metricsForEvents[eventType] = new FrequencyMetrics();
				return _metricsForEvents[eventType];
			}
		}

		private void UpdateFrequencyMetricForCommandSend(MyRvLinkCommandType commandType)
		{
			if (GetFrequencyMetricForCommandSend(commandType) is FrequencyMetrics frequencyMetrics)
			{
				frequencyMetrics.Update();
			}
		}

		private void UpdateFrequencyMetricForCommandFailure(MyRvLinkCommandType commandType)
		{
			if (GetFrequencyMetricForCommandFailure(commandType) is FrequencyMetrics frequencyMetrics)
			{
				frequencyMetrics.Update();
			}
		}

		private void UpdateFrequencyMetricForEvent(MyRvLinkEventType eventType)
		{
			if (GetFrequencyMetricForEvent(eventType) is FrequencyMetrics frequencyMetrics)
			{
				frequencyMetrics.Update();
			}
		}

		public async Task<CommandResult> SendDirectCommandRelayMomentary(ILogicalDeviceRelayHBridge logicalDevice, HBridgeCommand command, CancellationToken cancelToken)
		{
			foreach (ILogicalDeviceSourceCommandMonitor commandMonitor in CommandMonitors)
			{
				if (commandMonitor is ILogicalDeviceSourceCommandMonitorMovement logicalDeviceSourceCommandMonitorMovement)
				{
					await logicalDeviceSourceCommandMonitorMovement.WillSendCommandRelayMomentaryAsync(this, logicalDevice, command, cancelToken);
				}
			}
			CommandResult result = await SendDirectCommandRelayMomentaryImpl(logicalDevice, command, cancelToken);
			foreach (ILogicalDeviceSourceCommandMonitor commandMonitor2 in CommandMonitors)
			{
				if (commandMonitor2 is ILogicalDeviceSourceCommandMonitorMovement logicalDeviceSourceCommandMonitorMovement2)
				{
					await logicalDeviceSourceCommandMonitorMovement2.DidSendCommandRelayMomentaryAsync(this, logicalDevice, command, result, cancelToken);
				}
			}
			return result;
		}

		private async Task<CommandResult> SendDirectCommandRelayMomentaryImpl(ILogicalDeviceRelayHBridge logicalDevice, HBridgeCommand command, CancellationToken cancelToken)
		{
			if (!(_deviceTracker?.IsLogicalDeviceOnline(logicalDevice) ?? false))
			{
				return CommandResult.ErrorDeviceOffline;
			}
			(byte DeviceTableId, byte DeviceId)? myRvLinkDevice = GetMyRvDeviceFromLogicalDevice(logicalDevice);
			if (!myRvLinkDevice.HasValue)
			{
				return CommandResult.ErrorDeviceOffline;
			}
			try
			{
				MyRvLinkCommandContext<HBridgeCommand> commandContext;
				lock (this)
				{
					if (logicalDevice.CustomData.TryGetValue("DirectConnectionMyRvLink.IDirectCommandMovement", out var obj) && obj is MyRvLinkCommandContext<HBridgeCommand> myRvLinkCommandContext)
					{
						commandContext = myRvLinkCommandContext;
					}
					else
					{
						logicalDevice.CustomData["DirectConnectionMyRvLink.IDirectCommandMovement"] = (commandContext = new MyRvLinkCommandContext<HBridgeCommand>());
					}
				}
				if (commandContext.LastSentCommandReceivedError)
				{
					TaggedLog.Warning("DirectConnectionMyRvLink", "{LogPrefix} Momentary relay last sent command received an error!");
					commandContext.ClearLastSentCommandReceivedError();
					return commandContext.ActiveFailure?.CommandResult ?? CommandResult.ErrorOther;
				}
				bool flag = commandContext.CanResendCommand(command);
				if (flag)
				{
					flag = await ResendRunningCommandAsync(commandContext.SentCommandId, cancelToken);
				}
				if (flag)
				{
					commandContext.SentCommand(commandContext.SentCommandId, command);
					return commandContext.ActiveFailure?.CommandResult ?? CommandResult.Completed;
				}
				ushort nextCommandId = GetNextCommandId();
				MyRvLinkCommandActionMovement command2 = new MyRvLinkCommandActionMovement(nextCommandId, myRvLinkDevice.Value.DeviceTableId, myRvLinkDevice.Value.DeviceId, logicalDevice.LogicalId, command);
				SendCommandAsync(command2, cancelToken, TimeSpan.FromMilliseconds(2500.0), MyRvLinkSendCommandOption.DontWaitForResponse, delegate(IMyRvLinkCommandResponse response)
				{
					commandContext.ProcessResponse(response);
				});
				commandContext.SentCommand(nextCommandId, command);
				return commandContext.ActiveFailure?.CommandResult ?? CommandResult.Completed;
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("DirectConnectionMyRvLink", LogPrefix + " Sending command failed " + ex.Message);
				return CommandResult.ErrorOther;
			}
		}

		public async Task<bool> SetRealTimeClockTimeAsync(DateTime dateTime, CancellationToken cancellationToken)
		{
			if (!IsStarted || !IsConnected)
			{
				throw new MyRvLinkException("Unable to set RTC because DirectConnectionMyRvLink isn't started or connected");
			}
			if (_firmwareUpdateInProgress)
			{
				throw new MyRvLinkPidWriteException("Can't perform Pid writes while a firmware update is in progress!");
			}
			MyRvLinkCommandSetRealTimeClock command = new MyRvLinkCommandSetRealTimeClock(GetNextCommandId(), dateTime);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken);
			if (!(myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure failure))
			{
				if (myRvLinkCommandResponse is IMyRvLinkCommandResponseSuccess)
				{
					return true;
				}
				throw new MyRvLinkException("Failed to set RTC: Unknown result");
			}
			throw new MyRvLinkCommandResponseFailureException(failure);
		}

		public Task RemoveOfflineDevicesAsync(CancellationToken cancellationToken)
		{
			return RemoveOfflineDevicesAsync(enableConfigurationMode: false, cancellationToken);
		}

		public async Task RemoveOfflineDevicesAsync(bool enableConfigurationMode, CancellationToken cancellationToken)
		{
			if (!IsStarted || !IsConnected)
			{
				throw new MyRvLinkException("Unable to Remove Offline Devices as service isn't started");
			}
			MyRvLinkGatewayInformation gatewayInfo = GatewayInfo;
			if (gatewayInfo == null)
			{
				throw new MyRvLinkException("Unable to Remove Offline Devices as no gateway information is available yet");
			}
			_deviceTracker?.RemoveOfflineDevices();
			MyRvLinkCommandRemoveOfflineDevices command = new MyRvLinkCommandRemoveOfflineDevices(GetNextCommandId(), gatewayInfo.DeviceTableId, !enableConfigurationMode);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await SendCommandAsync(command, cancellationToken, MyRvLinkSendCommandOption.DontWaitForResponse);
			if (myRvLinkCommandResponse is IMyRvLinkCommandResponseFailure)
			{
				throw new MyRvLinkException("Failed to Remove Offline Devices: " + myRvLinkCommandResponse.ToString());
			}
		}

		public bool IsLogicalDeviceRenameSupported(ILogicalDevice? logicalDevice)
		{
			return IsLogicalDeviceSupported(logicalDevice);
		}

		public async Task RenameLogicalDevice(ILogicalDevice? logicalDevice, FUNCTION_NAME toName, byte toFunctionInstance, CancellationToken cancellationToken)
		{
			FUNCTION_NAME toName2 = toName;
			if (logicalDevice == null)
			{
				throw new ArgumentNullException("logicalDevice");
			}
			CommandResult commandResult = await SendCommandAsync(logicalDevice, ((byte DeviceTableId, byte DeviceId) myRvLinkDevice) => new MyRvLinkCommandRenameDevice(GetNextCommandId(), myRvLinkDevice.DeviceTableId, myRvLinkDevice.DeviceId, toName2, toFunctionInstance, (ushort)2), cancellationToken);
			if (commandResult != 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Rename failed because of ");
				defaultInterpolatedStringHandler.AppendFormatted(commandResult);
				throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		public float? GetTemperature()
		{
			return _temperature;
		}

		public Task<float?> TryGetVoltageAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(_voltage);
		}

		private void UpdateRvStatus(MyRvLinkRvStatus rvStatus)
		{
			if (rvStatus == null)
			{
				TaggedLog.Debug("DirectConnectionMyRvLink", LogPrefix + " Ignoring DirectConnectionMyRvLink, RvStatus is null.");
				_voltage = null;
				_temperature = null;
			}
			else
			{
				_voltage = rvStatus.GetVoltage();
				_temperature = rvStatus.GetTemperature();
			}
		}

		public Task<CommandResult> SendSoftwareUpdateAuthorizationAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken)
		{
			return SendCommandAsync(logicalDevice, ((byte DeviceTableId, byte DeviceId) myRvLinkDevice) => new MyRvLinkCommandSoftwareUpdateAuthorization(GetNextCommandId(), myRvLinkDevice.DeviceTableId, myRvLinkDevice.DeviceId), cancellationToken);
		}

		public Task<CommandResult> SendDirectCommandRelayBasicSwitch(ILogicalDeviceSwitchable logicalDevice, bool turnOn, CancellationToken cancelToken)
		{
			ILogicalDeviceSwitchable logicalDevice2 = logicalDevice;
			return SendCommandAsync(logicalDevice2, MakeCommand, cancelToken);
			IMyRvLinkCommand MakeCommand((byte DeviceTableId, byte DeviceId) myRvLinkDevice)
			{
				if (logicalDevice2 is LogicalDeviceGeneratorGenie)
				{
					return new MyRvLinkCommandActionGeneratorGenie(GetNextCommandId(), command: (!turnOn) ? GeneratorGenieCommand.Off : GeneratorGenieCommand.On, deviceTableId: myRvLinkDevice.DeviceTableId, deviceId: myRvLinkDevice.DeviceId);
				}
				if (logicalDevice2 != null)
				{
					return new MyRvLinkCommandActionSwitch(GetNextCommandId(), switchState: turnOn ? MyRvLinkCommandActionSwitchState.On : MyRvLinkCommandActionSwitchState.Off, deviceTableId: myRvLinkDevice.DeviceTableId, switchDeviceIdList: new byte[1] { myRvLinkDevice.DeviceId });
				}
				throw new MyRvLinkException("Unsupported device for DirectConnectionMyRvLink");
			}
		}

		public async Task<bool> TrySwitchAllMasterControllable(IEnumerable<ILogicalDevice> logicalDeviceList, bool allOn, CancellationToken cancellationToken)
		{
			if (!IsStarted || !IsConnected)
			{
				return false;
			}
			string operationText = (allOn ? "On" : "Off");
			TaggedLog.Debug("DirectConnectionMyRvLink", LogPrefix + " All Lights " + operationText);
			if (!IsConnected)
			{
				TaggedLog.Debug("DirectConnectionMyRvLink", LogPrefix + " Unable to Turn " + operationText + " All lights because not connected");
				return false;
			}
			MyRvLinkDeviceTracker deviceTracker = _deviceTracker;
			if (deviceTracker == null)
			{
				TaggedLog.Debug("DirectConnectionMyRvLink", LogPrefix + " Unable to Turn " + operationText + " All lights because devices not yet loaded");
				return false;
			}
			IEnumerable<ILogicalDeviceSwitchable> enumerable = Enumerable.Where(Enumerable.OfType<ILogicalDeviceSwitchable>(logicalDeviceList), (ILogicalDeviceSwitchable switchable) => switchable.IsMasterSwitchControllable);
			List<byte> list = new List<byte>();
			foreach (ILogicalDeviceSwitchable item in enumerable)
			{
				if (!(_deviceTracker?.IsLogicalDeviceOnline(item) ?? false))
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(47, 3);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" Unable to Turn ");
					defaultInterpolatedStringHandler.AppendFormatted(operationText);
					defaultInterpolatedStringHandler.AppendLiteral(" Light ");
					defaultInterpolatedStringHandler.AppendFormatted(item);
					defaultInterpolatedStringHandler.AppendLiteral(" because it isn't online");
					TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
					continue;
				}
				byte? myRvDeviceIdFromLogicalDevice = deviceTracker.GetMyRvDeviceIdFromLogicalDevice(item);
				if (!myRvDeviceIdFromLogicalDevice.HasValue)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(57, 4);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" Unable to Turn ");
					defaultInterpolatedStringHandler.AppendFormatted(operationText);
					defaultInterpolatedStringHandler.AppendLiteral(" Light ");
					defaultInterpolatedStringHandler.AppendFormatted(item);
					defaultInterpolatedStringHandler.AppendLiteral(" because it isn't associated with ");
					defaultInterpolatedStringHandler.AppendFormatted("DirectConnectionMyRvLink");
					TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				else
				{
					list.Add(myRvDeviceIdFromLogicalDevice.Value);
				}
			}
			try
			{
				if (list.Count == 0)
				{
					return false;
				}
				MyRvLinkCommandActionSwitch command = new MyRvLinkCommandActionSwitch(GetNextCommandId(), deviceTracker.DeviceTableId, allOn ? MyRvLinkCommandActionSwitchState.On : MyRvLinkCommandActionSwitchState.Off, list.ToArray());
				IMyRvLinkCommandResponse obj = await SendCommandAsync(command, cancellationToken);
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(46, 2);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" TrySwitchAllMasterControllable Completed for\n");
				defaultInterpolatedStringHandler.AppendFormatted(command);
				TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				if (obj.CommandResult != 0)
				{
					throw new MyRvLinkException("Failed to turn all lights " + operationText);
				}
				return true;
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 3);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" Unable to Turn ");
				defaultInterpolatedStringHandler.AppendFormatted(operationText);
				defaultInterpolatedStringHandler.AppendLiteral(" Lights: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Debug("DirectConnectionMyRvLink", defaultInterpolatedStringHandler.ToStringAndClear());
				return false;
			}
		}
	}
}
