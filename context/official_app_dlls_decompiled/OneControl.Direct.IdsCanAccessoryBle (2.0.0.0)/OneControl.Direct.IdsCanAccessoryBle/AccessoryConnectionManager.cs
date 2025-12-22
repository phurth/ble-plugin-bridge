using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using ids.portable.ble.Ble;
using ids.portable.ble.BleManager;
using ids.portable.ble.Exceptions;
using ids.portable.ble.Platforms.Shared;
using ids.portable.ble.Platforms.Shared.ScanResults;
using ids.portable.common;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.BlockTransfer;
using IDS.Portable.LogicalDevice.FirmwareUpdate;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;

namespace OneControl.Direct.IdsCanAccessoryBle
{
	public static class AccessoryConnectionManager
	{
		public static readonly Guid AccessoryPrimaryServiceDefault = IdsCanAccessoryBleScanResultPrimaryServiceFactory.AccessoryPrimaryService;

		public static readonly Guid KeySeedExchangeServiceGuidDefault = IdsCanAccessoryBleScanResultPrimaryServiceFactory.KeySeedExchangeServiceGuidDefault;

		public static readonly Guid SeedCharacteristicGuidDefault = IdsCanAccessoryBleScanResultPrimaryServiceFactory.SeedCharacteristicGuidDefault;

		public static readonly Guid KeyCharacteristicGuidDefault = IdsCanAccessoryBleScanResultPrimaryServiceFactory.KeyCharacteristicGuidDefault;

		public static readonly Guid PidWriteCharacteristic = IdsCanAccessoryBleScanResultPrimaryServiceFactory.PidWriteCharacteristic;

		public static readonly Guid ReadHistoryDataCharacteristic = IdsCanAccessoryBleScanResultPrimaryServiceFactory.ReadHistoryDataCharacteristic;

		public static readonly Guid WriteHistoryDataCharacteristic = IdsCanAccessoryBleScanResultPrimaryServiceFactory.WriteHistoryDataCharacteristic;

		public static readonly Guid ReadSoftwarePartNumberCharacteristic = IdsCanAccessoryBleScanResultPrimaryServiceFactory.ReadSoftwarePartNumberCharacteristic;

		public const uint KeySeedExchangeCypher = 2645682455u;

		public static readonly Guid AccessoryBleOtaServiceDefault = IdsCanAccessoryBleScanResultPrimaryServiceFactory.BleOtaService;

		public static readonly Guid UnlockGetSeedCharacteristic = IdsCanAccessoryBleScanResultPrimaryServiceFactory.UnlockGetSeedCharacteristic;

		public static readonly Guid UnlockWriteKeyCharacteristic = IdsCanAccessoryBleScanResultPrimaryServiceFactory.UnlockWriteKeyCharacteristic;

		public static readonly Guid BlockPropertiesCharacteristic = IdsCanAccessoryBleScanResultPrimaryServiceFactory.BlockPropertiesCharacteristic;

		public static readonly Guid BlockBeginTransferCharacteristic = IdsCanAccessoryBleScanResultPrimaryServiceFactory.BlockBeginTransferCharacteristic;

		public static readonly Guid WriteBulkTransferDataCharacteristic = IdsCanAccessoryBleScanResultPrimaryServiceFactory.WriteBulkTransferDataCharacteristic;

		public static readonly Guid EndTransferCrcCheckCharacteristic = IdsCanAccessoryBleScanResultPrimaryServiceFactory.EndTransferCrcCheckCharacteristic;

		public static readonly Guid GetCrcCharacteristic = IdsCanAccessoryBleScanResultPrimaryServiceFactory.GetCrcCharacteristic;

		public static readonly Guid OtaRebootCharacteristic = IdsCanAccessoryBleScanResultPrimaryServiceFactory.OtaRebootCharacteristic;

		public const int BleConnectionAutoCloseTimeoutMsDefault = 30000;

		public const int BleConnectTimeoutMaxMsDefault = 160000;

		public const int BleConnectAttemptMsDefault = 80000;

		public const int BleConnectionRetryDelayMsDefault = 200;

		public const int FunctionInstanceMaxValueDefault = 15;
	}
	public class AccessoryConnectionManager<TLogicalDevice> : CommonDisposable, IFirmwareUpdateDevice where TLogicalDevice : ILogicalDevice
	{
		public class BlockWriteTimeTracker
		{
			public enum TrackId
			{
				None,
				ProgressAck,
				BufferCopy,
				UpdateAndSendCommand,
				CrcCheck,
				WaitingForSafeToReboot,
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

		private readonly IBleService _bleService;

		private readonly string LogTag;

		public const int DefaultBleMtuSize = 185;

		private readonly int _bleConnectTimeoutMaxMs;

		private readonly int _bleConnectAttemptMs;

		private readonly int _bleConnectionRetryDelayMs;

		private readonly TLogicalDevice _logicalDevice;

		private readonly Guid _bleDeviceId;

		private readonly string _bleDeviceName;

		private readonly TaskSerialQueue _getSharedConnectionSerialQueue = new TaskSerialQueue(100);

		private readonly Watchdog? _bleConnectionWatchdog;

		private Plugin.BLE.Abstractions.Contracts.IDevice? _bleDevice;

		private bool _manualCloseRequired;

		public const int WaitForRebootIntoBootLoaderDelayMs = 1000;

		private const int ErrorDelayMilliseconds = 200;

		public const int WaitForRebootIntoBootloaderAttempts = 20;

		public const int DefaultJumpToBootMs = 10000;

		private const int BlockPropertiesLength = 13;

		private const int BlockPropertiesBlockFlagsIndex = 0;

		private const int BlockPropertiesBlockCapacityIndex = 1;

		private const int BlockPropertiesBlockCurrentSizeIndex = 5;

		private const int BlockPropertiesBlockStartAddressIndex = 9;

		private const int BeginBlockTransferBytesLength = 8;

		private const int BeginBlockTransferStartAddressOffset = 0;

		private const int BeginBlockTransferSizeOffset = 4;

		private const int MaxDataBlockSize = 181;

		private const int SequenceNumberLength = 2;

		private const int CrcCheckBytesLength = 8;

		private const int OtaRebootWindowMs = 10000;

		private const int OtaRebootWindowOffset = 4;

		private const int MaximumSafeToRebootCheckAttempts = 5;

		private const int SafeToRebootCheckDelayMs = 2000;

		private const int MillisecondsPerSecond = 1000;

		private const int ReadPayloadDelayTimeMs = 100;

		private const int HistoryDataBlockIndex = 0;

		private const int HistoryDataStartIndex = 1;

		private const int HistoryDataLengthIndex = 2;

		private const int HistoryDataRequestSize = 3;

		private const int HistoryDataWriteSize = 3;

		private const int HistoryDataReadMinimumSize = 3;

		private const int PidWriteDataSize = 8;

		private const int PidStartIndex = 0;

		private const int PidValueStartIndex = 2;

		public const int PidWriteMaxRetryCount = 3;

		private const int SoftwarePartNumberSize = 6;

		private const int SoftwarePartNumberHyphenIndex = 5;

		private FunctionName? _accessoryFunctionNameCached;

		private int? _accessoryFunctionInstanceCached;

		public bool IsSharedConnectionActive
		{
			get
			{
				Plugin.BLE.Abstractions.Contracts.IDevice bleDevice = _bleDevice;
				if (bleDevice != null)
				{
					return bleDevice.State == DeviceState.Connected;
				}
				return false;
			}
		}

		public int PidWriteVerifyRetryCount { get; internal set; }

		public int PidWriteRetryDelayMs { get; internal set; }

		public int PidWriteVerifyDelayMs { get; internal set; }

		protected FunctionName AccessoryFunctionName
		{
			get
			{
				FunctionName valueOrDefault = _accessoryFunctionNameCached.GetValueOrDefault();
				if (!_accessoryFunctionNameCached.HasValue)
				{
					TLogicalDevice logicalDevice = _logicalDevice;
					valueOrDefault = logicalDevice.LogicalId.FunctionName.ToFunctionName();
					_accessoryFunctionNameCached = valueOrDefault;
					return valueOrDefault;
				}
				return valueOrDefault;
			}
		}

		protected int AccessoryFunctionInstance
		{
			get
			{
				int valueOrDefault = _accessoryFunctionInstanceCached.GetValueOrDefault();
				if (!_accessoryFunctionInstanceCached.HasValue)
				{
					TLogicalDevice logicalDevice = _logicalDevice;
					valueOrDefault = logicalDevice.LogicalId.FunctionInstance;
					_accessoryFunctionInstanceCached = valueOrDefault;
					return valueOrDefault;
				}
				return valueOrDefault;
			}
		}

		public AccessoryConnectionManager(IBleService bleService, string logTag, int bleConnectionAutoCloseTimeoutMs, int connectionTimeoutMaxMs, int bleConnectAttemptMs, int bleConnectionRetryDelayMs, TLogicalDevice logicalDevice, Guid bleDeviceId, string bleDeviceName)
		{
			_bleService = bleService;
			LogTag = logTag;
			_bleConnectTimeoutMaxMs = connectionTimeoutMaxMs;
			_bleConnectAttemptMs = bleConnectAttemptMs;
			_bleConnectionRetryDelayMs = bleConnectionRetryDelayMs;
			_logicalDevice = logicalDevice;
			_bleDeviceId = bleDeviceId;
			_bleDeviceName = bleDeviceName;
			_bleConnectionWatchdog = new Watchdog(bleConnectionAutoCloseTimeoutMs, OnBleConnectionAutoClose, autoStartOnFirstPet: true);
		}

		public async Task<Plugin.BLE.Abstractions.Contracts.IDevice> GetSharedConnectionAsync(CancellationToken cancellationToken, AccessorySharedConnectionOption option = AccessorySharedConnectionOption.None)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (option.HasFlag(AccessorySharedConnectionOption.ManualClose))
			{
				_manualCloseRequired = true;
			}
			using (await _getSharedConnectionSerialQueue.GetLockAsync(cancellationToken))
			{
				TLogicalDevice logicalDevice = _logicalDevice;
				string accessoryDescription = logicalDevice.LogicalId.ToString(LogicalDeviceIdFormat.Debug);
				Plugin.BLE.Abstractions.Contracts.IDevice bleDevice = _bleDevice;
				if (bleDevice != null && bleDevice.State == DeviceState.Connected)
				{
					_bleConnectionWatchdog?.TryPet(autoReset: true);
					TaggedLog.Debug(LogTag, "Reusing Accessory connection for " + accessoryDescription);
					return _bleDevice;
				}
				if (option.HasFlag(AccessorySharedConnectionOption.DontConnect))
				{
					throw new InvalidOperationException("There is no currently active connection");
				}
				_bleDevice?.TryDispose();
				_bleDevice = null;
				Stopwatch connectionStopwatch = Stopwatch.StartNew();
				Exception connectionFailure = null;
				while (connectionStopwatch.ElapsedMilliseconds < _bleConnectTimeoutMaxMs)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						TaggedLog.Information(LogTag, "Connecting to Accessory for " + accessoryDescription + " CANCEL requested");
						throw new OperationCanceledException("Connecting to Accessory for " + accessoryDescription + " CANCEL requested");
					}
					try
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
						if (connectionFailure == null)
						{
							TaggedLog.Information(LogTag, "Connecting to Accessory for " + accessoryDescription);
						}
						else
						{
							string logTag = LogTag;
							defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(52, 3);
							defaultInterpolatedStringHandler.AppendLiteral("Connecting to Accessory RETRY for ");
							defaultInterpolatedStringHandler.AppendFormatted(accessoryDescription);
							defaultInterpolatedStringHandler.AppendLiteral(" because of ");
							defaultInterpolatedStringHandler.AppendFormatted(connectionFailure.Message);
							defaultInterpolatedStringHandler.AppendLiteral(" @ ~");
							defaultInterpolatedStringHandler.AppendFormatted(connectionStopwatch.ElapsedMilliseconds);
							defaultInterpolatedStringHandler.AppendLiteral("ms");
							TaggedLog.Information(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
						}
						BleManagerConnectionParameters bleManagerConnectionParameters = new BleManagerConnectionParameters(_bleDeviceId, _bleDeviceName, PairingMethod.None, null, _bleConnectAttemptMs);
						_bleDevice = await _bleService.Manager.ConnectToDeviceAsync(bleManagerConnectionParameters, cancellationToken);
						if (_bleDevice!.State != DeviceState.Connected)
						{
							throw new BleManagerConnectionException("Connect to Device returned w/o being connected.", connectionFailure);
						}
						int num = await _bleDevice!.RequestMtuAsync(185);
						string logTag2 = LogTag;
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Connected to Accessory for ");
						defaultInterpolatedStringHandler.AppendFormatted(accessoryDescription);
						defaultInterpolatedStringHandler.AppendLiteral(" with MTU ");
						defaultInterpolatedStringHandler.AppendFormatted(num);
						defaultInterpolatedStringHandler.AppendLiteral(" SUCCESS");
						TaggedLog.Information(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
						connectionFailure = null;
					}
					catch (Exception ex)
					{
						connectionFailure = ex;
						_bleDevice?.TryDispose();
						_bleDevice = null;
						await TaskExtension.TryDelay(_bleConnectionRetryDelayMs, cancellationToken);
						continue;
					}
					break;
				}
				if (connectionFailure != null || _bleDevice == null)
				{
					string logTag3 = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to connect to Accessory ");
					defaultInterpolatedStringHandler.AppendFormatted(accessoryDescription);
					defaultInterpolatedStringHandler.AppendLiteral(": ");
					defaultInterpolatedStringHandler.AppendFormatted(connectionFailure);
					TaggedLog.Error(logTag3, defaultInterpolatedStringHandler.ToStringAndClear());
					_bleDevice?.TryDispose();
					_bleDevice = null;
					throw new BleManagerConnectionException("Could not connect to Accessory.", connectionFailure);
				}
				try
				{
					TaggedLog.Information(LogTag, "Connected to Accessory " + accessoryDescription + " attempting unlock");
					if (_bleService.Manager.UseKeySeed && await _bleService.Manager.PerformKeySeedExchange(_bleDevice, 2645682455u, AccessoryConnectionManager.KeySeedExchangeServiceGuidDefault, AccessoryConnectionManager.SeedCharacteristicGuidDefault, AccessoryConnectionManager.KeyCharacteristicGuidDefault, cancellationToken) != BleDeviceKeySeedExchangeResult.Succeeded)
					{
						throw new BleManagerConnectionKeySeedException("Failed KeySeed exchange.");
					}
				}
				catch (Exception ex2)
				{
					TaggedLog.Error(LogTag, "Connected to Accessory " + accessoryDescription + " KeySeed exchange failed: " + ex2.Message);
					_bleDevice.TryDispose();
					_bleDevice = null;
					throw;
				}
				TaggedLog.Information(LogTag, "Connected to Accessory " + accessoryDescription + " SUCCESS");
				_bleConnectionWatchdog?.TryPet(autoReset: true);
				return _bleDevice;
			}
		}

		public void CloseSharedConnectionAsync(bool force = false)
		{
			_manualCloseRequired = false;
			if (!force)
			{
				Watchdog bleConnectionWatchdog = _bleConnectionWatchdog;
				if (bleConnectionWatchdog != null && !bleConnectionWatchdog.Triggered && !bleConnectionWatchdog.IsDisposed)
				{
					return;
				}
			}
			Task.Run(async delegate
			{
				using (await _getSharedConnectionSerialQueue.GetLockAsync(CancellationToken.None))
				{
					if (_bleDevice == null)
					{
						return;
					}
					string logTag = LogTag;
					TLogicalDevice logicalDevice = _logicalDevice;
					TaggedLog.Debug(logTag, "Closing Accessory connection for " + logicalDevice.LogicalId.ToString(LogicalDeviceIdFormat.Debug));
					await _bleService.Manager.TryDisconnectDeviceAsync(_bleDevice);
					_bleDevice?.TryDispose();
					_bleDevice = null;
				}
			});
		}

		private void OnBleConnectionAutoClose()
		{
			if (_manualCloseRequired)
			{
				return;
			}
			Task.Run(async delegate
			{
				using (await _getSharedConnectionSerialQueue.GetLockAsync(CancellationToken.None))
				{
					Watchdog bleConnectionWatchdog = _bleConnectionWatchdog;
					if ((bleConnectionWatchdog != null && !bleConnectionWatchdog.Triggered && !bleConnectionWatchdog.IsDisposed) || _bleDevice == null)
					{
						return;
					}
					string logTag = LogTag;
					TLogicalDevice logicalDevice = _logicalDevice;
					TaggedLog.Debug(logTag, "Auto closing Accessory connection for " + logicalDevice.LogicalId.ToString(LogicalDeviceIdFormat.Debug));
					await _bleService.Manager.TryDisconnectDeviceAsync(_bleDevice);
					_bleDevice?.TryDispose();
					_bleDevice = null;
				}
			});
		}

		public override void Dispose(bool disposing)
		{
			_bleConnectionWatchdog?.TryDispose();
			OnBleConnectionAutoClose();
		}

		public Task<FirmwareUpdateSupport> TryGetFirmwareUpdateSupportAsync(ILogicalDevice logicalDevice, CancellationToken cancelToken)
		{
			try
			{
				if (logicalDevice.ActiveConnection == LogicalDeviceActiveConnection.Offline)
				{
					return Task.FromResult(FirmwareUpdateSupport.DeviceOffline);
				}
				if (!(logicalDevice.DeviceService.GetPrimaryDeviceSourceDirect(logicalDevice) is IAccessoryBleDeviceSourceLocap))
				{
					return Task.FromResult(FirmwareUpdateSupport.NotSupported);
				}
				if (logicalDevice is ILogicalDeviceJumpToBootloader logicalDeviceJumpToBootloader && logicalDeviceJumpToBootloader.IsJumpToBootRequiredForFirmwareUpdate)
				{
					return Task.FromResult(FirmwareUpdateSupport.SupportedViaBootloader);
				}
				return Task.FromResult(FirmwareUpdateSupport.SupportedViaDevice);
			}
			catch (Exception ex)
			{
				TaggedLog.Warning(LogTag, "Unable to determine if firmware update is supported: " + ex.Message);
				return Task.FromResult(FirmwareUpdateSupport.Unknown);
			}
		}

		public async Task UpdateFirmwareAsync(ILogicalDeviceFirmwareUpdateSession firmwareUpdateSession, IReadOnlyList<byte> data, Func<ILogicalDeviceTransferProgress, bool> progressAck, CancellationToken cancellationToken, IReadOnlyDictionary<FirmwareUpdateOption, object>? options = null)
		{
			if (firmwareUpdateSession.IsDisposed)
			{
				TaggedLog.Warning(LogTag, "Unable to Update Firmware because update session is disposed AccessoryConnectionManager");
				throw new FirmwareUpdateSessionDisposedException();
			}
			ILogicalDeviceFirmwareUpdateDevice logicalDeviceToReflash = firmwareUpdateSession.LogicalDevice;
			if (options == null)
			{
				options = new Dictionary<FirmwareUpdateOption, object>();
			}
			if (logicalDeviceToReflash is ILogicalDeviceJumpToBootloader logicalDeviceJumpToBootloader && logicalDeviceJumpToBootloader.IsJumpToBootRequiredForFirmwareUpdate)
			{
				if (!options.TryGetJumpToBootHoldTime(out var holdTime))
				{
					holdTime = TimeSpan.FromMilliseconds(10000.0);
					string logTag = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(72, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Jump to boot time not specified but required so using a default time of ");
					defaultInterpolatedStringHandler.AppendFormatted(holdTime);
					TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				}
				await JumpToBootloaderAsync(logicalDeviceJumpToBootloader, holdTime, cancellationToken);
			}
			await UpdateFirmwareInternalAsync(logicalDeviceToReflash, data, progressAck, cancellationToken, options);
		}

		public async Task JumpToBootloaderAsync(ILogicalDeviceJumpToBootloader logicalDevice, TimeSpan holdTime, CancellationToken cancellationToken)
		{
			Plugin.BLE.Abstractions.Contracts.IDevice device = (await GetSharedConnectionAsync(cancellationToken)) ?? throw new AccessoryConnectionManagerAccessoryOfflineException(_logicalDevice);
			if (!(await _bleService.Manager.WriteCharacteristicAsync(device, AccessoryConnectionManager.AccessoryBleOtaServiceDefault, AccessoryConnectionManager.OtaRebootCharacteristic, new byte[1] { 1 }, cancellationToken)))
			{
				throw new AccessoryConnectionManagerWriteFailedException(logicalDevice);
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			for (int attempt = 0; attempt < 20; attempt++)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await Task.Delay(1000, cancellationToken);
				try
				{
					await GetSharedOtaConnectionAsync(cancellationToken, AccessorySharedConnectionOption.ManualClose);
					return;
				}
				catch (Exception ex)
				{
					string logTag = LogTag;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Error getting connection to OTA Device: ");
					defaultInterpolatedStringHandler.AppendFormatted(ex);
					TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				}
			}
			string logTag2 = LogTag;
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Failed to enter/find bootloader for ");
			defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
			TaggedLog.Information(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
			throw new FirmwareUpdateBootloaderException("Unable to find Bootloader Device");
		}

		private async Task UpdateFirmwareInternalAsync(ILogicalDevice logicalDevice, IReadOnlyList<byte> data, Func<ILogicalDeviceTransferProgress, bool> progressAck, CancellationToken cancellationToken, IReadOnlyDictionary<FirmwareUpdateOption, object> options)
		{
			_ = 4;
			try
			{
				Plugin.BLE.Abstractions.Contracts.IDevice deviceToReflash = await GetSharedOtaConnectionAsync(cancellationToken, AccessorySharedConnectionOption.ManualClose);
				FirmwareUpdateSupport firmwareUpdateSupport = await TryGetFirmwareUpdateSupportAsync(logicalDevice, cancellationToken);
				if (firmwareUpdateSupport != FirmwareUpdateSupport.SupportedViaDevice && firmwareUpdateSupport != FirmwareUpdateSupport.SupportedViaBootloader)
				{
					throw new FirmwareUpdateNotSupportedException(logicalDevice, firmwareUpdateSupport);
				}
				byte[] array = await _bleService.Manager.ReadCharacteristicAsync(deviceToReflash, AccessoryConnectionManager.AccessoryBleOtaServiceDefault, AccessoryConnectionManager.BlockPropertiesCharacteristic, cancellationToken);
				if (array == null)
				{
					throw new AccessoryConnectionManagerReadFailedException(logicalDevice);
				}
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
				if (array.Length != 13)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Block properties length: ");
					defaultInterpolatedStringHandler.AppendFormatted(array.Length);
					defaultInterpolatedStringHandler.AppendLiteral(" Expected: ");
					defaultInterpolatedStringHandler.AppendFormatted(13);
					throw new AccessoryConnectionManagerReadDataException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				_ = array[0];
				uint valueUInt = array.GetValueUInt32(1);
				uint valueUInt2 = array.GetValueUInt32(5);
				uint valueUInt3 = array.GetValueUInt32(9);
				string logTag = LogTag;
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(62, 4);
				defaultInterpolatedStringHandler.AppendLiteral("blockFlags: ");
				defaultInterpolatedStringHandler.AppendFormatted(array[0]);
				defaultInterpolatedStringHandler.AppendLiteral(" blockCapacity: ");
				defaultInterpolatedStringHandler.AppendFormatted(valueUInt);
				defaultInterpolatedStringHandler.AppendLiteral(" currentSize: ");
				defaultInterpolatedStringHandler.AppendFormatted(valueUInt2);
				defaultInterpolatedStringHandler.AppendLiteral(" blockStartAddress: ");
				defaultInterpolatedStringHandler.AppendFormatted(valueUInt3);
				TaggedLog.Information(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				byte[] array2 = new byte[8];
				array2.SetValueUInt32(valueUInt3, 0);
				array2.SetValueUInt32(valueUInt2, 4);
				if (!(await _bleService.Manager.WriteCharacteristicWithResponseAsync(deviceToReflash, AccessoryConnectionManager.AccessoryBleOtaServiceDefault, AccessoryConnectionManager.BlockBeginTransferCharacteristic, array2, cancellationToken)))
				{
					throw new AccessoryConnectionManagerWriteFailedException(logicalDevice);
				}
				await DeviceBlockWriteAsync(deviceToReflash, logicalDevice, data, progressAck, cancellationToken);
			}
			catch (AccessoryConnectionManagerReadFailedException ex)
			{
				TaggedLog.Warning(LogTag, "Characteristic read failed: " + ex.Message);
				throw;
			}
			catch (AccessoryConnectionManagerWriteFailedException ex2)
			{
				TaggedLog.Warning(LogTag, "Characteristic write failed: " + ex2.Message);
				throw;
			}
			catch (AccessoryConnectionManagerReadDataException ex3)
			{
				TaggedLog.Warning(LogTag, "Received unexpected data from the device: " + ex3.Message);
				throw;
			}
			catch (Exception ex4)
			{
				string logTag2 = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to Update Firmware - ");
				defaultInterpolatedStringHandler.AppendFormatted(ex4);
				TaggedLog.Error(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
				throw new BlockTransferException("Unable to Update Firmware", ex4);
			}
		}

		private async Task<Plugin.BLE.Abstractions.Contracts.IDevice> GetSharedOtaConnectionAsync(CancellationToken cancellationToken, AccessorySharedConnectionOption option = AccessorySharedConnectionOption.None)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (option.HasFlag(AccessorySharedConnectionOption.ManualClose))
			{
				_manualCloseRequired = true;
			}
			using (await _getSharedConnectionSerialQueue.GetLockAsync(cancellationToken))
			{
				TLogicalDevice logicalDevice = _logicalDevice;
				string accessoryDescription = logicalDevice.LogicalId.ToString(LogicalDeviceIdFormat.Debug);
				Plugin.BLE.Abstractions.Contracts.IDevice bleDevice = _bleDevice;
				if (bleDevice != null && bleDevice.State == DeviceState.Connected)
				{
					_bleConnectionWatchdog?.TryPet(autoReset: true);
					TaggedLog.Debug(LogTag, "Reusing Accessory connection for " + accessoryDescription);
					return _bleDevice;
				}
				if (option.HasFlag(AccessorySharedConnectionOption.DontConnect))
				{
					throw new InvalidOperationException("There is no currently active connection");
				}
				_bleDevice?.TryDispose();
				_bleDevice = null;
				Stopwatch connectionStopwatch = Stopwatch.StartNew();
				Exception connectionFailure = null;
				while (connectionStopwatch.ElapsedMilliseconds < _bleConnectTimeoutMaxMs)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						TaggedLog.Information(LogTag, "Connecting to Accessory for " + accessoryDescription + " CANCEL requested");
						throw new OperationCanceledException("Connecting to Accessory for " + accessoryDescription + " CANCEL requested");
					}
					try
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
						if (connectionFailure == null)
						{
							TaggedLog.Information(LogTag, "Connecting to Accessory for " + accessoryDescription);
						}
						else
						{
							string logTag = LogTag;
							defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(52, 3);
							defaultInterpolatedStringHandler.AppendLiteral("Connecting to Accessory RETRY for ");
							defaultInterpolatedStringHandler.AppendFormatted(accessoryDescription);
							defaultInterpolatedStringHandler.AppendLiteral(" because of ");
							defaultInterpolatedStringHandler.AppendFormatted(connectionFailure.Message);
							defaultInterpolatedStringHandler.AppendLiteral(" @ ~");
							defaultInterpolatedStringHandler.AppendFormatted(connectionStopwatch.ElapsedMilliseconds);
							defaultInterpolatedStringHandler.AppendLiteral("ms");
							TaggedLog.Information(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
						}
						BleManagerConnectionParameters bleManagerConnectionParameters = new BleManagerConnectionParameters(_bleDeviceId, _bleDeviceName, PairingMethod.None, null, _bleConnectAttemptMs);
						_bleDevice = await _bleService.Manager.ConnectToDeviceAsync(bleManagerConnectionParameters, cancellationToken);
						if (_bleDevice!.State != DeviceState.Connected)
						{
							throw new BleManagerConnectionException("Connect to Device returned w/o being connected.", connectionFailure);
						}
						int num = await _bleDevice!.RequestMtuAsync(185);
						string logTag2 = LogTag;
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Connected to Accessory for ");
						defaultInterpolatedStringHandler.AppendFormatted(accessoryDescription);
						defaultInterpolatedStringHandler.AppendLiteral(" with MTU ");
						defaultInterpolatedStringHandler.AppendFormatted(num);
						defaultInterpolatedStringHandler.AppendLiteral(" SUCCESS");
						TaggedLog.Information(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
						connectionFailure = null;
					}
					catch (Exception ex)
					{
						connectionFailure = ex;
						_bleDevice?.TryDispose();
						_bleDevice = null;
						await TaskExtension.TryDelay(_bleConnectionRetryDelayMs, cancellationToken);
						continue;
					}
					break;
				}
				if (connectionFailure != null || _bleDevice == null)
				{
					string logTag3 = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to connect to Accessory ");
					defaultInterpolatedStringHandler.AppendFormatted(accessoryDescription);
					defaultInterpolatedStringHandler.AppendLiteral(": ");
					defaultInterpolatedStringHandler.AppendFormatted(connectionFailure);
					TaggedLog.Error(logTag3, defaultInterpolatedStringHandler.ToStringAndClear());
					_bleDevice?.TryDispose();
					_bleDevice = null;
					throw new BleManagerConnectionException("Could not connect to Accessory.", connectionFailure);
				}
				try
				{
					TaggedLog.Information(LogTag, "Connected to Accessory " + accessoryDescription + " attempting unlock");
					if (_bleService.Manager.UseKeySeed && await _bleService.Manager.PerformKeySeedExchange(_bleDevice, 2645682455u, AccessoryConnectionManager.AccessoryBleOtaServiceDefault, AccessoryConnectionManager.UnlockGetSeedCharacteristic, AccessoryConnectionManager.UnlockWriteKeyCharacteristic, cancellationToken) != BleDeviceKeySeedExchangeResult.Succeeded)
					{
						throw new BleManagerConnectionKeySeedException("Failed KeySeed exchange.");
					}
				}
				catch (Exception ex2)
				{
					TaggedLog.Error(LogTag, "Connected to Accessory " + accessoryDescription + " KeySeed exchange failed: " + ex2.Message);
					_bleDevice.TryDispose();
					_bleDevice = null;
					throw;
				}
				TaggedLog.Information(LogTag, "Connected to Accessory " + accessoryDescription + " SUCCESS");
				_bleConnectionWatchdog?.TryPet(autoReset: true);
				return _bleDevice;
			}
		}

		public async Task DeviceBlockWriteAsync(Plugin.BLE.Abstractions.Contracts.IDevice deviceToReflash, ILogicalDevice logicalDevice, IReadOnlyList<byte> data, Func<ILogicalDeviceTransferProgress, bool> progressAck, CancellationToken cancellationToken)
		{
			if (deviceToReflash.State != DeviceState.Connected)
			{
				throw new AccessoryConnectionManagerAccessoryOfflineException(logicalDevice);
			}
			cancellationToken.ThrowIfCancellationRequested();
			Stopwatch timer = Stopwatch.StartNew();
			int totalRetryAmount = 0;
			byte[] sendDataChunk = new byte[181];
			BlockWriteTimeTracker timeTracker = new BlockWriteTimeTracker();
			int bytesSent = 0;
			ushort sequence = 0;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
			while (bytesSent < data.Count && !cancellationToken.IsCancellationRequested)
			{
				if ((int)sequence % 100 == 0)
				{
					string logTag = LogTag;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(16, 3);
					defaultInterpolatedStringHandler.AppendLiteral("LoCAP OTA ");
					defaultInterpolatedStringHandler.AppendFormatted("DeviceBlockWriteAsync");
					defaultInterpolatedStringHandler.AppendLiteral(" Stats");
					defaultInterpolatedStringHandler.AppendFormatted(Environment.NewLine);
					defaultInterpolatedStringHandler.AppendFormatted(timeTracker);
					TaggedLog.Information(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				}
				timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.ProgressAck);
				if (!progressAck(new LogicalDeviceTransferProgress((UInt48)bytesSent, (UInt48)totalRetryAmount, (byte)0, TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds))))
				{
					throw new OperationCanceledException();
				}
				timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.BufferCopy);
				int num = ((data.Count - (bytesSent + 2) < 181) ? (data.Count - (bytesSent + 2)) : 181);
				sendDataChunk.Clear();
				data.ToExistingArray(bytesSent, sequence.GetBytes(), 0, 2);
				data.ToExistingArray(bytesSent + 2, sendDataChunk, 0, num - 2);
				timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.UpdateAndSendCommand);
				if (!(await _bleService.Manager.WriteCharacteristicWithResponseAsync(deviceToReflash, AccessoryConnectionManager.AccessoryBleOtaServiceDefault, AccessoryConnectionManager.WriteBulkTransferDataCharacteristic, sendDataChunk, cancellationToken)))
				{
					string logTag2 = LogTag;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(64, 1);
					defaultInterpolatedStringHandler.AppendLiteral("DeviceBlockWrite failed characteristic write, totalRetryAmount: ");
					defaultInterpolatedStringHandler.AppendFormatted(totalRetryAmount);
					TaggedLog.Error(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
					totalRetryAmount++;
					await TaskExtension.TryDelay(200, cancellationToken);
				}
				else
				{
					sequence = (ushort)(sequence + 1);
					bytesSent += 181;
				}
			}
			cancellationToken.ThrowIfCancellationRequested();
			timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.CrcCheck);
			uint value = Crc32Le.Calculate(sendDataChunk, sendDataChunk.Length);
			byte[] array = new byte[8];
			array.SetValueUInt32(value, 0);
			array.SetValueUInt32(10000u, 4);
			await _bleService.Manager.WriteCharacteristicAsync(deviceToReflash, AccessoryConnectionManager.AccessoryBleOtaServiceDefault, AccessoryConnectionManager.EndTransferCrcCheckCharacteristic, array, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.WaitingForSafeToReboot);
			bool safeToReboot = false;
			for (int attempts = 0; attempts < 5; attempts++)
			{
				try
				{
					byte[] array2 = await _bleService.Manager.ReadCharacteristicAsync(deviceToReflash, AccessoryConnectionManager.AccessoryBleOtaServiceDefault, AccessoryConnectionManager.BlockPropertiesCharacteristic, cancellationToken);
					if (array2 == null)
					{
						throw new AccessoryConnectionManagerReadFailedException(logicalDevice);
					}
					if (array2.Length != 13)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Block properties length: ");
						defaultInterpolatedStringHandler.AppendFormatted(array2.Length);
						defaultInterpolatedStringHandler.AppendLiteral(" Expected: ");
						defaultInterpolatedStringHandler.AppendFormatted(13);
						throw new AccessoryConnectionManagerReadDataException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					BlockTransferPropertyFlags blockTransferPropertyFlags = (BlockTransferPropertyFlags)array2[0];
					if (blockTransferPropertyFlags.HasFlag(BlockTransferPropertyFlags.SafeToReboot))
					{
						safeToReboot = true;
						break;
					}
					await TaskExtension.TryDelay(2000, cancellationToken);
					continue;
				}
				catch (Exception ex)
				{
					string logTag3 = LogTag;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Exception reading block properties checking for SafeToReboot flag: ");
					defaultInterpolatedStringHandler.AppendFormatted(ex);
					TaggedLog.Error(logTag3, defaultInterpolatedStringHandler.ToStringAndClear());
					continue;
				}
			}
			if (!safeToReboot)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Accessory not safe to reboot after: ");
				defaultInterpolatedStringHandler.AppendFormatted(10000);
				throw new AccessoryConnectionManagerUnsafeToRebootException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
			timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.Finish);
			if (!(await _bleService.Manager.WriteCharacteristicAsync(deviceToReflash, AccessoryConnectionManager.AccessoryBleOtaServiceDefault, AccessoryConnectionManager.OtaRebootCharacteristic, new byte[1], cancellationToken)))
			{
				throw new AccessoryConnectionManagerWriteFailedException(logicalDevice);
			}
			timeTracker.Stop();
			string logTag4 = LogTag;
			defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(6, 3);
			defaultInterpolatedStringHandler.AppendFormatted("DeviceBlockWriteAsync");
			defaultInterpolatedStringHandler.AppendLiteral(" Stats");
			defaultInterpolatedStringHandler.AppendFormatted(Environment.NewLine);
			defaultInterpolatedStringHandler.AppendFormatted(timeTracker);
			TaggedLog.Information(logTag4, defaultInterpolatedStringHandler.ToStringAndClear());
			timeTracker.SwitchTrackingTo(BlockWriteTimeTracker.TrackId.ProgressAck);
			if (!progressAck(new LogicalDeviceTransferProgress((UInt48)bytesSent, (UInt48)totalRetryAmount, (byte)0, TimeSpan.FromMilliseconds(timer.ElapsedMilliseconds))))
			{
				throw new OperationCanceledException();
			}
		}

		public async Task<IReadOnlyList<byte>> GetAccessoryHistoryDataAsync(byte block, byte startIndex = 0, byte dataLength = byte.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
		{
			byte[] historyDataRequest = new byte[3] { block, startIndex, dataLength };
			try
			{
				Plugin.BLE.Abstractions.Contracts.IDevice obj = await GetSharedConnectionAsync(cancellationToken);
				if (obj == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to get Shared Connection for ");
					defaultInterpolatedStringHandler.AppendFormatted(_logicalDevice);
					throw new LogicalDeviceHistoryDataException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				Plugin.BLE.Abstractions.Contracts.IDevice connectedBleDevice = obj;
				await _bleService.Manager.WriteCharacteristicAsync(connectedBleDevice, AccessoryConnectionManager.AccessoryPrimaryServiceDefault, AccessoryConnectionManager.WriteHistoryDataCharacteristic, historyDataRequest, cancellationToken);
				await TaskExtension.TryDelay(100, cancellationToken);
				byte[] array = await _bleService.Manager.ReadCharacteristicAsync(connectedBleDevice, AccessoryConnectionManager.AccessoryPrimaryServiceDefault, AccessoryConnectionManager.ReadHistoryDataCharacteristic, cancellationToken);
				if (array == null || array.Length < 3)
				{
					object device = _logicalDevice;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to read history data ");
					defaultInterpolatedStringHandler.AppendFormatted(_logicalDevice);
					throw new LogicalDeviceHistoryDataException((ILogicalDevice?)device, defaultInterpolatedStringHandler.ToStringAndClear());
				}
				if ((array[0] != historyDataRequest[0] || array[1] != historyDataRequest[1]) && array[2] != 0)
				{
					string logTag = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(59, 2);
					defaultInterpolatedStringHandler.AppendLiteral("History data block requested ");
					defaultInterpolatedStringHandler.AppendFormatted(historyDataRequest[0]);
					defaultInterpolatedStringHandler.AppendLiteral(" doesn't match received block ");
					defaultInterpolatedStringHandler.AppendFormatted(array[0]);
					TaggedLog.Warning(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
					object device2 = _logicalDevice;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(56, 1);
					defaultInterpolatedStringHandler.AppendLiteral("History data payload did not match the input parameters ");
					defaultInterpolatedStringHandler.AppendFormatted(_logicalDevice);
					throw new LogicalDeviceHistoryDataException((ILogicalDevice?)device2, defaultInterpolatedStringHandler.ToStringAndClear());
				}
				return new ArraySegment<byte>(array, 3, array.Length - 3);
			}
			catch (Exception innerException)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Error retrieving accessory history data ");
				defaultInterpolatedStringHandler.AppendFormatted(_logicalDevice);
				throw new LogicalDeviceHistoryDataException(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
			}
		}

		public Task<UInt48> PidReadAsync(Pid pid, Action<float, string> readProgress, CancellationToken cancellationToken)
		{
			TLogicalDevice logicalDevice;
			try
			{
				switch (pid)
				{
				case Pid.IdsCanFunctionName:
					return Task.FromResult((UInt48)(ushort)AccessoryFunctionName);
				case Pid.IdsCanFunctionInstance:
					return Task.FromResult((UInt48)AccessoryFunctionInstance);
				case Pid.IdsCanCircuitId:
					logicalDevice = _logicalDevice;
					return Task.FromResult((UInt48)(uint)logicalDevice.CircuitId.Value);
				}
			}
			catch (Exception ex)
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Error PID ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				defaultInterpolatedStringHandler.AppendLiteral(" error ");
				defaultInterpolatedStringHandler.AppendFormatted(_logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(" over BLE: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Error(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				throw new LogicalDevicePidValueReadException(pid.ConvertToPid(), _logicalDevice, ex);
			}
			if (!pid.IsAutoCacheingPid())
			{
				string logTag2 = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Error PID ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				defaultInterpolatedStringHandler.AppendLiteral(" not supported by ");
				defaultInterpolatedStringHandler.AppendFormatted(_logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(" over BLE.");
				TaggedLog.Error(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
				throw new LogicalDevicePidValueReadException(pid.ConvertToPid(), _logicalDevice);
			}
			readProgress(0f, "Finding Accessory");
			readProgress(60f, "Reading Value.");
			logicalDevice = _logicalDevice;
			UInt48? obj = ((logicalDevice != null) ? logicalDevice.GetCachedPidRawValue(pid) : null);
			readProgress(100f, "Complete.");
			UInt48? uInt = obj;
			if (!uInt.HasValue)
			{
				PID pid2 = pid.ConvertToPid();
				object device = _logicalDevice;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(45, 1);
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				defaultInterpolatedStringHandler.AppendLiteral(" data not yet received and hasn't been cached");
				throw new LogicalDevicePidValueReadException(pid2, (ILogicalDevice?)device, defaultInterpolatedStringHandler.ToStringAndClear());
			}
			return Task.FromResult(uInt.GetValueOrDefault());
		}

		public Task<uint> PidReadAsync(Pid pid, ushort address, Action<float, string> readProgress, CancellationToken cancellationToken)
		{
			throw new LogicalDevicePidValueReadNotSupportedException(pid.ConvertToPid(), address, _logicalDevice);
		}

		public async Task PidWriteAsync(Pid pid, UInt48 pidValue, LogicalDeviceSessionType pidWriteAccess, Action<float, string> writeProgress, CancellationToken cancellationToken)
		{
			if (writeProgress == null)
			{
				writeProgress = WriteProgress;
			}
			writeProgress(0f, "Finding Accessory Monitor");
			byte[] pidData = new byte[8];
			pidData.SetValueUInt16((ushort)pid, 0);
			pidData.SetValueUInt48(pidValue, 2);
			writeProgress(20f, "Connecting to Accessory.");
			try
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Getting Accessory Connection to ");
				defaultInterpolatedStringHandler.AppendFormatted(_logicalDevice);
				TaggedLog.Information(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				Plugin.BLE.Abstractions.Contracts.IDevice connectedBleDevice = (await GetSharedConnectionAsync(cancellationToken)) ?? throw new LogicalDevicePidValueWriteException(pid.ConvertToPid(), _logicalDevice, pidValue);
				string logTag2 = LogTag;
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Write pid ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
				defaultInterpolatedStringHandler.AppendFormatted(pidValue, "X");
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(pidData.DebugDump());
				TaggedLog.Information(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
				bool flag = false;
				for (int writeAttempt2 = 0; writeAttempt2 < 3; writeAttempt2++)
				{
					if (writeAttempt2 != 0)
					{
						await Task.Delay(PidWriteRetryDelayMs, cancellationToken);
					}
					string logTag3 = LogTag;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Write pid ");
					defaultInterpolatedStringHandler.AppendFormatted(pid);
					defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
					defaultInterpolatedStringHandler.AppendFormatted(pidValue, "X");
					defaultInterpolatedStringHandler.AppendLiteral(" Attempt ");
					defaultInterpolatedStringHandler.AppendFormatted(writeAttempt2 + 1);
					TaggedLog.Information(logTag3, defaultInterpolatedStringHandler.ToStringAndClear());
					flag = await _bleService.Manager.WriteCharacteristicAsync(connectedBleDevice, AccessoryConnectionManager.AccessoryPrimaryServiceDefault, AccessoryConnectionManager.PidWriteCharacteristic, pidData, cancellationToken);
					if (flag)
					{
						writeProgress(60f, "Wrote PID value.");
						string logTag4 = LogTag;
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 3);
						defaultInterpolatedStringHandler.AppendLiteral("Write pid ");
						defaultInterpolatedStringHandler.AppendFormatted(pid);
						defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
						defaultInterpolatedStringHandler.AppendFormatted(pidValue, "X");
						defaultInterpolatedStringHandler.AppendLiteral(" Attempt ");
						defaultInterpolatedStringHandler.AppendFormatted(writeAttempt2 + 1);
						defaultInterpolatedStringHandler.AppendLiteral(" SUCCESSFUL");
						TaggedLog.Information(logTag4, defaultInterpolatedStringHandler.ToStringAndClear());
						break;
					}
					if (PidWriteVerifyDelayMs == 0)
					{
						break;
					}
				}
				if (!flag)
				{
					throw new LogicalDevicePidValueWriteException(pid.ConvertToPid(), _logicalDevice, pidValue);
				}
				writeProgress(90f, "Checking for successful write.");
				if (PidWriteVerifyDelayMs == 0)
				{
					if (pid.IsAutoCacheingPid())
					{
						string logTag5 = LogTag;
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Write pid ");
						defaultInterpolatedStringHandler.AppendFormatted(pid);
						defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
						defaultInterpolatedStringHandler.AppendFormatted(pidValue, "X");
						defaultInterpolatedStringHandler.AppendLiteral(" SUCCESSFUL Cache Updated");
						TaggedLog.Information(logTag5, defaultInterpolatedStringHandler.ToStringAndClear());
						TLogicalDevice logicalDevice = _logicalDevice;
						logicalDevice.SetCachedPidRawValue(pid, pidValue);
					}
					string logTag6 = LogTag;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Write pid ");
					defaultInterpolatedStringHandler.AppendFormatted(pid);
					defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
					defaultInterpolatedStringHandler.AppendFormatted(pidValue, "X");
					defaultInterpolatedStringHandler.AppendLiteral(" SUCCESSFUL without verify");
					TaggedLog.Information(logTag6, defaultInterpolatedStringHandler.ToStringAndClear());
				}
				else
				{
					string logTag7 = LogTag;
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Write pid ");
					defaultInterpolatedStringHandler.AppendFormatted(pid);
					defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
					defaultInterpolatedStringHandler.AppendFormatted(pidValue, "X");
					defaultInterpolatedStringHandler.AppendLiteral(" VERIFYING");
					TaggedLog.Information(logTag7, defaultInterpolatedStringHandler.ToStringAndClear());
					bool pidWriteVerifySuccess = false;
					for (int writeAttempt2 = 0; writeAttempt2 < PidWriteVerifyRetryCount; writeAttempt2++)
					{
						if (writeAttempt2 != 0)
						{
							await Task.Delay(PidWriteVerifyDelayMs, cancellationToken);
						}
						if ((ulong)(await PidReadAsync(pid, delegate
						{
						}, cancellationToken)) == (ulong)pidValue)
						{
							writeProgress(100f, "Complete.");
							string logTag8 = LogTag;
							defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 2);
							defaultInterpolatedStringHandler.AppendLiteral("Write pid ");
							defaultInterpolatedStringHandler.AppendFormatted(pid);
							defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
							defaultInterpolatedStringHandler.AppendFormatted(pidValue, "X");
							defaultInterpolatedStringHandler.AppendLiteral(" VERIFYING SUCCESSFUL");
							TaggedLog.Information(logTag8, defaultInterpolatedStringHandler.ToStringAndClear());
							break;
						}
						string logTag9 = LogTag;
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(34, 3);
						defaultInterpolatedStringHandler.AppendLiteral("Write pid ");
						defaultInterpolatedStringHandler.AppendFormatted(pid);
						defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
						defaultInterpolatedStringHandler.AppendFormatted(pidValue, "X");
						defaultInterpolatedStringHandler.AppendLiteral(" VERIFYING attempt ");
						defaultInterpolatedStringHandler.AppendFormatted(writeAttempt2 + 1);
						TaggedLog.Information(logTag9, defaultInterpolatedStringHandler.ToStringAndClear());
					}
					if (!pidWriteVerifySuccess)
					{
						throw new LogicalDevicePidValueWriteVerifyException(pid.ConvertToPid(), _logicalDevice, pidValue);
					}
				}
				writeProgress(100f, "Complete.");
			}
			catch (LogicalDevicePidValueWriteVerifyException ex)
			{
				string logTag10 = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Write pid successful, but unable to verify pid ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
				defaultInterpolatedStringHandler.AppendFormatted(pidValue, "X");
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Warning(logTag10, defaultInterpolatedStringHandler.ToStringAndClear());
				writeProgress(100f, "Complete.");
			}
			catch (LogicalDevicePidValueWriteException ex2)
			{
				string logTag11 = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Write pid ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
				defaultInterpolatedStringHandler.AppendFormatted(pidValue, "X");
				defaultInterpolatedStringHandler.AppendLiteral(" FAILED: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex2.Message);
				TaggedLog.Error(logTag11, defaultInterpolatedStringHandler.ToStringAndClear());
				throw;
			}
			catch (TimeoutException ex3)
			{
				string logTag12 = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Write pid ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
				defaultInterpolatedStringHandler.AppendFormatted(pidValue, "X");
				defaultInterpolatedStringHandler.AppendLiteral(" FAILED: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex3.Message);
				TaggedLog.Error(logTag12, defaultInterpolatedStringHandler.ToStringAndClear());
				throw;
			}
			catch (OperationCanceledException ex4)
			{
				string logTag13 = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Write pid ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
				defaultInterpolatedStringHandler.AppendFormatted(pidValue, "X");
				defaultInterpolatedStringHandler.AppendLiteral(" FAILED: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex4.Message);
				TaggedLog.Error(logTag13, defaultInterpolatedStringHandler.ToStringAndClear());
				throw;
			}
			catch (Exception ex5)
			{
				string logTag14 = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(24, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Write pid ");
				defaultInterpolatedStringHandler.AppendFormatted(pid);
				defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
				defaultInterpolatedStringHandler.AppendFormatted(pidValue, "X");
				defaultInterpolatedStringHandler.AppendLiteral(" FAILED: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex5.Message);
				TaggedLog.Error(logTag14, defaultInterpolatedStringHandler.ToStringAndClear());
				throw new LogicalDevicePidValueWriteException(pid.ConvertToPid(), _logicalDevice, pidValue, ex5);
			}
		}

		private void WriteProgress(float percentComplete, string status)
		{
			string logTag = LogTag;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(23, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Pid write progress: ");
			defaultInterpolatedStringHandler.AppendFormatted(percentComplete);
			defaultInterpolatedStringHandler.AppendLiteral("%: ");
			defaultInterpolatedStringHandler.AppendFormatted(status);
			TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public Task PidWriteAsync(Pid pid, ushort address, uint pidValue, LogicalDeviceSessionType pidWriteAccess, Action<float, string> writeProgress, CancellationToken cancellationToken)
		{
			throw new LogicalDevicePidValueWriteException(pid.ConvertToPid(), _logicalDevice, pidValue);
		}

		public async Task<string> GetSoftwarePartNumberAsync(CancellationToken cancellationToken)
		{
			_ = 1;
			try
			{
				Plugin.BLE.Abstractions.Contracts.IDevice obj = await GetSharedConnectionAsync(cancellationToken);
				if (obj == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to get Shared Connection for ");
					defaultInterpolatedStringHandler.AppendFormatted(_logicalDevice);
					throw new IdsCanAccessoryException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				Plugin.BLE.Abstractions.Contracts.IDevice device = obj;
				byte[] array = await _bleService.Manager.ReadCharacteristicAsync(device, AccessoryConnectionManager.AccessoryPrimaryServiceDefault, AccessoryConnectionManager.ReadSoftwarePartNumberCharacteristic, cancellationToken);
				if (array == null || array.Length != 6)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(65, 3);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to read software part number for ");
					defaultInterpolatedStringHandler.AppendFormatted(_logicalDevice);
					defaultInterpolatedStringHandler.AppendLiteral(" expected ");
					defaultInterpolatedStringHandler.AppendFormatted(6);
					defaultInterpolatedStringHandler.AppendLiteral(" bytes but got ");
					defaultInterpolatedStringHandler.AppendFormatted((array != null) ? array.Length : 0);
					throw new IdsCanAccessoryException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				Array.Reverse(array);
				string text = Encoding.ASCII.GetString(array);
				if (text.Length == 6)
				{
					text = text.Insert(5, "-");
				}
				return text;
			}
			catch (Exception innerException)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Error retrieving software part number ");
				defaultInterpolatedStringHandler.AppendFormatted(_logicalDevice);
				throw new IdsCanAccessoryException(defaultInterpolatedStringHandler.ToStringAndClear(), innerException);
			}
		}

		internal void Update(IdsCanAccessoryStatus accessoryStatus)
		{
			_accessoryFunctionNameCached = accessoryStatus.FunctionName.ToFunctionName();
			_accessoryFunctionInstanceCached = accessoryStatus.FunctionInstance;
		}
	}
}
