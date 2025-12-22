using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using ids.portable.ble.BleManager;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;
using OneControl.Devices.EchoBrakeControl;
using OneControl.Direct.IdsCanAccessoryBle.Connections;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace OneControl.Direct.IdsCanAccessoryBle.EchoBrakeControl
{
	public class EchoBrakeControlBleDeviceDriver : BackgroundOperation, IEchoBrakeControlCommands, ICommonDisposable, IDisposable
	{
		private const string LogTag = "EchoBrakeControlBleDeviceDriver";

		private const int SleepTimeMs = 15000;

		public const int EchoMaxProfiles = 5;

		public const int WaitForBleDeviceTimeMs = 30000;

		public const int WaitForBleDeviceRetryTimeMs = 1000;

		private const int ProductNameByteSize = 64;

		private const int ModelNumberByteSize = 32;

		private const int VersionInfoByteSize = 4;

		private const int SerialNumberByteSize = 10;

		private const int ProductNicknameByteSize = 64;

		private const int ProfileNameByteSize = 30;

		private const int ProfileMaxBrakingPowerSize = 1;

		private const int ProfileBrakePowerSensitivitySize = 1;

		private const int HazardModeOverrideSwitchBitIndex = 6;

		private const int TrailerConnectionByteIndex = 0;

		private const int TrailerConnectionBitIndex = 1;

		private const int OutputOverloadByteIndex = 0;

		private const int OutputOverloadBitIndex = 0;

		private const int BatteryLevelByteStartIndex = 1;

		private const int BatteryLevelByteEndIndex = 2;

		public readonly PRODUCT_ID ProductId = PRODUCT_ID.CURT_ECHO_BRAKE_CONTROLLER;

		public readonly DEVICE_TYPE DeviceType = (byte)53;

		public readonly FUNCTION_NAME FunctionName = (ushort)329;

		public const byte FunctionInstance = 0;

		private static readonly Guid ServiceGuid = new Guid("00005000-4375-7274-204d-6667204c4c43");

		private static readonly Guid MaxBrakeSettingCharacteristicGuid = new Guid("00005001-4375-7274-204d-6667204c4c43");

		private static readonly Guid SensitivitySettingCharacteristicGuid = new Guid("00005002-4375-7274-204d-6667204c4c43");

		private static readonly Guid SelectedSettingCharacteristicGuid = new Guid("00005003-4375-7274-204d-6667204c4c43");

		private static readonly Guid BrakeManualOverrideCharacteristicGuid = new Guid("00005004-4375-7274-204d-6667204c4c43");

		private static readonly Guid BrakeOutputCharacteristicGuid = new Guid("00005005-4375-7274-204d-6667204c4c43");

		private static readonly Guid DiagnosticCharacteristicGuid = new Guid("00005006-4375-7274-204d-6667204c4c43");

		private static readonly Guid ProfileNameCharacteristicGuid = new Guid("00005007-4375-7274-204d-6667204c4c43");

		private static readonly Guid HazardSwitchCharacteristicGuid = new Guid("00005008-4375-7274-204d-6667204c4c43");

		private static readonly Guid ProductNameCharacteristicGuid = new Guid("0000FF01-4375-7274-204d-6667204c4c43");

		private static readonly Guid ModelNumberCharacteristicGuid = new Guid("0000FF02-4375-7274-204d-6667204c4c43");

		private static readonly Guid VersionInfoCharacteristicGuid = new Guid("0000FF03-4375-7274-204d-6667204c4c43");

		private static readonly Guid SerialNumberCharacteristicGuid = new Guid("0000FF04-4375-7274-204d-6667204c4c43");

		private static readonly Guid ProductNicknameCharacteristicGuid = new Guid("0000FF05-4375-7274-204d-6667204c4c43");

		private readonly IEchoBrakeControlBleDeviceSource _sourceDirect;

		private readonly IBleManager _bleManager;

		private Plugin.BLE.Abstractions.Contracts.IDevice? _bleDevice;

		private ICharacteristic? _brakeOutputCharacteristic;

		private ICharacteristic? _diagnosticCharacteristic;

		private int _isDisposed;

		public const int ManualOverrideDelayTimeMs = 250;

		public ILogicalDeviceEchoBrakeControl? LogicalDevice { get; private set; }

		public bool IsConnected { get; private set; }

		public SensorConnectionEchoBrakeControl SensorConnection { get; }

		internal Guid BleDeviceId => SensorConnection.ConnectionGuid;

		public MAC AccessoryMacAddress { get; }

		public bool IsDisposed => _isDisposed != 0;

		public static IReadOnlyList<EchoBrakeControlProfile> AllProfiles => new List<EchoBrakeControlProfile>
		{
			EchoBrakeControlProfile.Profile1,
			EchoBrakeControlProfile.Profile2,
			EchoBrakeControlProfile.Profile3,
			EchoBrakeControlProfile.Profile4,
			EchoBrakeControlProfile.Profile5
		};

		public event UpdateEchoBrakeControlReachabilityEventHandler? UpdateEchoBrakeControlReachabilityEvent;

		public EchoBrakeControlBleDeviceDriver(IBleManager bleManager, IEchoBrakeControlBleDeviceSource sourceDirect, SensorConnectionEchoBrakeControl sensorConnection)
		{
			_bleManager = bleManager ?? throw new ArgumentNullException("bleManager");
			_sourceDirect = sourceDirect;
			SensorConnection = sensorConnection ?? throw new ArgumentNullException("sensorConnection");
			AccessoryMacAddress = sensorConnection.AccessoryMacAddress;
			LogicalDevice = CreateLogicalDevice();
		}

		private ILogicalDeviceEchoBrakeControl? CreateLogicalDevice()
		{
			if (LogicalDevice != null)
			{
				return null;
			}
			TaggedLog.Information("EchoBrakeControlBleDeviceDriver", "Creating Logical Device for Echo Brake Controller");
			LogicalDeviceId logicalDeviceId = new LogicalDeviceId(DeviceType, 0, FunctionName, 0, ProductId, AccessoryMacAddress);
			ILogicalDevice logicalDevice = _sourceDirect.DeviceService.DeviceManager?.AddLogicalDevice(logicalDeviceId, 0, _sourceDirect, (ILogicalDevice ld) => true);
			if (!(logicalDevice is ILogicalDeviceEchoBrakeControl result) || logicalDevice.IsDisposed)
			{
				TaggedLog.Warning("EchoBrakeControlBleDeviceDriver", "Unable to create LogicalDeviceEchoBrakeControl");
				return null;
			}
			return result;
		}

		protected override async Task BackgroundOperationAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested && !IsDisposed)
			{
				try
				{
					_bleDevice?.TryDispose();
					_bleDevice = await _bleManager.ConnectToNonLippertDeviceAsync(BleDeviceId, cancellationToken);
					if (_bleDevice == null)
					{
						throw new EchoBrakeControlException("Failed to connect to device.");
					}
					_brakeOutputCharacteristic = await _bleManager.GetCharacteristicAsync(_bleDevice, ServiceGuid, BrakeOutputCharacteristicGuid, cancellationToken);
					if (_brakeOutputCharacteristic == null)
					{
						throw new EchoBrakeControlException("Failed to get brake characteristic notify update");
					}
					_brakeOutputCharacteristic!.ValueUpdated += OnBrakeDataReceived;
					if (!(await _bleManager.StartCharacteristicUpdatesAsync(_brakeOutputCharacteristic)))
					{
						throw new EchoBrakeControlException("Failed to start brake characteristic notify update");
					}
					_diagnosticCharacteristic = await _bleManager.GetCharacteristicAsync(_bleDevice, ServiceGuid, DiagnosticCharacteristicGuid, cancellationToken);
					if (_diagnosticCharacteristic == null)
					{
						throw new EchoBrakeControlException("Failed to get diagnostic characteristic notify update");
					}
					_diagnosticCharacteristic!.ValueUpdated += OnDiagnosticDataReceived;
					if (!(await _bleManager.StartCharacteristicUpdatesAsync(_diagnosticCharacteristic)))
					{
						throw new EchoBrakeControlException("Failed to start diagnostic characteristic notify update");
					}
					IsConnected = true;
					this.UpdateEchoBrakeControlReachabilityEvent?.Invoke(this);
				}
				catch (Exception ex)
				{
					TaggedLog.Error("EchoBrakeControlBleDeviceDriver", "Failed to connect to Echo Brake Control, message: " + ex.Message);
					await TaskExtension.TryDelay(15000, cancellationToken);
					continue;
				}
				while (_bleDevice != null && (_bleDevice!.State == DeviceState.Connecting || _bleDevice!.State == DeviceState.Connected) && !cancellationToken.IsCancellationRequested)
				{
					await TaskExtension.TryDelay(15000, cancellationToken);
				}
				TryDisconnect();
			}
		}

		private void TryDisconnect()
		{
			IsConnected = false;
			this.UpdateEchoBrakeControlReachabilityEvent?.Invoke(this);
			try
			{
				_brakeOutputCharacteristic!.ValueUpdated -= OnBrakeDataReceived;
			}
			catch
			{
			}
			try
			{
				_diagnosticCharacteristic!.ValueUpdated -= OnDiagnosticDataReceived;
			}
			catch
			{
			}
			_bleDevice?.TryDispose();
			_bleDevice = null;
		}

		private void OnBrakeDataReceived(object sender, CharacteristicUpdatedEventArgs args)
		{
			try
			{
				byte[] value = args.Characteristic.Value;
				if (value != null && value.Length != 0)
				{
					LogicalDevice?.UpdateBrakeOutput(value[0]);
				}
			}
			catch (Exception ex)
			{
				throw new EchoBrakeControlException("EchoBrakeControlBleDeviceDriver - Notified on new Brake Characteristic data, failed to update the logical device, Message: " + ex.Message);
			}
		}

		private void OnDiagnosticDataReceived(object sender, CharacteristicUpdatedEventArgs args)
		{
			try
			{
				byte[] value = args.Characteristic.Value;
				if (value != null && value.Length != 0)
				{
					LogicalDevice?.UpdateTrailerConnection(value[0].IsBitSet(1));
					LogicalDevice?.UpdateOutputOverload(value[0].IsBitSet(0));
					ushort batteryLevel = BitConverter.ToUInt16(new byte[2]
					{
						value[1],
						value[2]
					}, 0);
					LogicalDevice?.UpdateBatteryLevel(batteryLevel);
				}
			}
			catch (Exception ex)
			{
				throw new EchoBrakeControlException("EchoBrakeControlBleDeviceDriver - Notified on new Diagnostic Characteristic data, failed to update the logical device, Message: " + ex.Message);
			}
		}

		public LogicalDeviceReachability Reachability(ILogicalDevice logicalDevice)
		{
			if (logicalDevice != LogicalDevice)
			{
				return LogicalDeviceReachability.Unknown;
			}
			if (!IsConnected)
			{
				return LogicalDeviceReachability.Unreachable;
			}
			return LogicalDeviceReachability.Reachable;
		}

		public void TryDispose()
		{
			try
			{
				if (!IsDisposed)
				{
					Dispose();
				}
			}
			catch
			{
			}
		}

		public void Dispose()
		{
			if (!IsDisposed && Interlocked.Exchange(ref _isDisposed, 1) == 0)
			{
				Dispose(disposing: true);
			}
		}

		public virtual void Dispose(bool disposing)
		{
			TryDisconnect();
			Stop();
		}

		private async Task<byte[]> GetRawMaxBrakingPowersAsync(CancellationToken cancellationToken)
		{
			return await ReadByteDataAsync(MaxBrakeSettingCharacteristicGuid, 5, cancellationToken);
		}

		[AsyncIteratorStateMachine(typeof(_003CGetMaxBrakingPowersAsync_003Ed__76))]
		private IAsyncEnumerable<byte> GetMaxBrakingPowersAsync(CancellationToken cancellationToken)
		{
			return new _003CGetMaxBrakingPowersAsync_003Ed__76(-2)
			{
				_003C_003E4__this = this,
				_003C_003E3__cancellationToken = cancellationToken
			};
		}

		public async Task<byte> GetMaxBrakingPowerAsync(EchoBrakeControlProfile profile, CancellationToken cancellationToken)
		{
			int echoBrakeProfileIndex = profile.GetEchoBrakeProfileIndex();
			return await GetMaxBrakingPowersAsync(cancellationToken).Skip(echoBrakeProfileIndex).Take(1).FirstOrDefaultAsync(cancellationToken);
		}

		public async Task<byte> SetMaxBrakingPowerAsync(EchoBrakeControlProfile profile, byte maxBrakingPower, CancellationToken cancellationToken)
		{
			maxBrakingPower = (byte)MathCommon.Clamp(maxBrakingPower, 5, 100);
			int profileIndex = profile.GetEchoBrakeProfileIndex();
			byte[] array = await ReadByteDataAsync(MaxBrakeSettingCharacteristicGuid, 5, cancellationToken);
			array[profileIndex] = maxBrakingPower;
			await WriteByteDataAsync(array, MaxBrakeSettingCharacteristicGuid, cancellationToken);
			return maxBrakingPower;
		}

		private async Task<byte[]> GetRawBrakePowerSensitivitiesAsync(CancellationToken cancellationToken)
		{
			byte[] array = await ReadByteDataAsync(SensitivitySettingCharacteristicGuid, 5, cancellationToken);
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (byte)MathCommon.Clamp(array[i], 0, 8);
			}
			return array;
		}

		[AsyncIteratorStateMachine(typeof(_003CGetBrakePowerSensitivitiesAsync_003Ed__80))]
		private IAsyncEnumerable<EchoBrakeControlSensitivity> GetBrakePowerSensitivitiesAsync(CancellationToken cancellationToken)
		{
			return new _003CGetBrakePowerSensitivitiesAsync_003Ed__80(-2)
			{
				_003C_003E4__this = this,
				_003C_003E3__cancellationToken = cancellationToken
			};
		}

		public async Task<EchoBrakeControlSensitivity> GetBrakePowerSensitivityAsync(EchoBrakeControlProfile profile, CancellationToken cancellationToken)
		{
			int echoBrakeProfileIndex = profile.GetEchoBrakeProfileIndex();
			return await GetBrakePowerSensitivitiesAsync(cancellationToken).Skip(echoBrakeProfileIndex).Take(1).FirstOrDefaultAsync(cancellationToken);
		}

		public async Task SetBrakePowerSensitivityAsync(EchoBrakeControlProfile profile, EchoBrakeControlSensitivity sensitivity, CancellationToken cancellationToken)
		{
			int profileIndex = profile.GetEchoBrakeProfileIndex();
			if (!IsEchoBrakeControlSensitivityValid(sensitivity))
			{
				throw new EchoBrakeControlInvalidInputException("EchoBrakeControlBleDeviceDriver", "Invalid sensitivity, sensitivity cannot be unknown.", "SetBrakePowerSensitivityAsync");
			}
			byte[] array = await ReadByteDataAsync(SensitivitySettingCharacteristicGuid, 5, cancellationToken);
			array[profileIndex] = (byte)sensitivity;
			await WriteByteDataAsync(array, SensitivitySettingCharacteristicGuid, cancellationToken);
		}

		public async Task<EchoBrakeControlProfile> GetSelectedProfileAsync(CancellationToken cancellationToken)
		{
			return Enum<EchoBrakeControlProfile>.TryConvert((await ReadByteDataAsync(SelectedSettingCharacteristicGuid, 1, cancellationToken))[0]);
		}

		public async Task SetSelectedProfileAsync(EchoBrakeControlProfile profile, CancellationToken cancellationToken)
		{
			int echoBrakeProfileIndex = profile.GetEchoBrakeProfileIndex();
			await WriteByteDataAsync(new byte[1] { (byte)echoBrakeProfileIndex }, SelectedSettingCharacteristicGuid, cancellationToken);
		}

		[Obsolete("See SetManualOverrideAsync with manual override state")]
		public async Task SetManualOverrideAsync(byte outputDutyCycle, CancellationToken cancellationToken)
		{
			await WriteByteDataAsync(new byte[1] { outputDutyCycle }, BrakeManualOverrideCharacteristicGuid, cancellationToken);
		}

		public async Task SetManualOverrideAsync(ManualOverrideState manualOverrideState, CancellationToken cancellationToken)
		{
			await WriteByteDataAsync(new byte[1] { (byte)manualOverrideState }, BrakeManualOverrideCharacteristicGuid, cancellationToken);
		}

		public async Task PerformOperationManualOverride(Func<EchoBrakeControlOperationAck> progressAck, CancellationToken cancellationToken)
		{
			await WaitForBleDeviceConnectionAsync(cancellationToken);
			try
			{
				await SetManualOverrideAsync(ManualOverrideState.Enable, cancellationToken);
				while (progressAck() != 0)
				{
					await Task.Delay(250, cancellationToken);
					await SetManualOverrideAsync(ManualOverrideState.Continue, cancellationToken);
				}
			}
			finally
			{
				try
				{
					await SetManualOverrideAsync(ManualOverrideState.Disable, CancellationToken.None);
				}
				catch (Exception ex)
				{
					string message = "Unable to stop manual operation because " + ex.Message;
					TaggedLog.Error("EchoBrakeControlBleDeviceDriver", message);
					throw new EchoBrakeControlUnableToStopManualOperationException(message, ex);
				}
			}
		}

		private async Task<byte[]> GetRawProfileNamesAsync(CancellationToken cancellationToken)
		{
			return await ReadByteDataAsync(ProfileNameCharacteristicGuid, 150, cancellationToken);
		}

		private async IAsyncEnumerable<string> GetProfileNamesAsync(CancellationToken cancellationToken)
		{
			byte[] allRawProfileNames = await GetRawProfileNamesAsync(cancellationToken);
			for (int profileIndex = 0; profileIndex < 5; profileIndex++)
			{
				byte[] array = new ArraySegment<byte>(allRawProfileNames, 30 * profileIndex, 30).ToArray();
				yield return Encoding.ASCII.GetString(array).Trim();
			}
		}

		public async Task<string> GetProfileNameAsync(EchoBrakeControlProfile profile, CancellationToken cancellationToken)
		{
			int echoBrakeProfileIndex = profile.GetEchoBrakeProfileIndex();
			return await GetProfileNamesAsync(cancellationToken).Skip(echoBrakeProfileIndex).Take(1).FirstOrDefaultAsync(cancellationToken);
		}

		public async Task SetProfileNameAsync(EchoBrakeControlProfile profile, string name, CancellationToken cancellationToken)
		{
			if (name.Length > 30)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Too large, name must be < ");
				defaultInterpolatedStringHandler.AppendFormatted(30);
				throw new ArgumentOutOfRangeException("name", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			int profileIndex = profile.GetEchoBrakeProfileIndex();
			byte[] array = await ReadByteDataAsync(ProfileNameCharacteristicGuid, 150, cancellationToken);
			byte[] bytes = Encoding.UTF8.GetBytes(name.PadRight(30, ' '));
			int num = 30 * profileIndex;
			int num2 = num + 30;
			for (int i = num; i < num2; i++)
			{
				array[i] = bytes[i - num];
			}
			await WriteByteDataAsync(array, ProfileNameCharacteristicGuid, cancellationToken);
		}

		public async Task<bool> GetHazardModeOverrideSwitchStateAsync(CancellationToken cancellationToken)
		{
			return (await ReadByteDataAsync(HazardSwitchCharacteristicGuid, 1, cancellationToken))[0].IsBitSet(6);
		}

		public async Task SetHazardModeOverrideSwitchStateAsync(bool state, CancellationToken cancellationToken)
		{
			byte data = 0;
			data.SetBit(6, state);
			await WriteByteDataAsync(new byte[1] { data }, HazardSwitchCharacteristicGuid, cancellationToken);
		}

		public async Task<string> GetProductNameAsync(CancellationToken cancellationToken)
		{
			return (await GetAsciiCharacteristicToStringWithoutNullCharactersAsync(ProductNameCharacteristicGuid, 64, cancellationToken)).Trim();
		}

		public async Task<string> GetModelNumberAsync(CancellationToken cancellationToken)
		{
			return (await GetAsciiCharacteristicToStringWithoutNullCharactersAsync(ModelNumberCharacteristicGuid, 32, cancellationToken)).Trim();
		}

		public async Task<string> GetFirmwareVersionAsync(CancellationToken cancellationToken)
		{
			return (await GetAsciiCharacteristicToStringWithoutNullCharactersAsync(VersionInfoCharacteristicGuid, 4, cancellationToken)).Trim();
		}

		public async Task<string> GetSerialNumberAsync(CancellationToken cancellationToken)
		{
			return (await GetAsciiCharacteristicToStringWithoutNullCharactersAsync(SerialNumberCharacteristicGuid, 10, cancellationToken)).Trim();
		}

		public static async Task<string> GetSerialNumberAsync(IBleManager bleManager, SensorConnectionEchoBrakeControl sensorConnection, CancellationToken cancellationToken)
		{
			Plugin.BLE.Abstractions.Contracts.IDevice bleDevice = await bleManager.ConnectToNonLippertDeviceAsync(sensorConnection.ConnectionGuid, cancellationToken);
			if (bleDevice == null)
			{
				throw new EchoBrakeControlException("Failed to connect to device.");
			}
			try
			{
				return GetAsciiCharacteristicToStringWithoutNullCharacters((await bleManager.ReadCharacteristicAsync(bleDevice, ServiceGuid, SerialNumberCharacteristicGuid, cancellationToken)) ?? throw new EchoBrakeControlException("Unable to read serialization data"), 10).Trim();
			}
			catch (Exception ex)
			{
				TaggedLog.Error("EchoBrakeControlBleDeviceDriver", "Unable to get serial number: " + ex.Message);
				throw;
			}
			finally
			{
				bleDevice.TryDispose();
			}
		}

		public async Task<string> GetProductNicknameAsync(CancellationToken cancellationToken)
		{
			return (await GetAsciiCharacteristicToStringWithoutNullCharactersAsync(ProductNicknameCharacteristicGuid, 64, cancellationToken)).Trim();
		}

		public async Task SetProductNicknameAsync(string nickname, CancellationToken cancellationToken)
		{
			if (nickname.Length > 64)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 1);
				defaultInterpolatedStringHandler.AppendLiteral("Too large, name must be < ");
				defaultInterpolatedStringHandler.AppendFormatted(64);
				throw new ArgumentOutOfRangeException("nickname", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			byte[] bytes = Encoding.UTF8.GetBytes(nickname.PadRight(64, '\n'));
			await WriteByteDataAsync(bytes, ProductNicknameCharacteristicGuid, cancellationToken);
		}

		public async IAsyncEnumerable<IEchoBrakeControlProfile> ReadProfilesAllAsync(IEchoBrakeControlProfilesMutable? mutableProfiles, CancellationToken cancellationToken)
		{
			List<string> profileNames = await GetProfileNamesAsync(cancellationToken).ToListAsync(cancellationToken);
			List<EchoBrakeControlSensitivity> brakePowerSensitivities = await GetBrakePowerSensitivitiesAsync(cancellationToken).ToListAsync(cancellationToken);
			List<byte> maxBrakingPowers = await GetMaxBrakingPowersAsync(cancellationToken).ToListAsync(cancellationToken);
			foreach (EchoBrakeControlProfile allProfile in AllProfiles)
			{
				int echoBrakeProfileIndex = allProfile.GetEchoBrakeProfileIndex();
				string text = profileNames[echoBrakeProfileIndex];
				EchoBrakeControlSensitivity echoBrakeControlSensitivity = brakePowerSensitivities[echoBrakeProfileIndex];
				byte b = maxBrakingPowers[echoBrakeProfileIndex];
				yield return mutableProfiles?.UpdateEchoBrakeControlProfile(allProfile, text, echoBrakeControlSensitivity, b) ?? new EchoBrakeControlProfileEntry(allProfile, text, echoBrakeControlSensitivity, b);
			}
		}

		public async Task WriteProfilesAsync(IEnumerable<IEchoBrakeControlProfile> profiles, CancellationToken cancellationToken)
		{
			HashSet<EchoBrakeControlProfile> hashSet = new HashSet<EchoBrakeControlProfile>(AllProfiles);
			byte[] array = new byte[150];
			byte[] brakePowerSensitivities = new byte[5];
			byte[] maxBrakingPowers = new byte[5];
			foreach (IEchoBrakeControlProfile profile in profiles)
			{
				if (!hashSet.Contains(profile.ProfileId))
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(86, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Duplicate profile ");
					defaultInterpolatedStringHandler.AppendFormatted(profile.ProfileId);
					defaultInterpolatedStringHandler.AppendLiteral(" found, profiles must contain exactly ");
					defaultInterpolatedStringHandler.AppendFormatted(5);
					defaultInterpolatedStringHandler.AppendLiteral(" items and each must be unique");
					throw new ArgumentOutOfRangeException("profiles", defaultInterpolatedStringHandler.ToStringAndClear());
				}
				hashSet.Remove(profile.ProfileId);
				int echoBrakeProfileIndex = profile.ProfileId.GetEchoBrakeProfileIndex();
				Buffer.BlockCopy(Encoding.UTF8.GetBytes(profile.ProfileName.PadRight(30, ' ')), 0, array, echoBrakeProfileIndex * 30, 30);
				brakePowerSensitivities[echoBrakeProfileIndex] = (byte)profile.ProfileSensitivity;
				maxBrakingPowers[echoBrakeProfileIndex] = profile.ProfileMaxBrakingPower;
			}
			if (hashSet.Count != 0)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(79, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Missing ");
				defaultInterpolatedStringHandler.AppendFormatted(hashSet.Count);
				defaultInterpolatedStringHandler.AppendLiteral(" profiles, profiles must contain exactly ");
				defaultInterpolatedStringHandler.AppendFormatted(5);
				defaultInterpolatedStringHandler.AppendLiteral(" items and each must be unique");
				throw new ArgumentOutOfRangeException("profiles", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			await WriteByteDataAsync(array, ProfileNameCharacteristicGuid, cancellationToken);
			await WriteByteDataAsync(brakePowerSensitivities, SensitivitySettingCharacteristicGuid, cancellationToken);
			await WriteByteDataAsync(maxBrakingPowers, MaxBrakeSettingCharacteristicGuid, cancellationToken);
		}

		public static string ConvertByteArrayToAsciiString(byte[] data)
		{
			return Encoding.ASCII.GetString(data);
		}

		public bool IsEchoBrakeControlSensitivityValid(EchoBrakeControlSensitivity sensitivity)
		{
			return sensitivity != EchoBrakeControlSensitivity.Unknown;
		}

		private async Task<byte[]> ReadByteDataAsync(Guid characteristicGuid, int requiredSize, CancellationToken cancellationToken)
		{
			_ = 2;
			try
			{
				Plugin.BLE.Abstractions.Contracts.IDevice device = await WaitForBleDeviceConnectionAsync(cancellationToken);
				ICharacteristic characteristic = await _bleManager.GetCharacteristicAsync(device, ServiceGuid, characteristicGuid, cancellationToken);
				if (characteristic == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(42, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to get device's characteristic for ");
					defaultInterpolatedStringHandler.AppendFormatted(characteristicGuid);
					throw new EchoBrakeControlException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				byte[] array = await _bleManager.ReadCharacteristicAsync(characteristic, cancellationToken);
				if (array == null)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to read device's characteristic for ");
					defaultInterpolatedStringHandler.AppendFormatted(characteristicGuid);
					throw new EchoBrakeControlException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				if (array.Length < requiredSize)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Read ");
					defaultInterpolatedStringHandler.AppendFormatted(array.Length);
					defaultInterpolatedStringHandler.AppendLiteral(" when expecting at least ");
					defaultInterpolatedStringHandler.AppendFormatted(requiredSize);
					throw new EchoBrakeControlReadException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				return array;
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 3);
				defaultInterpolatedStringHandler.AppendFormatted("EchoBrakeControlBleDeviceDriver");
				defaultInterpolatedStringHandler.AppendLiteral(" - An error occurred getting the characteristic ");
				defaultInterpolatedStringHandler.AppendFormatted(characteristicGuid);
				defaultInterpolatedStringHandler.AppendLiteral(" data - Exception: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				throw new EchoBrakeControlException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		public async Task<Plugin.BLE.Abstractions.Contracts.IDevice> WaitForBleDeviceConnectionAsync(CancellationToken cancellationToken)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (!base.StartedOrWillStart)
				{
					throw new EchoBrakeControlDeviceServiceNotConnectedException("EchoBrakeControlBleDeviceDriver");
				}
				if (IsConnected)
				{
					Plugin.BLE.Abstractions.Contracts.IDevice bleDevice = _bleDevice;
					if (bleDevice != null)
					{
						return bleDevice;
					}
				}
				if (stopwatch.ElapsedMilliseconds >= 30000)
				{
					break;
				}
				await Task.Delay(1000, cancellationToken);
			}
			throw new EchoBrakeControlDeviceServiceNotConnectedException("EchoBrakeControlBleDeviceDriver");
		}

		private async Task<string> GetAsciiCharacteristicToStringWithoutNullCharactersAsync(Guid characteristicGuid, int minSize, CancellationToken cancellationToken)
		{
			try
			{
				return GetAsciiCharacteristicToStringWithoutNullCharacters(await ReadByteDataAsync(characteristicGuid, minSize, cancellationToken), minSize);
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(73, 3);
				defaultInterpolatedStringHandler.AppendFormatted("EchoBrakeControlBleDeviceDriver");
				defaultInterpolatedStringHandler.AppendLiteral(" - An error occurred getting the ascii characteristic ");
				defaultInterpolatedStringHandler.AppendFormatted(characteristicGuid);
				defaultInterpolatedStringHandler.AppendLiteral(" data - Exception: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				throw new EchoBrakeControlException(defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		private static string GetAsciiCharacteristicToStringWithoutNullCharacters(byte[] data, int minSize)
		{
			try
			{
				return ConvertByteArrayToAsciiString(new ArraySegment<byte>(data, 0, minSize).ToArray()).Replace("\0", " ");
			}
			catch (Exception ex)
			{
				throw new EchoBrakeControlException("EchoBrakeControlBleDeviceDriver - An error occurred converting the ascii characteristic data - Exception: " + ex.Message);
			}
		}

		private async Task WriteByteDataAsync(byte[] data, Guid characteristicGuid, CancellationToken cancellationToken)
		{
			Plugin.BLE.Abstractions.Contracts.IDevice device = await WaitForBleDeviceConnectionAsync(cancellationToken);
			try
			{
				if (data.Length == 0 || await _bleManager.WriteCharacteristicWithResponseAsync(device, ServiceGuid, characteristicGuid, data, cancellationToken))
				{
					return;
				}
				throw new EchoBrakeControlWriteFailureException("EchoBrakeControlBleDeviceDriver", characteristicGuid.ToString(), "WriteByteDataAsync");
			}
			catch (Exception ex)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 3);
				defaultInterpolatedStringHandler.AppendFormatted("EchoBrakeControlBleDeviceDriver");
				defaultInterpolatedStringHandler.AppendLiteral(" - An error occurred writing the characteristic ");
				defaultInterpolatedStringHandler.AppendFormatted(characteristicGuid);
				defaultInterpolatedStringHandler.AppendLiteral(" data - Exception: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				throw new EchoBrakeControlException(defaultInterpolatedStringHandler.ToStringAndClear(), ex);
			}
		}
	}
}
