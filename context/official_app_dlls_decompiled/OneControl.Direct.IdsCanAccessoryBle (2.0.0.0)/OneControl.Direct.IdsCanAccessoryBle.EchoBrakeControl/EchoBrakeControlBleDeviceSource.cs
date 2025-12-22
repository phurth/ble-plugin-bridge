using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using ids.portable.ble.Ble;
using ids.portable.ble.Exceptions;
using ids.portable.ble.ScanResults;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.EchoBrakeControl;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public class EchoBrakeControlBleDeviceSource : CommonDisposable, IEchoBrakeControlBleDeviceSource, ILogicalDeviceSourceDirectEchoBrakeControl, ILogicalDeviceSourceDirectConnection, ILogicalDeviceSourceDirect, ILogicalDeviceSource, ILogicalDeviceSourceConnection, IEchoBrakeControlCommandsWithLogicalDevice, IAccessoryBleDeviceSource<SensorConnectionEchoBrakeControl>, IAccessoryBleDeviceSource, ICommonDisposable, IDisposable, ILogicalDeviceSourceDirectIdsAccessory, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata, IAccessoryBleDeviceSourceDevices<EchoBrakeControlBleDeviceDriver>
	{
		private readonly IBleService _bleService;

		private const string LogTag = "EchoBrakeControlBleDeviceSource";

		private const string EchoBrakeControlSoftwarePartNumber = "EchoV1";

		private readonly ConcurrentDictionary<Guid, EchoBrakeControlBleDeviceDriver> _registeredEchoBrakeControls = new ConcurrentDictionary<Guid, EchoBrakeControlBleDeviceDriver>();

		private const string DeviceSourceTokenDefault = "Ids.Accessory.EchoBrakeControl.Default";

		public bool IsStarted;

		public string DeviceSourceToken { get; }

		public bool AllowAutoOfflineLogicalDeviceRemoval => false;

		public bool IsDeviceSourceActive => Enumerable.Any(_registeredEchoBrakeControls);

		public ILogicalDeviceService DeviceService { get; }

		public IEnumerable<ISensorConnection> SensorConnectionsAll
		{
			get
			{
				foreach (EchoBrakeControlBleDeviceDriver value in _registeredEchoBrakeControls.Values)
				{
					yield return value.SensorConnection;
				}
			}
		}

		public IEnumerable<EchoBrakeControlBleDeviceDriver> SensorDevices => _registeredEchoBrakeControls.Values;

		public IN_MOTION_LOCKOUT_LEVEL InTransitLockoutLevel => (byte)0;

		public ILogicalDeviceSessionManager? SessionManager
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public IReadOnlyList<ILogicalDeviceTag> ConnectionTagList
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool IsConnected => IsStarted;

		public event Action<ILogicalDeviceSourceDirectConnection>? DidConnectEvent;

		public event Action<ILogicalDeviceSourceDirectConnection>? DidDisconnectEvent;

		public event UpdateDeviceSourceReachabilityEventHandler? UpdateDeviceSourceReachabilityEvent;

		public EchoBrakeControlBleDeviceSource(IBleService bleService, ILogicalDeviceService deviceService, string deviceSourceToken = "Ids.Accessory.EchoBrakeControl.Default")
		{
			_bleService = bleService;
			DeviceService = deviceService;
			DeviceSourceToken = deviceSourceToken ?? "Ids.Accessory.EchoBrakeControl.Default";
			_bleService.Scanner.DidReceiveScanResult += ReceivedBleScanResult;
		}

		public ILogicalDeviceEchoBrakeControl? GetEchoBrakeControlLogicalDevice(SensorConnectionEchoBrakeControl connection)
		{
			foreach (KeyValuePair<Guid, EchoBrakeControlBleDeviceDriver> registeredEchoBrakeControl in _registeredEchoBrakeControls)
			{
				if (registeredEchoBrakeControl.Value.SensorConnection.ConnectionGuid == connection.ConnectionGuid)
				{
					return registeredEchoBrakeControl.Value.LogicalDevice;
				}
			}
			return null;
		}

		public bool RegisterSensor(SensorConnectionEchoBrakeControl sensorConnection)
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
					EchoBrakeControlBleDeviceDriver echoBrakeControlBleDeviceDriver = new EchoBrakeControlBleDeviceDriver(_bleService.Manager, this, sensorConnection);
					echoBrakeControlBleDeviceDriver.UpdateEchoBrakeControlReachabilityEvent += DeviceReachabilityUpdated;
					bool num = _registeredEchoBrakeControls.TryAdd(valueOrDefault, echoBrakeControlBleDeviceDriver);
					if (num)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(28, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Register Echo Brake Control ");
						defaultInterpolatedStringHandler.AppendFormatted(valueOrDefault);
						TaggedLog.Debug("EchoBrakeControlBleDeviceSource", defaultInterpolatedStringHandler.ToStringAndClear());
					}
					if (IsStarted)
					{
						echoBrakeControlBleDeviceDriver.Start();
					}
					return num;
				}
				catch (BleScannerServiceAlreadyRegisteredException)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(42, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Echo Brake Control already registered for ");
					defaultInterpolatedStringHandler.AppendFormatted(valueOrDefault);
					TaggedLog.Debug("EchoBrakeControlBleDeviceSource", defaultInterpolatedStringHandler.ToStringAndClear());
					return false;
				}
				catch (Exception ex2)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(39, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Echo Brake Control error registering ");
					defaultInterpolatedStringHandler.AppendFormatted(valueOrDefault);
					defaultInterpolatedStringHandler.AppendLiteral(": ");
					defaultInterpolatedStringHandler.AppendFormatted(ex2.Message);
					TaggedLog.Error("EchoBrakeControlBleDeviceSource", defaultInterpolatedStringHandler.ToStringAndClear());
					return false;
				}
			}
			return false;
		}

		private void DeviceReachabilityUpdated(EchoBrakeControlBleDeviceDriver echoBrakeControlBle)
		{
			this.UpdateDeviceSourceReachabilityEvent?.Invoke(this);
			if (echoBrakeControlBle.IsConnected)
			{
				this.DidConnectEvent?.Invoke(this);
			}
			else
			{
				this.DidDisconnectEvent?.Invoke(this);
			}
			echoBrakeControlBle.LogicalDevice?.UpdateDeviceOnline(echoBrakeControlBle.IsConnected);
		}

		public void UnRegisterSensor(Guid bleDeviceId)
		{
			if (_registeredEchoBrakeControls.TryRemove(bleDeviceId, out var echoBrakeControlBleDeviceDriver))
			{
				echoBrakeControlBleDeviceDriver.UpdateEchoBrakeControlReachabilityEvent -= DeviceReachabilityUpdated;
				echoBrakeControlBleDeviceDriver.TryDispose();
			}
		}

		public bool IsSensorRegistered(Guid bleDeviceId)
		{
			return _registeredEchoBrakeControls.ContainsKey(bleDeviceId);
		}

		public bool IsLogicalDeviceSupported(ILogicalDevice? logicalDevice)
		{
			foreach (EchoBrakeControlBleDeviceDriver value in _registeredEchoBrakeControls.Values)
			{
				if (value.LogicalDevice == logicalDevice)
				{
					return true;
				}
			}
			return false;
		}

		public bool IsLogicalDeviceOnline(ILogicalDevice? logicalDevice)
		{
			foreach (EchoBrakeControlBleDeviceDriver value in _registeredEchoBrakeControls.Values)
			{
				if (value.LogicalDevice == logicalDevice)
				{
					return value.IsConnected;
				}
			}
			return false;
		}

		public EchoBrakeControlBleDeviceDriver? GetSensorDevice(ILogicalDevice? logicalDevice)
		{
			return Enumerable.FirstOrDefault(_registeredEchoBrakeControls.Values, (EchoBrakeControlBleDeviceDriver ts) => true);
		}

		public IEnumerable<ILogicalDeviceTag> MakeDeviceSourceTags(ILogicalDevice? logicalDevice)
		{
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
		}

		public bool IsLogicalDeviceRenameSupported(ILogicalDevice? logicalDevice)
		{
			return IsLogicalDeviceSupported(logicalDevice);
		}

		public async Task RenameLogicalDevice(ILogicalDevice? logicalDevice, FUNCTION_NAME toName, byte toFunctionInstance, CancellationToken cancellationToken)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(109, 3);
			defaultInterpolatedStringHandler.AppendLiteral("Echo Brake Control rename: LogicalDevice: ");
			defaultInterpolatedStringHandler.AppendFormatted(logicalDevice);
			defaultInterpolatedStringHandler.AppendLiteral("  toName: ");
			defaultInterpolatedStringHandler.AppendFormatted(toName);
			defaultInterpolatedStringHandler.AppendLiteral(" toFunctionInstance: ");
			defaultInterpolatedStringHandler.AppendFormatted(toFunctionInstance);
			defaultInterpolatedStringHandler.AppendLiteral(". This has not been implemented yet.");
			TaggedLog.Debug("EchoBrakeControlBleDeviceSource", defaultInterpolatedStringHandler.ToStringAndClear());
			throw new NotImplementedException();
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
			foreach (KeyValuePair<Guid, EchoBrakeControlBleDeviceDriver> registeredEchoBrakeControl in _registeredEchoBrakeControls)
			{
				UnRegisterSensor(registeredEchoBrakeControl.Key);
			}
			_registeredEchoBrakeControls.Clear();
		}

		public Task<string> GetSoftwarePartNumberAsync(ILogicalDevice logicalDevice, CancellationToken cancelToken)
		{
			return Task.FromResult("EchoV1");
		}

		public Version? GetDeviceProtocolVersion(ILogicalDevice logicalDevice)
		{
			return null;
		}

		public void Start()
		{
			IsStarted = true;
			foreach (KeyValuePair<Guid, EchoBrakeControlBleDeviceDriver> registeredEchoBrakeControl in _registeredEchoBrakeControls)
			{
				registeredEchoBrakeControl.Value.Start();
			}
		}

		public void Stop()
		{
			IsStarted = false;
			foreach (KeyValuePair<Guid, EchoBrakeControlBleDeviceDriver> registeredEchoBrakeControl in _registeredEchoBrakeControls)
			{
				registeredEchoBrakeControl.Value.Stop();
			}
		}

		public LogicalDeviceReachability DeviceSourceReachability(ILogicalDevice logicalDevice)
		{
			return FindAssociatedBleControlForLogicalDevice(logicalDevice)?.Reachability(logicalDevice) ?? LogicalDeviceReachability.Unknown;
		}

		private EchoBrakeControlBleDeviceDriver? FindAssociatedBleControlForLogicalDevice(ILogicalDevice logicalDevice)
		{
			foreach (KeyValuePair<Guid, EchoBrakeControlBleDeviceDriver> registeredEchoBrakeControl in _registeredEchoBrakeControls)
			{
				if (logicalDevice == registeredEchoBrakeControl.Value.LogicalDevice)
				{
					return registeredEchoBrakeControl.Value;
				}
			}
			return null;
		}

		public Task<byte> GetMaxBrakingPowerAsync(ILogicalDevice logicalDevice, EchoBrakeControlProfile profile, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.GetMaxBrakingPowerAsync(profile, cancellationToken);
		}

		public Task<byte> SetMaxBrakingPowerAsync(ILogicalDevice logicalDevice, EchoBrakeControlProfile profile, byte maxBrakingPower, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.SetMaxBrakingPowerAsync(profile, maxBrakingPower, cancellationToken);
		}

		public async Task<EchoBrakeControlSensitivity> GetBrakePowerSensitivityAsync(ILogicalDevice logicalDevice, EchoBrakeControlProfile profile, CancellationToken cancellationToken)
		{
			return await (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.GetBrakePowerSensitivityAsync(profile, cancellationToken);
		}

		public Task SetBrakePowerSensitivityAsync(ILogicalDevice logicalDevice, EchoBrakeControlProfile profile, EchoBrakeControlSensitivity sensitivity, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.SetBrakePowerSensitivityAsync(profile, sensitivity, cancellationToken);
		}

		public Task<EchoBrakeControlProfile> GetSelectedProfileAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.GetSelectedProfileAsync(cancellationToken);
		}

		public Task SetSelectedProfileAsync(ILogicalDevice logicalDevice, EchoBrakeControlProfile profile, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.SetSelectedProfileAsync(profile, cancellationToken);
		}

		public Task SetManualOverrideAsync(ILogicalDevice logicalDevice, byte outputDutyCycle, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.SetManualOverrideAsync(outputDutyCycle, cancellationToken);
		}

		public Task SetManualOverrideAsync(ILogicalDevice logicalDevice, ManualOverrideState manualOverrideState, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.SetManualOverrideAsync(manualOverrideState, cancellationToken);
		}

		public Task PerformOperationManualOverride(ILogicalDevice logicalDevice, Func<EchoBrakeControlOperationAck> progressAck, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.PerformOperationManualOverride(progressAck, cancellationToken);
		}

		public Task<string> GetProfileNameAsync(ILogicalDevice logicalDevice, EchoBrakeControlProfile profile, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.GetProfileNameAsync(profile, cancellationToken);
		}

		public Task SetProfileNameAsync(ILogicalDevice logicalDevice, EchoBrakeControlProfile profile, string name, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.SetProfileNameAsync(profile, name, cancellationToken);
		}

		public Task<bool> GetHazardModeOverrideSwitchStateAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.GetHazardModeOverrideSwitchStateAsync(cancellationToken);
		}

		public Task SetHazardModeOverrideSwitchStateAsync(ILogicalDevice logicalDevice, bool state, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.SetHazardModeOverrideSwitchStateAsync(state, cancellationToken);
		}

		public Task<string> GetProductNameAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.GetProductNameAsync(cancellationToken);
		}

		public Task<string> GetModelNumberAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.GetModelNumberAsync(cancellationToken);
		}

		public Task<string> GetFirmwareVersionAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.GetFirmwareVersionAsync(cancellationToken);
		}

		public Task<string> GetSerialNumberAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.GetSerialNumberAsync(cancellationToken);
		}

		public Task<string> GetProductNicknameAsync(ILogicalDevice logicalDevice, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.GetProductNicknameAsync(cancellationToken);
		}

		public Task SetProductNicknameAsync(ILogicalDevice logicalDevice, string nickname, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.SetProductNicknameAsync(nickname, cancellationToken);
		}

		public IAsyncEnumerable<IEchoBrakeControlProfile> ReadProfilesAllAsync(ILogicalDevice logicalDevice, IEchoBrakeControlProfilesMutable? mutableProfiles, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.ReadProfilesAllAsync(mutableProfiles, cancellationToken);
		}

		public Task WriteProfilesAsync(ILogicalDevice logicalDevice, IEnumerable<IEchoBrakeControlProfile> profiles, CancellationToken cancellationToken)
		{
			return (FindAssociatedBleControlForLogicalDevice(logicalDevice) ?? throw new EchoBrakeControlDeviceNotFoundException("Couldn't find device associated to logical device"))!.WriteProfilesAsync(profiles, cancellationToken);
		}
	}
}
