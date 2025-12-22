using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using ids.portable.ble.Ble;
using ids.portable.ble.Exceptions;
using ids.portable.ble.ScanResults;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDeviceSource;
using OneControl.Direct.IdsCanAccessoryBle.Connections;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace OneControl.Direct.IdsCanAccessoryBle.AccessoryTemplate.BleDeviceSource
{
	public abstract class BleDeviceSourceLocap<TAccessoryBleDeviceDriver, TSensorConnection, TLogicalDevice> : CommonDisposable, IAccessoryBleDeviceSourceLocap<TSensorConnection>, IAccessoryBleDeviceSourceLocap, ILogicalDeviceSourceDirect, ILogicalDeviceSource, IAccessoryBleDeviceSource<TSensorConnection>, IAccessoryBleDeviceSource, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata, ILogicalDeviceSourceDirectPid, ILogicalDeviceSourceDirectAccessoryHistoryData, IAccessoryBleDeviceSourceDevices<TAccessoryBleDeviceDriver> where TAccessoryBleDeviceDriver : IAccessoryBleDeviceDriverLocap<TSensorConnection, TLogicalDevice> where TSensorConnection : ISensorConnectionBleLocap where TLogicalDevice : class, ILogicalDeviceAccessory
	{
		private readonly IBleService _bleService;

		private readonly int _bleConnectAttemptTimeoutMs;

		private readonly ConcurrentDictionary<Guid, TAccessoryBleDeviceDriver> _registeredBleDeviceDrivers = new ConcurrentDictionary<Guid, TAccessoryBleDeviceDriver>();

		private const int FunctionInstanceMaxValue = 15;

		protected abstract string LogTag { get; }

		public string DeviceSourceToken { get; }

		public virtual bool AllowAutoOfflineLogicalDeviceRemoval => false;

		public bool IsDeviceSourceActive => Enumerable.Any(_registeredBleDeviceDrivers);

		public ILogicalDeviceService DeviceService { get; }

		public IN_MOTION_LOCKOUT_LEVEL InTransitLockoutLevel => (byte)0;

		public IEnumerable<ISensorConnection> SensorConnectionsAll
		{
			get
			{
				foreach (TAccessoryBleDeviceDriver value in _registeredBleDeviceDrivers.Values)
				{
					yield return value.SensorConnection;
				}
			}
		}

		public IEnumerable<TAccessoryBleDeviceDriver> SensorDevices => _registeredBleDeviceDrivers.Values;

		protected BleDeviceSourceLocap(IBleService bleService, ILogicalDeviceService deviceService, string deviceSourceToken, TimeSpan bleConnectAttemptTimeout)
		{
			_bleService = bleService;
			DeviceService = deviceService;
			DeviceSourceToken = deviceSourceToken ?? throw new ArgumentNullException("deviceSourceToken");
			_bleConnectAttemptTimeoutMs = (int)bleConnectAttemptTimeout.TotalMilliseconds;
			_bleService.Scanner.DidReceiveScanResult += ReceivedBleScanResult;
		}

		public bool IsLogicalDeviceSupported(ILogicalDevice? logicalDevice)
		{
			ILogicalDevice logicalDevice2 = logicalDevice;
			if (logicalDevice2 == null || logicalDevice2.IsDisposed)
			{
				return false;
			}
			return Enumerable.FirstOrDefault(_registeredBleDeviceDrivers.Values, (TAccessoryBleDeviceDriver sd) => logicalDevice2.LogicalId.ProductMacAddress?.Equals(sd.AccessoryMacAddress) ?? false) != null;
		}

		public bool IsLogicalDeviceOnline(ILogicalDevice? logicalDevice)
		{
			ILogicalDevice logicalDevice2 = logicalDevice;
			if (logicalDevice2 == null || logicalDevice2.IsDisposed)
			{
				return false;
			}
			return Enumerable.FirstOrDefault(_registeredBleDeviceDrivers.Values, (TAccessoryBleDeviceDriver sd) => logicalDevice2.LogicalId.ProductMacAddress?.Equals(sd.AccessoryMacAddress) ?? false)?.IsOnline ?? false;
		}

		private void ReceivedBleScanResult(IBleScanResult scanResult)
		{
			if (!_registeredBleDeviceDrivers.TryGetValue(scanResult.DeviceId, out var val) || !(scanResult is IdsCanAccessoryScanResult accessoryScanResult))
			{
				return;
			}
			try
			{
				val.Update(accessoryScanResult);
			}
			catch (Exception ex)
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to update sensor ");
				defaultInterpolatedStringHandler.AppendFormatted(scanResult);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Warning(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		public override void Dispose(bool disposing)
		{
			try
			{
				_bleService.Scanner.DidReceiveScanResult -= ReceivedBleScanResult;
			}
			catch
			{
			}
			foreach (KeyValuePair<Guid, TAccessoryBleDeviceDriver> registeredBleDeviceDriver in _registeredBleDeviceDrivers)
			{
				registeredBleDeviceDriver.Value.TryDispose();
			}
			_registeredBleDeviceDrivers.Clear();
		}

		public virtual Task<IReadOnlyList<byte>> GetAccessoryHistoryDataAsync(ILogicalDevice logicalDevice, byte block, byte startIndex = 0, byte dataLength = byte.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(47, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Getting of history data for ");
			defaultInterpolatedStringHandler.AppendFormatted(typeof(TAccessoryBleDeviceDriver));
			defaultInterpolatedStringHandler.AppendLiteral(" not supported see ");
			defaultInterpolatedStringHandler.AppendFormatted(GetType());
			throw new LogicalDeviceHistoryDataException(defaultInterpolatedStringHandler.ToStringAndClear());
		}

		public IN_MOTION_LOCKOUT_LEVEL GetLogicalDeviceInTransitLockoutLevel(ILogicalDevice? logicalDevice)
		{
			return (byte)0;
		}

		public bool IsLogicalDeviceHazardous(ILogicalDevice? logicalDevice)
		{
			return false;
		}

		public async Task<string> GetSoftwarePartNumberAsync(ILogicalDevice logicalDevice, CancellationToken cancelToken)
		{
			TAccessoryBleDeviceDriver sensorDevice = GetSensorDevice(logicalDevice);
			string text = sensorDevice?.SoftwarePartNumber ?? logicalDevice.CustomSnapshotData.SoftwarePartNumber ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
			try
			{
				AccessoryConnectionManager<TLogicalDevice> accessoryConnectionManager = sensorDevice?.AccessoryConnectionManager;
				if (accessoryConnectionManager == null)
				{
					throw new IdsCanAccessoryException("Can't find accessory or it's connection manager");
				}
				using CancellationTokenSource timeoutCts = new CancellationTokenSource(_bleConnectAttemptTimeoutMs);
				using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancelToken);
				return await accessoryConnectionManager.GetSoftwarePartNumberAsync(linkedCts.Token);
			}
			catch (Exception ex)
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to get software part number for ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Warning(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				return string.Empty;
			}
		}

		public Version? GetDeviceProtocolVersion(ILogicalDevice logicalDevice)
		{
			return null;
		}

		public Task<UInt48> PidReadAsync(ILogicalDevice logicalDevice, Pid pid, Action<float, string> readProgress, CancellationToken cancellationToken)
		{
			TAccessoryBleDeviceDriver sensorDevice = GetSensorDevice(logicalDevice);
			if (sensorDevice == null)
			{
				throw new LogicalDevicePidValueReadException(pid.ConvertToPid(), logicalDevice);
			}
			TAccessoryBleDeviceDriver val = sensorDevice;
			return (val.AccessoryConnectionManager ?? throw new LogicalDevicePidValueReadException(pid.ConvertToPid(), logicalDevice))!.PidReadAsync(pid, readProgress, cancellationToken);
		}

		public Task<uint> PidReadAsync(ILogicalDevice logicalDevice, Pid pid, ushort address, Action<float, string> readProgress, CancellationToken cancellationToken)
		{
			TAccessoryBleDeviceDriver sensorDevice = GetSensorDevice(logicalDevice);
			if (sensorDevice == null)
			{
				throw new LogicalDevicePidValueReadException(pid.ConvertToPid(), logicalDevice);
			}
			TAccessoryBleDeviceDriver val = sensorDevice;
			return (val.AccessoryConnectionManager ?? throw new LogicalDevicePidValueReadException(pid.ConvertToPid(), logicalDevice))!.PidReadAsync(pid, address, readProgress, cancellationToken);
		}

		public Task PidWriteAsync(ILogicalDevice logicalDevice, Pid pid, UInt48 pidValue, LogicalDeviceSessionType pidWriteAccess, Action<float, string> writeProgress, CancellationToken cancellationToken)
		{
			TAccessoryBleDeviceDriver sensorDevice = GetSensorDevice(logicalDevice);
			if (sensorDevice == null)
			{
				throw new LogicalDevicePidValueWriteException(pid.ConvertToPid(), logicalDevice, pidValue);
			}
			TAccessoryBleDeviceDriver val = sensorDevice;
			return (val.AccessoryConnectionManager ?? throw new LogicalDevicePidValueWriteException(pid.ConvertToPid(), logicalDevice, pidValue))!.PidWriteAsync(pid, pidValue, pidWriteAccess, writeProgress, cancellationToken);
		}

		public Task PidWriteAsync(ILogicalDevice logicalDevice, Pid pid, ushort address, uint pidValue, LogicalDeviceSessionType pidWriteAccess, Action<float, string> writeProgress, CancellationToken cancellationToken)
		{
			TAccessoryBleDeviceDriver sensorDevice = GetSensorDevice(logicalDevice);
			if (sensorDevice == null)
			{
				throw new LogicalDevicePidValueWriteException(pid.ConvertToPid(), logicalDevice, pidValue);
			}
			TAccessoryBleDeviceDriver val = sensorDevice;
			return (val.AccessoryConnectionManager ?? throw new LogicalDevicePidValueWriteException(pid.ConvertToPid(), logicalDevice, pidValue))!.PidWriteAsync(pid, address, pidValue, pidWriteAccess, writeProgress, cancellationToken);
		}

		public TAccessoryBleDeviceDriver? GetSensorDevice(ILogicalDevice? logicalDevice)
		{
			ILogicalDevice logicalDevice2 = logicalDevice;
			return Enumerable.FirstOrDefault(_registeredBleDeviceDrivers.Values, (TAccessoryBleDeviceDriver ts) => ts.AccessoryMacAddress == logicalDevice2?.LogicalId.ProductMacAddress);
		}

		protected abstract TAccessoryBleDeviceDriver CreateDeviceBle(TSensorConnection sensorConnection);

		public bool RegisterSensor(TSensorConnection sensorConnection)
		{
			Guid? guid = sensorConnection?.ConnectionGuid;
			if (guid.HasValue)
			{
				Guid valueOrDefault = guid.GetValueOrDefault();
				try
				{
					if (IsSensorRegistered(valueOrDefault))
					{
						return false;
					}
					TAccessoryBleDeviceDriver val = CreateDeviceBle(sensorConnection);
					val.LogicalDevice?.AddDeviceSource(this);
					bool num = _registeredBleDeviceDrivers.TryAdd(valueOrDefault, val);
					if (num)
					{
						string logTag = LogTag;
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 3);
						defaultInterpolatedStringHandler.AppendLiteral("Register ");
						defaultInterpolatedStringHandler.AppendFormatted(typeof(TAccessoryBleDeviceDriver));
						defaultInterpolatedStringHandler.AppendLiteral(" Sensor ");
						defaultInterpolatedStringHandler.AppendFormatted(valueOrDefault);
						defaultInterpolatedStringHandler.AppendLiteral("/");
						defaultInterpolatedStringHandler.AppendFormatted(sensorConnection.AccessoryMac);
						TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
					}
					return num;
				}
				catch (BleScannerServiceAlreadyRegisteredException)
				{
					string logTag2 = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 2);
					defaultInterpolatedStringHandler.AppendFormatted(typeof(TAccessoryBleDeviceDriver));
					defaultInterpolatedStringHandler.AppendLiteral(" Sensor already registered for ");
					defaultInterpolatedStringHandler.AppendFormatted(valueOrDefault);
					TaggedLog.Debug(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
					return false;
				}
				catch (Exception ex2)
				{
					string logTag3 = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 3);
					defaultInterpolatedStringHandler.AppendFormatted(typeof(TAccessoryBleDeviceDriver));
					defaultInterpolatedStringHandler.AppendLiteral(" Sensor error registering ");
					defaultInterpolatedStringHandler.AppendFormatted(valueOrDefault);
					defaultInterpolatedStringHandler.AppendLiteral(": ");
					defaultInterpolatedStringHandler.AppendFormatted(ex2.Message);
					TaggedLog.Error(logTag3, defaultInterpolatedStringHandler.ToStringAndClear());
					return false;
				}
			}
			return false;
		}

		public abstract bool RegisterSensor(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName);

		public void UnRegisterSensor(Guid bleDeviceId)
		{
			if (_registeredBleDeviceDrivers.TryGetValue(bleDeviceId, out var val))
			{
				val.LogicalDevice?.RemoveDeviceSource(this);
				if (_registeredBleDeviceDrivers.TryRemove(bleDeviceId, out var _))
				{
					val.TryDispose();
				}
			}
		}

		public bool IsSensorRegistered(Guid bleDeviceId)
		{
			return _registeredBleDeviceDrivers.ContainsKey(bleDeviceId);
		}

		public bool IsLogicalDeviceRenameSupported(ILogicalDevice? logicalDevice)
		{
			return IsLogicalDeviceSupported(logicalDevice);
		}

		public async Task RenameLogicalDevice(ILogicalDevice? logicalDevice, FUNCTION_NAME toName, byte toFunctionInstance, CancellationToken cancellationToken)
		{
			string logTag = LogTag;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(62, 4);
			defaultInterpolatedStringHandler.AppendFormatted(typeof(TAccessoryBleDeviceDriver));
			defaultInterpolatedStringHandler.AppendLiteral(" Sensor Rename: LogicalDevice: ");
			defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
			defaultInterpolatedStringHandler.AppendLiteral("  toName: ");
			defaultInterpolatedStringHandler.AppendFormatted(toName);
			defaultInterpolatedStringHandler.AppendLiteral(" toFunctionInstance: ");
			defaultInterpolatedStringHandler.AppendFormatted(toFunctionInstance);
			TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
			if (logicalDevice == null)
			{
				throw new LogicalDeviceSourceDirectRenameException("Logical device is null.");
			}
			if (toFunctionInstance > 15)
			{
				throw new LogicalDeviceSourceDirectRenameException("Function instance outside of acceptable range.");
			}
			if (logicalDevice!.LogicalId.FunctionName != toName)
			{
				await PidWriteAsync(logicalDevice, Pid.IdsCanFunctionName, (ushort)toName, LogicalDeviceSessionType.Diagnostic, DefaultWriteProgress, cancellationToken);
			}
			if (logicalDevice!.LogicalId.FunctionInstance != toFunctionInstance)
			{
				await PidWriteAsync(logicalDevice, Pid.IdsCanFunctionInstance, (ushort)toFunctionInstance, LogicalDeviceSessionType.Diagnostic, DefaultWriteProgress, cancellationToken);
			}
			if (logicalDevice!.DeviceService.GetPrimaryDeviceSourceDirect(logicalDevice) == this)
			{
				logicalDevice!.Rename(toName, toFunctionInstance);
			}
		}

		protected void DefaultWriteProgress(float percentComplete, string status)
		{
			string logTag = LogTag;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(23, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Pid write progress: ");
			defaultInterpolatedStringHandler.AppendFormatted(percentComplete);
			defaultInterpolatedStringHandler.AppendLiteral("%: ");
			defaultInterpolatedStringHandler.AppendFormatted(status);
			TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
		}

		protected abstract ILogicalDeviceTag CreateLogicalDeviceTag(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName);

		public IEnumerable<ILogicalDeviceTag> MakeDeviceSourceTags(ILogicalDevice? logicalDevice)
		{
			ILogicalDevice logicalDevice2 = logicalDevice;
			if (logicalDevice2 == null || logicalDevice2.IsDisposed)
			{
				return new ILogicalDeviceTag[0];
			}
			TAccessoryBleDeviceDriver val = Enumerable.FirstOrDefault(_registeredBleDeviceDrivers.Values, (TAccessoryBleDeviceDriver sd) => logicalDevice2.LogicalId.ProductMacAddress?.Equals(sd.AccessoryMacAddress) ?? false);
			if (val == null)
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(96, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to make Device Source Tags ");
				defaultInterpolatedStringHandler.AppendFormatted(LogTag);
				defaultInterpolatedStringHandler.AppendLiteral(" because ");
				defaultInterpolatedStringHandler.AppendFormatted(typeof(TAccessoryBleDeviceDriver));
				defaultInterpolatedStringHandler.AppendLiteral(" Sensor isn't registered with this Source Direct for ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice2);
				TaggedLog.Warning(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				return new ILogicalDeviceTag[0];
			}
			ILogicalDeviceTag logicalDeviceTag = CreateLogicalDeviceTag(val.BleDeviceId, val.AccessoryMacAddress, val.SoftwarePartNumber, val.BleDeviceName);
			return new ILogicalDeviceTag[1] { logicalDeviceTag };
		}
	}
}
