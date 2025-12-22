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
using IDS.Portable.LogicalDevice.LogicalDeviceSource;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace OneControl.Direct.IdsCanAccessoryBle.GenericSensor
{
	public class DirectGenericSensorBle : CommonDisposable, IDirectGenericSensorBle, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirect, ILogicalDeviceSource, ILogicalDeviceSourceDirectPid, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectAccessoryHistoryData, ILogicalDeviceSourceDirectMetadata
	{
		private readonly IBleService _bleService;

		private const string LogTag = "DirectGenericSensorBle";

		private readonly ConcurrentDictionary<Guid, GenericSensorBle> _registeredGenericSensors = new ConcurrentDictionary<Guid, GenericSensorBle>();

		private const string DeviceSourceTokenDefault = "Ids.Accessory.GenericSensor.Default";

		public string DeviceSourceToken { get; }

		public bool AllowAutoOfflineLogicalDeviceRemoval => false;

		public bool IsDeviceSourceActive => Enumerable.Any(_registeredGenericSensors);

		public ILogicalDeviceService DeviceService { get; }

		protected virtual int TimeToWaitForRenameToCompleteMs { get; } = 10000;


		public IEnumerable<GenericSensorBle> GenericSensors => _registeredGenericSensors.Values;

		public IN_MOTION_LOCKOUT_LEVEL InTransitLockoutLevel => (byte)0;

		public DirectGenericSensorBle(IBleService bleService, ILogicalDeviceService deviceService, string deviceSourceToken = "Ids.Accessory.GenericSensor.Default")
		{
			_bleService = bleService;
			DeviceService = deviceService;
			DeviceSourceToken = deviceSourceToken ?? "Ids.Accessory.GenericSensor.Default";
			_bleService.Scanner.DidReceiveScanResult += ReceivedBleScanResult;
		}

		public bool RegisterGenericSensor(Guid bleDeviceId, MAC accessoryMacAddress, string softwarePartNumber, string bleDeviceName)
		{
			try
			{
				if (IsGenericSensorRegistered(bleDeviceId))
				{
					return false;
				}
				GenericSensorBle genericSensorBle = new GenericSensorBle(_bleService, this, bleDeviceId, accessoryMacAddress, softwarePartNumber, bleDeviceName);
				genericSensorBle.LogicalDevice?.AddDeviceSource(this);
				bool num = _registeredGenericSensors.TryAdd(bleDeviceId, genericSensorBle);
				if (num)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Register Generic Sensor ");
					defaultInterpolatedStringHandler.AppendFormatted(bleDeviceId);
					defaultInterpolatedStringHandler.AppendLiteral("/");
					defaultInterpolatedStringHandler.AppendFormatted(accessoryMacAddress);
					TaggedLog.Debug("DirectGenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				return num;
			}
			catch (BleScannerServiceAlreadyRegisteredException)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(38, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Generic Sensor already registered for ");
				defaultInterpolatedStringHandler.AppendFormatted(bleDeviceId);
				TaggedLog.Debug("DirectGenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
				return false;
			}
			catch (Exception ex2)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(35, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Generic Sensor error registering ");
				defaultInterpolatedStringHandler.AppendFormatted(bleDeviceId);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(ex2.Message);
				TaggedLog.Error("DirectGenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
				return false;
			}
		}

		public void UnRegisterGenericSensor(Guid bleDeviceId)
		{
			if (_registeredGenericSensors.TryGetValue(bleDeviceId, out var genericSensorBle))
			{
				genericSensorBle.LogicalDevice?.RemoveDeviceSource(this);
				if (_registeredGenericSensors.TryRemove(bleDeviceId, out var _))
				{
					genericSensorBle.TryDispose();
				}
			}
		}

		public bool IsGenericSensorRegistered(Guid bleDeviceId)
		{
			return _registeredGenericSensors.ContainsKey(bleDeviceId);
		}

		public bool IsLogicalDeviceSupported(ILogicalDevice? logicalDevice)
		{
			ILogicalDevice logicalDevice2 = logicalDevice;
			if (logicalDevice2 == null || logicalDevice2.IsDisposed)
			{
				return false;
			}
			return Enumerable.FirstOrDefault(_registeredGenericSensors.Values, (GenericSensorBle sd) => logicalDevice2.LogicalId.ProductMacAddress?.Equals(sd.AccessoryMacAddress) ?? false) != null;
		}

		public bool IsLogicalDeviceOnline(ILogicalDevice? logicalDevice)
		{
			ILogicalDevice logicalDevice2 = logicalDevice;
			if (logicalDevice2 == null || logicalDevice2.IsDisposed)
			{
				return false;
			}
			return Enumerable.FirstOrDefault(_registeredGenericSensors.Values, (GenericSensorBle sd) => logicalDevice2.LogicalId.ProductMacAddress?.Equals(sd.AccessoryMacAddress) ?? false)?.IsOnline ?? false;
		}

		public GenericSensorBle? GetGenericSensor(ILogicalDevice? logicalDevice)
		{
			ILogicalDevice logicalDevice2 = logicalDevice;
			return Enumerable.FirstOrDefault(_registeredGenericSensors.Values, (GenericSensorBle ts) => ts.AccessoryMacAddress == logicalDevice2?.LogicalId.ProductMacAddress);
		}

		public IEnumerable<ILogicalDeviceTag> MakeDeviceSourceTags(ILogicalDevice? logicalDevice)
		{
			TaggedLog.Warning("DirectGenericSensorBle", "Unable to make Device Source Tags DirectGenericSensorBle because Generic sensor does not support source tags");
			return new ILogicalDeviceTag[0];
		}

		public IN_MOTION_LOCKOUT_LEVEL GetLogicalDeviceInTransitLockoutLevel(ILogicalDevice? logicalDevice)
		{
			return (byte)0;
		}

		public bool IsLogicalDeviceHazardous(ILogicalDevice? logicalDevice)
		{
			return false;
		}

		private void ReceivedBleScanResult(IBleScanResult scanResult)
		{
			if (!_registeredGenericSensors.TryGetValue(scanResult.DeviceId, out var genericSensorBle) || !(scanResult is IdsCanAccessoryScanResult accessoryScanResult))
			{
				return;
			}
			try
			{
				genericSensorBle.Update(accessoryScanResult);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(33, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to update sensor device ");
				defaultInterpolatedStringHandler.AppendFormatted(scanResult);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Warning("DirectGenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		public Task<UInt48> PidReadAsync(ILogicalDevice logicalDevice, Pid pid, Action<float, string> readProgress, CancellationToken cancellationToken)
		{
			return ((GetGenericSensor(logicalDevice) ?? throw new LogicalDevicePidValueReadException(pid.ConvertToPid(), logicalDevice))!.AccessoryConnectionManager ?? throw new LogicalDevicePidValueReadException(pid.ConvertToPid(), logicalDevice))!.PidReadAsync(pid, readProgress, cancellationToken);
		}

		public Task<uint> PidReadAsync(ILogicalDevice logicalDevice, Pid pid, ushort address, Action<float, string> readProgress, CancellationToken cancellationToken)
		{
			return ((GetGenericSensor(logicalDevice) ?? throw new LogicalDevicePidValueReadException(pid.ConvertToPid(), logicalDevice))!.AccessoryConnectionManager ?? throw new LogicalDevicePidValueReadException(pid.ConvertToPid(), logicalDevice))!.PidReadAsync(pid, address, readProgress, cancellationToken);
		}

		public Task PidWriteAsync(ILogicalDevice logicalDevice, Pid pid, UInt48 pidValue, LogicalDeviceSessionType pidWriteAccess, Action<float, string> writeProgress, CancellationToken cancellationToken)
		{
			return ((GetGenericSensor(logicalDevice) ?? throw new LogicalDevicePidValueWriteException(pid.ConvertToPid(), logicalDevice, pidValue))!.AccessoryConnectionManager ?? throw new LogicalDevicePidValueWriteException(pid.ConvertToPid(), logicalDevice, pidValue))!.PidWriteAsync(pid, pidValue, pidWriteAccess, writeProgress, cancellationToken);
		}

		public Task PidWriteAsync(ILogicalDevice logicalDevice, Pid pid, ushort address, uint pidValue, LogicalDeviceSessionType pidWriteAccess, Action<float, string> writeProgress, CancellationToken cancellationToken)
		{
			return ((GetGenericSensor(logicalDevice) ?? throw new LogicalDevicePidValueWriteException(pid.ConvertToPid(), logicalDevice, pidValue))!.AccessoryConnectionManager ?? throw new LogicalDevicePidValueWriteException(pid.ConvertToPid(), logicalDevice, pidValue))!.PidWriteAsync(pid, address, pidValue, pidWriteAccess, writeProgress, cancellationToken);
		}

		public bool IsLogicalDeviceRenameSupported(ILogicalDevice? logicalDevice)
		{
			return IsLogicalDeviceSupported(logicalDevice);
		}

		public async Task RenameLogicalDevice(ILogicalDevice? logicalDevice, FUNCTION_NAME toName, byte toFunctionInstance, CancellationToken cancellationToken)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(69, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Generic sensor rename: LogicalDevice: ");
			defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
			defaultInterpolatedStringHandler.AppendLiteral("  toName: ");
			defaultInterpolatedStringHandler.AppendFormatted(toName);
			defaultInterpolatedStringHandler.AppendLiteral(" toFunctionInstance: ");
			defaultInterpolatedStringHandler.AppendFormatted(toFunctionInstance);
			TaggedLog.Debug("DirectGenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
			if (logicalDevice == null)
			{
				throw new LogicalDeviceSourceDirectRenameException("Logical device is null.");
			}
			if (toFunctionInstance > 15)
			{
				throw new LogicalDeviceSourceDirectRenameException("Function instance outside of acceptable range.");
			}
			FUNCTION_NAME originalFunctionName = logicalDevice!.LogicalId.FunctionName;
			int originalFunctionInstance = logicalDevice!.LogicalId.FunctionInstance;
			bool flag = false;
			if (originalFunctionName != toName)
			{
				await PidWriteAsync(logicalDevice, Pid.IdsCanFunctionName, (ushort)toName, LogicalDeviceSessionType.Diagnostic, WriteProgress, cancellationToken);
				flag = true;
			}
			if (originalFunctionInstance != toFunctionInstance)
			{
				await PidWriteAsync(logicalDevice, Pid.IdsCanFunctionInstance, (ushort)toFunctionInstance, LogicalDeviceSessionType.Diagnostic, WriteProgress, cancellationToken);
				flag = true;
			}
			if (flag)
			{
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(50, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Waiting for device name to change from `");
				defaultInterpolatedStringHandler.AppendFormatted(originalFunctionName);
				defaultInterpolatedStringHandler.AppendLiteral(":");
				defaultInterpolatedStringHandler.AppendFormatted(originalFunctionInstance);
				defaultInterpolatedStringHandler.AppendLiteral("` to `");
				defaultInterpolatedStringHandler.AppendFormatted(toName);
				defaultInterpolatedStringHandler.AppendLiteral(":");
				defaultInterpolatedStringHandler.AppendFormatted(toFunctionInstance);
				defaultInterpolatedStringHandler.AppendLiteral("` ");
				TaggedLog.Information("DirectGenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
				if (!(await logicalDevice!.TryWaitForRenameAsync(toName, toFunctionInstance, TimeToWaitForRenameToCompleteMs, cancellationToken)))
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(47, 4);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to verify device rename from `");
					defaultInterpolatedStringHandler.AppendFormatted(originalFunctionName);
					defaultInterpolatedStringHandler.AppendLiteral(":");
					defaultInterpolatedStringHandler.AppendFormatted(originalFunctionInstance);
					defaultInterpolatedStringHandler.AppendLiteral("` to `");
					defaultInterpolatedStringHandler.AppendFormatted(toName);
					defaultInterpolatedStringHandler.AppendLiteral(":");
					defaultInterpolatedStringHandler.AppendFormatted(toFunctionInstance);
					defaultInterpolatedStringHandler.AppendLiteral("` ");
					TaggedLog.Warning("DirectGenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
					throw new LogicalDeviceSourceDirectRenameAppliedButNotVerifiedException();
				}
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(36, 4);
				defaultInterpolatedStringHandler.AppendLiteral("Renamed device name from `");
				defaultInterpolatedStringHandler.AppendFormatted(originalFunctionName);
				defaultInterpolatedStringHandler.AppendLiteral(":");
				defaultInterpolatedStringHandler.AppendFormatted(originalFunctionInstance);
				defaultInterpolatedStringHandler.AppendLiteral("` to `");
				defaultInterpolatedStringHandler.AppendFormatted(toName);
				defaultInterpolatedStringHandler.AppendLiteral(":");
				defaultInterpolatedStringHandler.AppendFormatted(toFunctionInstance);
				defaultInterpolatedStringHandler.AppendLiteral("` ");
				TaggedLog.Information("DirectGenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		private void WriteProgress(float percentComplete, string status)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(23, 2);
			defaultInterpolatedStringHandler.AppendLiteral("Pid write progress: ");
			defaultInterpolatedStringHandler.AppendFormatted(percentComplete);
			defaultInterpolatedStringHandler.AppendLiteral("%: ");
			defaultInterpolatedStringHandler.AppendFormatted(status);
			TaggedLog.Debug("DirectGenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
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
			foreach (KeyValuePair<Guid, GenericSensorBle> registeredGenericSensor in _registeredGenericSensors)
			{
				registeredGenericSensor.Value.TryDispose();
			}
			_registeredGenericSensors.Clear();
		}

		public async Task<string> GetSoftwarePartNumberAsync(ILogicalDevice logicalDevice, CancellationToken cancelToken)
		{
			GenericSensorBle genericSensor = GetGenericSensor(logicalDevice);
			string text = genericSensor?.SoftwarePartNumber ?? logicalDevice.CustomSnapshotData.SoftwarePartNumber ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
			try
			{
				AccessoryConnectionManager<ILogicalDevice> accessoryConnectionManager = genericSensor?.AccessoryConnectionManager;
				if (accessoryConnectionManager == null)
				{
					throw new IdsCanAccessoryException("Can't find accessory or it's connection manager");
				}
				using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
				linkedCts.CancelAfter(80000);
				return await accessoryConnectionManager.GetSoftwarePartNumberAsync(linkedCts.Token);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to get software part number for ");
				defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Warning("DirectGenericSensorBle", defaultInterpolatedStringHandler.ToStringAndClear());
				return string.Empty;
			}
		}

		public Version? GetDeviceProtocolVersion(ILogicalDevice logicalDevice)
		{
			return null;
		}

		public Task<IReadOnlyList<byte>> GetAccessoryHistoryDataAsync(ILogicalDevice logicalDevice, byte block, byte startIndex = 0, byte dataLength = byte.MaxValue, CancellationToken cancellationToken = default(CancellationToken))
		{
			return ((GetGenericSensor(logicalDevice) ?? throw new LogicalDeviceHistoryDataException("Error retrieving generic sensor history data. Could not find accessory."))!.AccessoryConnectionManager ?? throw new LogicalDeviceHistoryDataException("Error retrieving generic sensor history data."))!.GetAccessoryHistoryDataAsync(block, startIndex, dataLength, cancellationToken);
		}
	}
}
