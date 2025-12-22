using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Devices.JaycoTbbGateway;
using IDS.Portable.LogicalDevice;
using OneControl.Devices;
using OneControl.Devices.AccessoryGateway;
using OneControl.Devices.AwningSensor;
using OneControl.Devices.BatteryMonitor;
using OneControl.Devices.BootLoader;
using OneControl.Devices.BrakingSystem;
using OneControl.Devices.DoorLock;
using OneControl.Devices.Leveler.Type5;
using OneControl.Devices.LightRgb;
using OneControl.Devices.TemperatureSensor;
using OneControl.Direct.MyRvLink.Events;

namespace OneControl.Direct.MyRvLink
{
	public class MyRvLinkDeviceTracker : CommonDisposable
	{
		private const string LogTag = "MyRvLinkDeviceTracker";

		private string LogPrefix;

		private MyRvLinkDeviceMetadataTracker? _deviceMetadataTracker;

		private readonly MyRvLinkDeviceManager _myRvLinkDeviceManager;

		private CancellationTokenSource? _commandGetDevicesTcs;

		private IN_MOTION_LOCKOUT_LEVEL _cachedInMotionLockoutLevel = (byte)0;

		public IDirectConnectionMyRvLink MyRvLinkService { get; }

		public byte DeviceTableId { get; private set; }

		public uint DeviceTableCrc { get; }

		public List<IMyRvLinkDevice> DeviceList { get; private set; } = new List<IMyRvLinkDevice>();


		public bool IsActive
		{
			get
			{
				if (!base.IsDisposed && DeviceTableCrc == MyRvLinkService.GatewayInfo?.DeviceTableCrc && MyRvLinkService.IsFirmwareVersionSupported)
				{
					return MyRvLinkService.IsConnected;
				}
				return false;
			}
		}

		public bool IsDeviceLoadComplete { get; private set; }

		public IN_MOTION_LOCKOUT_LEVEL CachedInMotionLockoutLevel
		{
			get
			{
				if (!IsActive)
				{
					return (byte)0;
				}
				return _cachedInMotionLockoutLevel;
			}
			set
			{
				if (_cachedInMotionLockoutLevel != value)
				{
					_cachedInMotionLockoutLevel = value;
					MyRvLinkService.DeviceService.UpdateInMotionLockoutLevel();
				}
			}
		}

		public MyRvLinkDeviceTracker(IDirectConnectionMyRvLink myRvLinkService, byte deviceTableId, uint deviceTableCrc)
		{
			MyRvLinkService = myRvLinkService ?? throw new ArgumentNullException("Invalid IMyRvLinkService", "myRvLinkService");
			LogPrefix = MyRvLinkService.LogPrefix;
			if (myRvLinkService.GatewayInfo == null)
			{
				throw new ArgumentNullException("Invalid GatewayInfo", "GatewayInfo");
			}
			DeviceTableId = deviceTableId;
			DeviceTableCrc = deviceTableCrc;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 3);
			defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
			defaultInterpolatedStringHandler.AppendLiteral(" CREATED Device Tracker for ");
			defaultInterpolatedStringHandler.AppendFormatted(myRvLinkService);
			defaultInterpolatedStringHandler.AppendLiteral(" with TagList: ");
			defaultInterpolatedStringHandler.AppendFormatted(LogicalDeviceTagManager.DebugTagsAsString(MyRvLinkService.ConnectionTagList));
			TaggedLog.Debug("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
			_myRvLinkDeviceManager = new MyRvLinkDeviceManager(MyRvLinkService.DeviceService, MyRvLinkService);
		}

		public void UpdateDeviceIdIfNeeded(byte deviceTableId, uint deviceTableCrc)
		{
			if (!base.IsDisposed && DeviceTableCrc == deviceTableCrc && DeviceTableId != deviceTableId)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(58, 4);
				defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
				defaultInterpolatedStringHandler.AppendLiteral(" Updated Device Table Id from ");
				defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId);
				defaultInterpolatedStringHandler.AppendLiteral(" to ");
				defaultInterpolatedStringHandler.AppendFormatted(deviceTableId);
				defaultInterpolatedStringHandler.AppendLiteral(" for Device Table CRC 0x");
				defaultInterpolatedStringHandler.AppendFormatted(deviceTableCrc, "X");
				TaggedLog.Debug("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
				DeviceTableId = deviceTableId;
			}
		}

		public bool IsLogicalDeviceOnline(ILogicalDevice? logicalDevice)
		{
			if (IsActive && logicalDevice != null && MyRvLinkService.IsLogicalDeviceSupported(logicalDevice))
			{
				return _myRvLinkDeviceManager.IsLogicalDeviceOnline(logicalDevice?.LogicalId);
			}
			return false;
		}

		public IN_MOTION_LOCKOUT_LEVEL GetLogicalDeviceInTransitLockoutLevel(ILogicalDevice? logicalDevice)
		{
			if (!IsActive || logicalDevice == null || !MyRvLinkService.IsLogicalDeviceSupported(logicalDevice))
			{
				return (byte)0;
			}
			return _myRvLinkDeviceManager.GetInTransitLockoutLevel(logicalDevice?.LogicalId);
		}

		public void GetDevicesIfNeeded()
		{
			if (!IsActive)
			{
				_commandGetDevicesTcs?.TryCancelAndDispose();
			}
			else
			{
				if (DeviceList.Count > 0 || _commandGetDevicesTcs != null)
				{
					return;
				}
				CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
				Interlocked.Exchange(ref _commandGetDevicesTcs, cancellationTokenSource)?.TryCancelAndDispose();
				CancellationToken commandCancellationToken = cancellationTokenSource.Token;
				Task.Run(async delegate
				{
					DeviceList = new List<IMyRvLinkDevice>();
					try
					{
						IReadOnlyList<IMyRvLinkDevice> readOnlyList = await MyRvLinkService.DeviceTableIdCache.GetDevicesForDeviceTableCrcAsync(DeviceTableCrc);
						if (readOnlyList == null)
						{
							throw new Exception("Device info not found in device cache.");
						}
						DeviceList = new List<IMyRvLinkDevice>(readOnlyList);
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 3);
						defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
						defaultInterpolatedStringHandler.AppendLiteral(" Getting Cached Devices for 0x");
						defaultInterpolatedStringHandler.AppendFormatted(DeviceTableCrc, "x8");
						defaultInterpolatedStringHandler.AppendLiteral(" has ");
						defaultInterpolatedStringHandler.AppendFormatted(DeviceList.Count);
						defaultInterpolatedStringHandler.AppendLiteral(" devices");
						TaggedLog.Information("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					catch (Exception ex)
					{
						TaggedLog.Error("MyRvLinkDeviceTracker", LogPrefix + " Unable to load devices " + ex.Message);
					}
					if (DeviceList.Count == 0)
					{
						try
						{
							List<IMyRvLinkDevice> deviceList = await GetDevicesAsync(commandCancellationToken).ConfigureAwait(false);
							DeviceList = deviceList;
							await MyRvLinkService.DeviceTableIdCache.UpdateDevicesAsync(DeviceTableCrc, DeviceTableId, DeviceList);
						}
						catch (Exception ex2)
						{
							TaggedLog.Debug("MyRvLinkDeviceTracker", LogPrefix + " Get Devices failed: " + ex2.Message);
						}
					}
					_commandGetDevicesTcs?.TryCancelAndDispose();
					_commandGetDevicesTcs = null;
				}, commandCancellationToken);
			}
		}

		private async Task<List<IMyRvLinkDevice>> GetDevicesAsync(CancellationToken cancellationToken)
		{
			MyRvLinkCommandGetDevices commandGetDevices = new MyRvLinkCommandGetDevices(MyRvLinkService.GetNextCommandId(), DeviceTableId, 0, 255);
			IMyRvLinkCommandResponse myRvLinkCommandResponse = await MyRvLinkService.SendCommandAsync(commandGetDevices, cancellationToken, MyRvLinkSendCommandOption.None);
			if (myRvLinkCommandResponse is MyRvLinkCommandResponseFailure)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(7, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Failed ");
				defaultInterpolatedStringHandler.AppendFormatted(myRvLinkCommandResponse);
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (commandGetDevices.ResponseState == MyRvLinkResponseState.Failed)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Get Device Command Failed ");
				defaultInterpolatedStringHandler.AppendFormatted(commandGetDevices.ResponseState);
				throw new MyRvLinkException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			if (commandGetDevices.ResponseReceivedDeviceTableCrc != DeviceTableCrc)
			{
				throw new MyRvLinkException("Response didn't match expected Device Table CRC, discarding response");
			}
			List<IMyRvLinkDevice> devices = commandGetDevices.Devices;
			if (devices == null || devices.Count == 0)
			{
				throw new MyRvLinkException("No Devices Found");
			}
			return devices;
		}

		public void UpdateMetadataIfNeeded(uint deviceMetadataTableCrc)
		{
			if (deviceMetadataTableCrc != _deviceMetadataTracker?.DeviceMetadataTableCrc)
			{
				_deviceMetadataTracker?.TryDispose();
				_deviceMetadataTracker = null;
			}
			if (_deviceMetadataTracker == null)
			{
				_deviceMetadataTracker = new MyRvLinkDeviceMetadataTracker(this, deviceMetadataTableCrc);
			}
			GetDevicesMetadataIfNeeded();
		}

		public void GetDevicesMetadataIfNeeded()
		{
			_deviceMetadataTracker?.GetDevicesMetadataIfNeeded();
		}

		public void UpdateMetadata(List<IMyRvLinkDeviceMetadata> deviceMetadataList, uint deviceMetadataTableCrc)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (!IsActive)
			{
				return;
			}
			if (deviceMetadataList.Count != deviceList.Count)
			{
				TaggedLog.Error("MyRvLinkDeviceTracker", LogPrefix + " Unable to update Devices with their metadata because the number of devices don't match.");
				return;
			}
			for (int i = 0; i < deviceMetadataList.Count; i++)
			{
				IMyRvLinkDevice myRvLinkDevice = deviceList[i];
				if (!(myRvLinkDevice is MyRvLinkDeviceHost myRvLinkDeviceHost))
				{
					if (myRvLinkDevice is MyRvLinkDeviceIdsCan myRvLinkDeviceIdsCan)
					{
						if (!(deviceMetadataList[i] is MyRvLinkDeviceIdsCanMetadata myRvLinkDeviceIdsCanMetadata))
						{
							TaggedLog.Error("MyRvLinkDeviceTracker", LogPrefix + " Unable to update IDS CAN device meta-data because metadata is of the incorrect type");
							continue;
						}
						myRvLinkDeviceIdsCan.UpdateMetadata(myRvLinkDeviceIdsCanMetadata);
						_myRvLinkDeviceManager.AddLogicalDevice(myRvLinkDeviceIdsCan)?.UpdateCircuitId(myRvLinkDeviceIdsCanMetadata.CircuitId);
					}
				}
				else if (!(deviceMetadataList[i] is MyRvLinkDeviceHostMetadata myRvLinkDeviceHostMetadata))
				{
					TaggedLog.Error("MyRvLinkDeviceTracker", LogPrefix + " Unable to update host device meta-data because metadata is of the incorrect type");
				}
				else
				{
					myRvLinkDeviceHost.UpdateMetadata(myRvLinkDeviceHostMetadata);
					_myRvLinkDeviceManager.AddLogicalDevice(myRvLinkDeviceHost)?.UpdateCircuitId(myRvLinkDeviceHostMetadata.CircuitId);
				}
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 2);
			defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
			defaultInterpolatedStringHandler.AppendLiteral(" DeviceTableId: 0x");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId, "X");
			TaggedLog.Information("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
			for (int j = 0; j < deviceMetadataList.Count; j++)
			{
				IMyRvLinkDevice myRvLinkDevice2 = deviceList[j];
				if (!(myRvLinkDevice2 is MyRvLinkDeviceHost myRvLinkDeviceHost2))
				{
					if (myRvLinkDevice2 is MyRvLinkDeviceIdsCan myRvLinkDeviceIdsCan2)
					{
						if (!(deviceMetadataList[j] is MyRvLinkDeviceIdsCanMetadata metadata))
						{
							defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 2);
							defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
							defaultInterpolatedStringHandler.AppendLiteral(" DeviceId 0x");
							defaultInterpolatedStringHandler.AppendFormatted(j, "X2");
							defaultInterpolatedStringHandler.AppendLiteral(": NON-IDS CAN Metadata not fully loaded");
							TaggedLog.Information("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
						}
						else
						{
							myRvLinkDeviceIdsCan2.UpdateMetadata(metadata);
							defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 3);
							defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
							defaultInterpolatedStringHandler.AppendLiteral(" DeviceId 0x");
							defaultInterpolatedStringHandler.AppendFormatted(j, "X2");
							defaultInterpolatedStringHandler.AppendLiteral(": IDS CAN Device ");
							defaultInterpolatedStringHandler.AppendFormatted(myRvLinkDeviceIdsCan2);
							TaggedLog.Information("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
						}
					}
					else
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 2);
						defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
						defaultInterpolatedStringHandler.AppendLiteral(" DeviceId 0x");
						defaultInterpolatedStringHandler.AppendFormatted(j, "X2");
						defaultInterpolatedStringHandler.AppendLiteral(": Unknown Device Type");
						TaggedLog.Information("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
				}
				else if (!(deviceMetadataList[j] is MyRvLinkDeviceHostMetadata metadata2))
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(44, 2);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" DeviceId 0x");
					defaultInterpolatedStringHandler.AppendFormatted(j, "X2");
					defaultInterpolatedStringHandler.AppendLiteral(": HOST Metadata not fully loaded");
					TaggedLog.Information("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				else
				{
					myRvLinkDeviceHost2.UpdateMetadata(metadata2);
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 3);
					defaultInterpolatedStringHandler.AppendFormatted(LogPrefix);
					defaultInterpolatedStringHandler.AppendLiteral(" DeviceId 0x");
					defaultInterpolatedStringHandler.AppendFormatted(j, "X2");
					defaultInterpolatedStringHandler.AppendLiteral(": HOST Device ");
					defaultInterpolatedStringHandler.AppendFormatted(myRvLinkDeviceHost2);
					TaggedLog.Information("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
			IsDeviceLoadComplete = true;
		}

		public byte? GetMyRvDeviceIdFromLogicalDevice(ILogicalDevice logicalDevice)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (!IsActive || deviceList == null || logicalDevice == null || logicalDevice.IsDisposed)
			{
				return null;
			}
			for (byte b = 0; b < deviceList.Count; b = (byte)(b + 1))
			{
				if (deviceList[b] is IMyRvLinkDeviceForLogicalDevice device && _myRvLinkDeviceManager.GetLogicalDevice(device) == logicalDevice)
				{
					return b;
				}
			}
			return null;
		}

		public IMyRvLinkDeviceForLogicalDevice? GetMyRvDeviceFromLogicalDevice(ILogicalDevice logicalDevice)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (!IsActive || deviceList == null || logicalDevice == null || logicalDevice.IsDisposed)
			{
				return null;
			}
			foreach (IMyRvLinkDevice item in deviceList)
			{
				if (item is IMyRvLinkDeviceForLogicalDevice myRvLinkDeviceForLogicalDevice && _myRvLinkDeviceManager.GetLogicalDevice(myRvLinkDeviceForLogicalDevice) == logicalDevice)
				{
					return myRvLinkDeviceForLogicalDevice;
				}
			}
			return null;
		}

		public ILogicalDevice? GetLogicalDeviceFromMyRvDevice(byte deviceTableId, byte deviceId)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceTableId != DeviceTableId || deviceList == null)
			{
				return null;
			}
			if (!deviceList.TryGetValueAtIndex(deviceId, out var item))
			{
				return null;
			}
			if (!(item is IMyRvLinkDeviceForLogicalDevice device))
			{
				return null;
			}
			return _myRvLinkDeviceManager.GetLogicalDevice(device);
		}

		public IEnumerable<(byte DeviceTableId, byte DeviceId, ILogicalDevice LogicalDevice)> EnumerateLogicalDevices(Func<ILogicalDevice, bool> filter)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (!IsActive || deviceList == null)
			{
				yield break;
			}
			for (byte deviceId = 0; deviceId < deviceList.Count; deviceId = (byte)(deviceId + 1))
			{
				if (deviceList[deviceId] is IMyRvLinkDeviceForLogicalDevice device)
				{
					ILogicalDevice logicalDevice = _myRvLinkDeviceManager.GetLogicalDevice(device);
					if (logicalDevice != null && !logicalDevice.IsDisposed && filter(logicalDevice))
					{
						yield return (DeviceTableId, deviceId, logicalDevice);
					}
				}
			}
		}

		internal void TakeDevicesOfflineIfNeeded()
		{
			_myRvLinkDeviceManager.TakeDevicesOfflineIfNeeded(forceOffline: false);
		}

		public void RemoveOfflineDevices()
		{
			_myRvLinkDeviceManager.RemoveOfflineDevices();
		}

		internal void RemoveInTransitLockoutLevel()
		{
			_myRvLinkDeviceManager.RemoveInTransitLockoutLevel(forceRemoveLockout: false);
		}

		public override void Dispose(bool disposing)
		{
			_deviceMetadataTracker?.TryDispose();
			_deviceMetadataTracker = null;
			_commandGetDevicesTcs?.TryCancelAndDispose();
			_myRvLinkDeviceManager.TakeDevicesOfflineIfNeeded(forceOffline: true);
			_myRvLinkDeviceManager.RemoveInTransitLockoutLevel(forceRemoveLockout: true);
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(11, 2);
			defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkDeviceTracker");
			defaultInterpolatedStringHandler.AppendLiteral(" DISPOSED: ");
			defaultInterpolatedStringHandler.AppendFormatted(this);
			TaggedLog.Debug("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 3);
			defaultInterpolatedStringHandler.AppendLiteral("DeviceTableId: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableId);
			defaultInterpolatedStringHandler.AppendLiteral(" Crc: ");
			defaultInterpolatedStringHandler.AppendFormatted(DeviceTableCrc);
			defaultInterpolatedStringHandler.AppendLiteral(" IsDisposed: ");
			defaultInterpolatedStringHandler.AppendFormatted(base.IsDisposed);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		internal void UpdateAccessoryGatewayStatus(MyRvLinkAccessoryGatewayStatus accessoryGatewayStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || accessoryGatewayStatus == null || accessoryGatewayStatus.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceAccessoryGatewayStatus) in accessoryGatewayStatus.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceAccessoryGateway logicalDeviceAccessoryGateway))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateAccessoryGatewayStatus");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceAccessoryGatewayStatus);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceAccessoryGateway");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else if (!logicalDeviceAccessoryGateway.DeviceStatus.EqualsData(logicalDeviceAccessoryGatewayStatus))
					{
						logicalDeviceAccessoryGateway.UpdateDeviceStatus(logicalDeviceAccessoryGatewayStatus.Data, logicalDeviceAccessoryGatewayStatus.Size);
					}
				}
			}
		}

		internal void UpdateAutoOperationProgressStatus(MyRvLinkLevelerType5ExtendedStatus progressStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || progressStatus == null)
			{
				return;
			}
			foreach (var (index, logicalDeviceLevelerStatusExtendedType) in progressStatus.EnumerateStatus())
			{
				if (!deviceList.TryGetValueAtIndex(index, out var item) || !(item is MyRvLinkDeviceIdsCan device))
				{
					continue;
				}
				if (_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceDirectLevelerType5 logicalDeviceDirectLevelerType)
				{
					if (!(logicalDeviceLevelerStatusExtendedType is LogicalDeviceLevelerStatusExtendedType5AutoStep) || !logicalDeviceDirectLevelerType.DeviceStatusExtendedAutoStep.EqualsData((LogicalDeviceLevelerStatusExtendedType5AutoStep)logicalDeviceLevelerStatusExtendedType))
					{
						if (logicalDeviceLevelerStatusExtendedType is LogicalDeviceLevelerStatusExtendedType5JackStrokeLength)
						{
							logicalDeviceDirectLevelerType.UpdateDeviceStatusExtended(logicalDeviceLevelerStatusExtendedType.Data, logicalDeviceLevelerStatusExtendedType.Size, 1);
						}
						else
						{
							logicalDeviceDirectLevelerType.UpdateDeviceStatusExtended(logicalDeviceLevelerStatusExtendedType.Data, logicalDeviceLevelerStatusExtendedType.Size, 0);
						}
					}
				}
				else
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(100, 3);
					defaultInterpolatedStringHandler.AppendFormatted("UpdateAutoOperationProgressStatus");
					defaultInterpolatedStringHandler.AppendLiteral(" Unable to update Auto Operation Progress status to ");
					defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceLevelerStatusExtendedType);
					defaultInterpolatedStringHandler.AppendLiteral(" because the logical device was unexpected for ");
					defaultInterpolatedStringHandler.AppendFormatted(this);
					defaultInterpolatedStringHandler.AppendLiteral(".");
					TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
		}

		internal void UpdateAwningSensorStatus(MyRvLinkAwningSensorStatus awningSensorStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || awningSensorStatus == null || awningSensorStatus.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceAwningSensorStatus) in awningSensorStatus.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceAwningSensor logicalDeviceAwningSensor))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateAwningSensorStatus");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceAwningSensorStatus);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceAwningSensor");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else if (!logicalDeviceAwningSensor.DeviceStatus.EqualsData(logicalDeviceAwningSensorStatus))
					{
						logicalDeviceAwningSensor.UpdateDeviceStatus(logicalDeviceAwningSensorStatus.Data, logicalDeviceAwningSensorStatus.Size);
					}
				}
			}
		}

		internal void UpdateBatteryMonitorStatus(MyRvLinkBatteryMonitorStatus batteryMonitorStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || batteryMonitorStatus == null || batteryMonitorStatus.DeviceTableId != DeviceTableId || !deviceList.TryGetValueAtIndex(batteryMonitorStatus.DeviceId, out var item) || !(item is MyRvLinkDeviceIdsCan device))
			{
				return;
			}
			if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceBatteryMonitor logicalDeviceBatteryMonitor))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
				defaultInterpolatedStringHandler.AppendFormatted("UpdateBatteryMonitorStatus");
				defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
				defaultInterpolatedStringHandler.AppendFormatted(batteryMonitorStatus);
				defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
				defaultInterpolatedStringHandler.AppendFormatted(this);
				defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
				defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceBatteryMonitor");
				TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			else
			{
				var (logicalDeviceBatteryMonitorStatus, logicalDeviceBatteryMonitorStatusExtended) = batteryMonitorStatus.GetStatusAndExtendedStatus();
				if (!logicalDeviceBatteryMonitor.DeviceStatus.EqualsData(logicalDeviceBatteryMonitorStatus))
				{
					logicalDeviceBatteryMonitor.UpdateDeviceStatus(logicalDeviceBatteryMonitorStatus.Data, logicalDeviceBatteryMonitorStatus.Size);
				}
				ILogicalDeviceWithStatusExtended logicalDeviceWithStatusExtended = logicalDeviceBatteryMonitor;
				if (logicalDeviceWithStatusExtended != null && !logicalDeviceWithStatusExtended.RawDeviceStatusExtended.Equals(logicalDeviceBatteryMonitorStatusExtended))
				{
					logicalDeviceWithStatusExtended.UpdateDeviceStatusExtended(logicalDeviceBatteryMonitorStatusExtended.Data, (uint)logicalDeviceBatteryMonitorStatusExtended.Data.Length, logicalDeviceBatteryMonitorStatusExtended.ExtendedByte);
				}
			}
		}

		internal void UpdateBootLoaderStatus(MyRvLinkBootLoaderStatus bootLoaderStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || bootLoaderStatus == null || bootLoaderStatus.DeviceTableId != DeviceTableId || !deviceList.TryGetValueAtIndex(bootLoaderStatus.DeviceId, out var item) || !(item is MyRvLinkDeviceIdsCan device))
			{
				return;
			}
			if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceReflashBootloader logicalDeviceReflashBootloader))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
				defaultInterpolatedStringHandler.AppendFormatted("UpdateBootLoaderStatus");
				defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
				defaultInterpolatedStringHandler.AppendFormatted(bootLoaderStatus);
				defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
				defaultInterpolatedStringHandler.AppendFormatted(this);
				defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
				defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceReflashBootloader");
				TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			else
			{
				LogicalDeviceReflashBootLoaderStatus status = bootLoaderStatus.GetStatus();
				if (!logicalDeviceReflashBootloader.DeviceStatus.EqualsData(status))
				{
					logicalDeviceReflashBootloader.UpdateDeviceStatus(status.Data, status.Size);
				}
			}
		}

		internal void UpdateBrakingSystemStatus(MyRvLinkBrakingSystemStatus absStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || absStatus.DeviceTableId != DeviceTableId || !deviceList.TryGetValueAtIndex(absStatus.DeviceId, out var item) || !(item is MyRvLinkDeviceIdsCan device))
			{
				return;
			}
			if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceBrakingSystem logicalDeviceBrakingSystem))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
				defaultInterpolatedStringHandler.AppendFormatted("UpdateBrakingSystemStatus");
				defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
				defaultInterpolatedStringHandler.AppendFormatted(absStatus);
				defaultInterpolatedStringHandler.AppendLiteral(" because ");
				defaultInterpolatedStringHandler.AppendLiteral("can't find logical device for ");
				defaultInterpolatedStringHandler.AppendFormatted(this);
				defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
				defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceBrakingSystem");
				TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			else
			{
				LogicalDeviceBrakingSystemStatus brakingSystemStatus = absStatus.GetBrakingSystemStatus();
				if (brakingSystemStatus != null && !logicalDeviceBrakingSystem.DeviceStatus.EqualsData(brakingSystemStatus))
				{
					logicalDeviceBrakingSystem.UpdateDeviceStatus(brakingSystemStatus.Data, brakingSystemStatus.Size);
				}
			}
		}

		internal void UpdateCloudGatewayStatus(MyRvLinkCloudGatewayStatus cloudGatewayStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || cloudGatewayStatus == null)
			{
				TaggedLog.Debug("MyRvLinkDeviceTracker", "Ignoring MyRvLinkDeviceTracker no device list or TableId's don't match");
			}
			else
			{
				if (cloudGatewayStatus.DeviceTableId != DeviceTableId)
				{
					return;
				}
				foreach (var (index, logicalDeviceCloudGatewayStatus, _) in cloudGatewayStatus.EnumerateStatus())
				{
					if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
					{
						if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceCloudGateway logicalDeviceCloudGateway))
						{
							DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
							defaultInterpolatedStringHandler.AppendFormatted("UpdateGeneratorGenieStatus");
							defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
							defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceCloudGatewayStatus);
							defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
							defaultInterpolatedStringHandler.AppendFormatted(this);
							defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
							defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceRemoteCloudGateway");
							TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
							break;
						}
						if (!logicalDeviceCloudGateway.DeviceStatus.EqualsData(logicalDeviceCloudGatewayStatus))
						{
							logicalDeviceCloudGateway.UpdateDeviceStatus(logicalDeviceCloudGatewayStatus);
						}
					}
				}
			}
		}

		internal void UpdateDimmableLightStatus(MyRvLinkDimmableLightStatus dimmableLightStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || dimmableLightStatus == null || dimmableLightStatus.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceLightDimmableStatus) in dimmableLightStatus.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceRemoteLightDimmable logicalDeviceRemoteLightDimmable))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateRgbLightStatus");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceLightDimmableStatus);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceRemoteLightDimmable");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
						break;
					}
					if (!logicalDeviceRemoteLightDimmable.DeviceStatus.EqualsData(logicalDeviceLightDimmableStatus))
					{
						logicalDeviceRemoteLightDimmable.UpdateDeviceStatus(logicalDeviceLightDimmableStatus);
					}
				}
			}
		}

		internal void UpdateDimmableLightExtended(MyRvLinkDimmableLightStatusExtended dimmableLightStatusExtended)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || dimmableLightStatusExtended == null || dimmableLightStatusExtended.DeviceTableId != DeviceTableId || !deviceList.TryGetValueAtIndex(dimmableLightStatusExtended.DeviceId, out var item) || !(item is MyRvLinkDeviceIdsCan device))
			{
				return;
			}
			if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceLightDimmable logicalDeviceLightDimmable))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
				defaultInterpolatedStringHandler.AppendFormatted("UpdateDimmableLightExtended");
				defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
				defaultInterpolatedStringHandler.AppendFormatted(dimmableLightStatusExtended);
				defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
				defaultInterpolatedStringHandler.AppendFormatted(this);
				defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
				defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceLightDimmable");
				TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			else
			{
				LogicalDeviceLightDimmableStatusExtended extendedStatus = dimmableLightStatusExtended.GetExtendedStatus();
				if (!logicalDeviceLightDimmable.RawDeviceStatusExtended.Equals(extendedStatus))
				{
					logicalDeviceLightDimmable.UpdateDeviceStatusExtended(extendedStatus.Data, (uint)extendedStatus.Data.Length, extendedStatus.ExtendedByte);
				}
			}
		}

		internal void UpdateDoorLockStatus(MyRvLinkDoorLockStatus doorLockStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || doorLockStatus == null || doorLockStatus.DeviceTableId != DeviceTableId || !deviceList.TryGetValueAtIndex(doorLockStatus.DeviceId, out var item) || !(item is MyRvLinkDeviceIdsCan device))
			{
				return;
			}
			if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceDoorLock logicalDeviceDoorLock))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
				defaultInterpolatedStringHandler.AppendFormatted("UpdateDoorLockStatus");
				defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
				defaultInterpolatedStringHandler.AppendFormatted(doorLockStatus);
				defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
				defaultInterpolatedStringHandler.AppendFormatted(this);
				defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
				defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceDoorLock");
				TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			else
			{
				LogicalDeviceDoorLockStatus status = doorLockStatus.GetStatus();
				if (!logicalDeviceDoorLock.DeviceStatus.EqualsData(status))
				{
					logicalDeviceDoorLock.UpdateDeviceStatus(status.Data, status.Size);
				}
			}
		}

		internal void UpdateGeneratorGenieStatus(MyRvLinkGeneratorGenieStatus generatorGenieStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || generatorGenieStatus == null || generatorGenieStatus.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceGeneratorGenieStatus) in generatorGenieStatus.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceGeneratorGenieDirect logicalDeviceGeneratorGenieDirect))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateGeneratorGenieStatus");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceGeneratorGenieStatus);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceGeneratorGenieRemote");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
						break;
					}
					if (!logicalDeviceGeneratorGenieDirect.DeviceStatus.EqualsData(logicalDeviceGeneratorGenieStatus))
					{
						logicalDeviceGeneratorGenieDirect.UpdateDeviceStatus(logicalDeviceGeneratorGenieStatus);
					}
				}
			}
		}

		internal void UpdateHourMeterStatus(MyRvLinkHourMeterStatus? tankSensorStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || tankSensorStatus == null || tankSensorStatus!.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, status) in tankSensorStatus!.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceHourMeterDirect logicalDeviceHourMeterDirect))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 3);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateHourMeterStatus");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(tankSensorStatus);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else
					{
						logicalDeviceHourMeterDirect.UpdateDeviceStatus(status);
					}
				}
			}
		}

		internal void UpdateHvacStatus(MyRvLinkHvacStatus hvacStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || hvacStatus == null || hvacStatus.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceClimateZoneStatus, logicalDeviceClimateZoneStatusEx) in hvacStatus.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceClimateZoneDirect logicalDeviceClimateZoneDirect))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateHvacStatus");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceClimateZoneStatus);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceClimateZoneRemote");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
						break;
					}
					if (!logicalDeviceClimateZoneDirect.DeviceStatus.EqualsData(logicalDeviceClimateZoneStatus))
					{
						logicalDeviceClimateZoneDirect.UpdateDeviceStatus(logicalDeviceClimateZoneStatus);
					}
					if (logicalDeviceClimateZoneDirect is ILogicalDeviceWithStatusExtended logicalDeviceWithStatusExtended && !logicalDeviceWithStatusExtended.RawDeviceStatusExtended.Equals(logicalDeviceClimateZoneStatusEx))
					{
						logicalDeviceWithStatusExtended.UpdateDeviceStatusExtended(logicalDeviceClimateZoneStatusEx.Data, (uint)logicalDeviceClimateZoneStatusEx.Data.Length, logicalDeviceClimateZoneStatusEx.ExtendedByte);
					}
				}
			}
		}

		internal void UpdateJaycoTbbStatus(MyRvLinkJaycoTbbStatus jaycoTbbStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || jaycoTbbStatus == null || jaycoTbbStatus.DeviceTableId != DeviceTableId || !deviceList.TryGetValueAtIndex(jaycoTbbStatus.DeviceId, out var item) || !(item is MyRvLinkDeviceIdsCan device))
			{
				return;
			}
			if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceJaycoTbb logicalDeviceJaycoTbb))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
				defaultInterpolatedStringHandler.AppendFormatted("UpdateJaycoTbbStatus");
				defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
				defaultInterpolatedStringHandler.AppendFormatted(jaycoTbbStatus);
				defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
				defaultInterpolatedStringHandler.AppendFormatted(this);
				defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
				defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceJaycoTbb");
				TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			else
			{
				LogicalDeviceJaycoTbbStatus status = jaycoTbbStatus.GetStatus();
				if (!logicalDeviceJaycoTbb.DeviceStatus.EqualsData(status))
				{
					logicalDeviceJaycoTbb.UpdateDeviceStatus(status.Data, status.Size);
				}
			}
		}

		internal void UpdateLeveler1Status(MyRvLinkLeveler1Status levelerStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || levelerStatus == null || levelerStatus.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceLevelerStatusType) in levelerStatus.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceDirectLevelerType1 logicalDeviceDirectLevelerType))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateLeveler1Status");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceLevelerStatusType);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceDirectLevelerType1");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else if (!logicalDeviceDirectLevelerType.DeviceStatus.EqualsData(logicalDeviceLevelerStatusType))
					{
						logicalDeviceDirectLevelerType.UpdateDeviceStatus(logicalDeviceLevelerStatusType.Data, logicalDeviceLevelerStatusType.Size);
					}
				}
			}
		}

		internal void UpdateLeveler3Status(MyRvLinkLeveler3Status levelerStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || levelerStatus == null || levelerStatus.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceLevelerStatusType) in levelerStatus.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceDirectLevelerType3 logicalDeviceDirectLevelerType))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateLeveler3Status");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceLevelerStatusType);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceDirectLevelerType3");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else if (!logicalDeviceDirectLevelerType.DeviceStatus.EqualsData(logicalDeviceLevelerStatusType))
					{
						logicalDeviceDirectLevelerType.UpdateDeviceStatus(logicalDeviceLevelerStatusType.Data, logicalDeviceLevelerStatusType.Size);
					}
				}
			}
		}

		internal void UpdateLeveler4Status(MyRvLinkLeveler4Status levelerStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || levelerStatus == null || levelerStatus.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceLevelerStatusType) in levelerStatus.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceDirectLevelerType4 logicalDeviceDirectLevelerType))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateLeveler4Status");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceLevelerStatusType);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceDirectLevelerType4");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else if (!logicalDeviceDirectLevelerType.DeviceStatus.EqualsData(logicalDeviceLevelerStatusType))
					{
						logicalDeviceDirectLevelerType.UpdateDeviceStatus(logicalDeviceLevelerStatusType.Data, logicalDeviceLevelerStatusType.Size);
					}
				}
			}
		}

		internal void UpdateLeveler5Status(MyRvLinkLeveler5Status levelerStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || levelerStatus == null || levelerStatus.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceLevelerStatusType) in levelerStatus.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceDirectLevelerType5 logicalDeviceDirectLevelerType))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateLeveler5Status");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceLevelerStatusType);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceDirectLevelerType5");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else if (!logicalDeviceDirectLevelerType.DeviceStatus.EqualsData(logicalDeviceLevelerStatusType))
					{
						logicalDeviceDirectLevelerType.UpdateDeviceStatus(logicalDeviceLevelerStatusType.Data, logicalDeviceLevelerStatusType.Size);
					}
				}
			}
		}

		internal void UpdateLevelerConsoleText(MyRvLinkLevelerConsoleText levelerConsoleText)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || levelerConsoleText == null || levelerConsoleText.DeviceTableId != DeviceTableId || !deviceList.TryGetValueAtIndex(levelerConsoleText.DeviceId, out var item) || !(item is MyRvLinkDeviceIdsCan device))
			{
				return;
			}
			ILogicalDevice logicalDevice = _myRvLinkDeviceManager.GetLogicalDevice(device);
			if (!(logicalDevice is ILogicalDeviceLevelerType1 logicalDeviceLevelerType))
			{
				if (!(logicalDevice is ILogicalDeviceLevelerType3 logicalDeviceLevelerType2))
				{
					if (logicalDevice is ILogicalDeviceLevelerType4 logicalDeviceLevelerType3)
					{
						logicalDeviceLevelerType3.UpdateDeviceConsoleText(levelerConsoleText.GetConsoleMessages());
						return;
					}
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(115, 2);
					defaultInterpolatedStringHandler.AppendFormatted("UpdateLevelerConsoleText");
					defaultInterpolatedStringHandler.AppendLiteral(" Unable to update leveler console messages because can't find logical device for ");
					defaultInterpolatedStringHandler.AppendFormatted(this);
					defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a Leveler 3 or 4.");
					TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				else
				{
					logicalDeviceLevelerType2.UpdateDeviceConsoleText(levelerConsoleText.GetConsoleMessages());
				}
			}
			else
			{
				logicalDeviceLevelerType.UpdateDeviceConsoleText(levelerConsoleText.GetConsoleMessages());
			}
		}

		internal void UpdateLockStatus(MyRvLinkDeviceLockStatus? deviceLockStatus)
		{
			if (DeviceList == null)
			{
				return;
			}
			if (deviceLockStatus == null)
			{
				_myRvLinkDeviceManager.RemoveInTransitLockoutLevel(forceRemoveLockout: true);
			}
			else
			{
				if (deviceLockStatus!.DeviceTableId != DeviceTableId)
				{
					return;
				}
				byte systemLockoutLevel = deviceLockStatus!.SystemLockoutLevel;
				CachedInMotionLockoutLevel = systemLockoutLevel;
				foreach (var item3 in deviceLockStatus!.EnumerateIsDeviceLocked())
				{
					byte item = item3.DeviceId;
					bool item2 = item3.isLocked;
					ILogicalDevice logicalDeviceFromMyRvDevice = GetLogicalDeviceFromMyRvDevice(deviceLockStatus!.DeviceTableId, item);
					if (logicalDeviceFromMyRvDevice != null)
					{
						_myRvLinkDeviceManager.UpdateInTransitLockoutLevel(logicalDeviceFromMyRvDevice.LogicalId, (byte)(item2 ? systemLockoutLevel : 0));
					}
				}
				_myRvLinkDeviceManager.LogicalDeviceDefaultChassisInfo?.UpdateDeviceStatus(deviceLockStatus!.ChassisInfoStatus);
			}
		}

		internal void UpdateMonitorPanelStatus(MyRvLinkMonitorPanelStatus? monitorPanelStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || monitorPanelStatus == null || monitorPanelStatus!.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, status) in monitorPanelStatus!.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceMonitorPanelDirect logicalDeviceMonitorPanelDirect))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 3);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateMonitorPanelStatus");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(monitorPanelStatus);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else
					{
						logicalDeviceMonitorPanelDirect.UpdateDeviceStatus(status);
					}
				}
			}
		}

		internal void UpdateOnlineStatus(MyRvLinkDeviceOnlineStatus? deviceOnlineStatus)
		{
			if (DeviceList == null)
			{
				return;
			}
			if (deviceOnlineStatus == null)
			{
				TaggedLog.Debug("MyRvLinkDeviceTracker", "Taking all devices offline");
				_myRvLinkDeviceManager.TakeDevicesOfflineIfNeeded(forceOffline: true);
			}
			else
			{
				if (deviceOnlineStatus!.DeviceTableId != DeviceTableId)
				{
					return;
				}
				foreach (var item3 in deviceOnlineStatus!.EnumerateIsDeviceOnline())
				{
					byte item = item3.DeviceId;
					bool item2 = item3.isOnline;
					ILogicalDevice logicalDeviceFromMyRvDevice = GetLogicalDeviceFromMyRvDevice(deviceOnlineStatus!.DeviceTableId, item);
					_myRvLinkDeviceManager.UpdateLogicalDeviceOnline(logicalDeviceFromMyRvDevice?.LogicalId, item2);
				}
			}
		}

		internal void UpdateRelayBasicLatchingStatus(MyRvLinkRelayBasicLatchingStatusType2? latchingRelayStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || latchingRelayStatus == null || latchingRelayStatus!.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceRelayBasicStatusType) in latchingRelayStatus!.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceLatchingRelay<LogicalDeviceRelayBasicStatusType2> logicalDeviceLatchingRelay))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkRelayBasicLatchingStatusType2");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceRelayBasicStatusType);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceRelayHBridge");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else if (!logicalDeviceLatchingRelay.DeviceStatus.EqualsData(logicalDeviceRelayBasicStatusType))
					{
						logicalDeviceLatchingRelay.UpdateDeviceStatus(logicalDeviceRelayBasicStatusType);
					}
				}
			}
		}

		internal void UpdateRelayBasicLatchingStatus(MyRvLinkRelayBasicLatchingStatusType1? latchingRelayStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || latchingRelayStatus == null || latchingRelayStatus!.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceRelayBasicStatusType) in latchingRelayStatus!.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceLatchingRelay<LogicalDeviceRelayBasicStatusType1> logicalDeviceLatchingRelay))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkRelayBasicLatchingStatusType2");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceRelayBasicStatusType);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceRelayHBridge");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else if (!logicalDeviceLatchingRelay.DeviceStatus.EqualsData(logicalDeviceRelayBasicStatusType))
					{
						logicalDeviceLatchingRelay.UpdateDeviceStatus(logicalDeviceRelayBasicStatusType);
					}
				}
			}
		}

		internal void UpdateRelayHBridgeMomentaryStatus(MyRvLinkRelayHBridgeMomentaryStatusType1? momentaryRelayStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || momentaryRelayStatus == null || momentaryRelayStatus!.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceRelayHBridgeStatusType) in momentaryRelayStatus!.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceRelayHBridge<LogicalDeviceRelayHBridgeStatusType1> logicalDeviceRelayHBridge))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkRelayHBridgeMomentaryStatusType1");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceRelayHBridgeStatusType);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceRelayHBridge");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else if (!logicalDeviceRelayHBridge.DeviceStatus.EqualsData(logicalDeviceRelayHBridgeStatusType))
					{
						logicalDeviceRelayHBridge.UpdateDeviceStatus(logicalDeviceRelayHBridgeStatusType);
					}
				}
			}
		}

		internal void UpdateRelayHBridgeMomentaryStatus(MyRvLinkRelayHBridgeMomentaryStatusType2? momentaryRelayStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || momentaryRelayStatus == null || momentaryRelayStatus!.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceRelayHBridgeStatusType) in momentaryRelayStatus!.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceRelayHBridge<LogicalDeviceRelayHBridgeStatusType2> logicalDeviceRelayHBridge))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("MyRvLinkRelayBasicLatchingStatusType2");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceRelayHBridgeStatusType);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceRelayHBridge");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else if (!logicalDeviceRelayHBridge.DeviceStatus.EqualsData(logicalDeviceRelayHBridgeStatusType))
					{
						logicalDeviceRelayHBridge.UpdateDeviceStatus(logicalDeviceRelayHBridgeStatusType);
					}
				}
			}
		}

		internal void UpdateRgbLightStatus(MyRvLinkRgbLightStatus rgbLightStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || rgbLightStatus == null || rgbLightStatus.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceLightRgbStatus) in rgbLightStatus.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceLightRgbDirect logicalDeviceLightRgbDirect))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateRgbLightStatus");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceLightRgbStatus);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceLightRgbDirect");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
						break;
					}
					if (!logicalDeviceLightRgbDirect.DeviceStatus.EqualsData(logicalDeviceLightRgbStatus))
					{
						logicalDeviceLightRgbDirect.UpdateDeviceStatus(logicalDeviceLightRgbStatus);
					}
				}
			}
		}

		internal void UpdateSessionStatus(MyRvLinkDeviceSessionStatus? deviceOnlineStatus)
		{
			if (DeviceList != null)
			{
				_ = deviceOnlineStatus?.DeviceTableId == DeviceTableId;
			}
		}

		internal void UpdateTankSensorStatus(MyRvLinkTankSensorStatus? tankSensorStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || tankSensorStatus == null || tankSensorStatus!.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, level) in tankSensorStatus!.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceDirectTankSensor logicalDeviceDirectTankSensor))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(82, 3);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateTankSensorStatus");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceDirectTankSensor");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
						break;
					}
					LogicalDeviceTankSensorStatus logicalDeviceTankSensorStatus = new LogicalDeviceTankSensorStatus();
					logicalDeviceTankSensorStatus.SetLevel(level);
					if (!logicalDeviceDirectTankSensor.DeviceStatus.EqualsData(logicalDeviceTankSensorStatus))
					{
						logicalDeviceDirectTankSensor.UpdateDeviceStatus(logicalDeviceTankSensorStatus);
					}
				}
			}
		}

		internal void UpdateTankSensorStatusV2(MyRvLinkTankSensorStatusV2? tankSensorStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || tankSensorStatus == null || tankSensorStatus!.DeviceTableId != DeviceTableId || !deviceList.TryGetValueAtIndex(tankSensorStatus!.DeviceId, out var item) || !(item is MyRvLinkDeviceIdsCan device))
			{
				return;
			}
			if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceTankSensor logicalDeviceTankSensor))
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
				defaultInterpolatedStringHandler.AppendFormatted("UpdateTankSensorStatusV2");
				defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
				defaultInterpolatedStringHandler.AppendFormatted(tankSensorStatus);
				defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
				defaultInterpolatedStringHandler.AppendFormatted(this);
				defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
				defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceTankSensor");
				TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			else
			{
				LogicalDeviceTankSensorStatus tankSensorStatus2 = tankSensorStatus!.GetTankSensorStatus();
				if (tankSensorStatus2 != null && !logicalDeviceTankSensor.DeviceStatus.EqualsData(tankSensorStatus2))
				{
					logicalDeviceTankSensor.UpdateDeviceStatus(tankSensorStatus2.Data, tankSensorStatus2.Size);
				}
			}
		}

		internal void UpdateTemperatureSensorStatus(MyRvLinkTemperatureSensorStatus temperatureSensorStatus)
		{
			List<IMyRvLinkDevice> deviceList = DeviceList;
			if (deviceList == null || temperatureSensorStatus == null || temperatureSensorStatus.DeviceTableId != DeviceTableId)
			{
				return;
			}
			foreach (var (index, logicalDeviceTemperatureSensorStatus) in temperatureSensorStatus.EnumerateStatus())
			{
				if (deviceList.TryGetValueAtIndex(index, out var item) && item is MyRvLinkDeviceIdsCan device)
				{
					if (!(_myRvLinkDeviceManager.GetLogicalDevice(device) is ILogicalDeviceTemperatureSensor logicalDeviceTemperatureSensor))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 4);
						defaultInterpolatedStringHandler.AppendFormatted("UpdateTemperatureSensorStatus");
						defaultInterpolatedStringHandler.AppendLiteral(" Unable to update status to ");
						defaultInterpolatedStringHandler.AppendFormatted(logicalDeviceTemperatureSensorStatus);
						defaultInterpolatedStringHandler.AppendLiteral(" because can't find logical device for ");
						defaultInterpolatedStringHandler.AppendFormatted(this);
						defaultInterpolatedStringHandler.AppendLiteral(" OR device isn't a ");
						defaultInterpolatedStringHandler.AppendFormatted("ILogicalDeviceTemperatureSensor");
						TaggedLog.Warning("MyRvLinkDeviceTracker", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					else if (!logicalDeviceTemperatureSensor.DeviceStatus.EqualsData(logicalDeviceTemperatureSensorStatus))
					{
						logicalDeviceTemperatureSensor.UpdateDeviceStatus(logicalDeviceTemperatureSensorStatus.Data, logicalDeviceTemperatureSensorStatus.Size);
					}
				}
			}
		}
	}
}
