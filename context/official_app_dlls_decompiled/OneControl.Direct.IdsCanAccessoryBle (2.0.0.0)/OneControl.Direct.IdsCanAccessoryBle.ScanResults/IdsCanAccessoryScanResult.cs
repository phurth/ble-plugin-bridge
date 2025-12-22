using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;
using ids.portable.ble.Ble;
using ids.portable.ble.Extensions;
using ids.portable.ble.Platforms.Shared;
using ids.portable.ble.Platforms.Shared.Reachability;
using ids.portable.ble.Platforms.Shared.ScanResults;
using ids.portable.ble.ScanResults;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public class IdsCanAccessoryScanResult : BleScanResult, IAccessoryScanResult, IBleScanResultWithReachability, IBleScanResult
	{
		private readonly IBleService _bleService;

		public const uint AccessoryLinkKeySeedCypher = 2645682455u;

		private const byte AccessoryMessageIdMask = 128;

		private const byte AccessoryMessageLinkModeMask = 64;

		private const byte AccessoryMessageTypeMask = 63;

		private const ushort LippertCompanyIdentifier = 1479;

		private const int MaxDeviceStatusSize = 8;

		private const int MinManufacturerSpecificData = 17;

		private const int CompanyIdSize = 2;

		private const int MessageIdSize = 1;

		private const int MacAddressSize = 6;

		public const int SoftwarePartNumberSize = 6;

		public const int SoftwarePartNumberHyphenIndex = 5;

		private const int AccessoryIdManufacturerSpecificDataSize = 15;

		public const int MessageIdIndex = 2;

		public const int MacAddressIndex = 3;

		public const int SoftwarePartNumberIndex = 9;

		public const int EncryptedDataPrefixSize = 3;

		public const int EncryptedDataPostfixSize = 5;

		public const int EncryptedDataContainerSize = 8;

		public const int EncryptedDataStartIndex = 3;

		private const int DeviceStatusNonStatusDataCount = 17;

		private const int DeviceStatusStartIndex = 12;

		private const int ExtDeviceStatusNonStatusDataCount = 8;

		private const int ExtDeviceStatusStartIndex = 3;

		public const int MinIdsCanExtendedStatusLen = 1;

		public const int MaxIdsCanExtendedStatusLen = 8;

		private const int AlertByteMaxSize = 5;

		private const int AbridgedStatusMaxSize = 8;

		public const int MinPidStatusLen = 2;

		public const int MaxPidStatusLen = 26;

		private const int PidIndexMask = 240;

		private const int PidIndexShift = 4;

		private const int PidValueSizeMask = 7;

		private byte[] _lastMessage = Array.Empty<byte>();

		private byte[] _lastStatusRawData = Array.Empty<byte>();

		private byte[] _lastAbridgedStatusRawData = Array.Empty<byte>();

		private byte[] _lastIdsCanExtendedStatusRawData = Array.Empty<byte>();

		private byte[] _lastPidStatusRawData = Array.Empty<byte>();

		private byte[] _lastVersionInfoRawData = Array.Empty<byte>();

		private byte[] _lastIdsCanExtendedStatusWithEnhancedByteRawData = Array.Empty<byte>();

		private readonly IdsCanAccessoryLinkManager _linkManager;

		private static string LogTag => "IdsCanAccessoryScanResult";

		public IReadOnlyList<byte> LastStatusRawData => _lastStatusRawData;

		public static IAccessoryScanResultStatisticsTracker? IdsCanAccessoryScanResultStatisticsTracker { get; set; }

		public static IIdsCanAccessoryScanResultHistoryTracker? AccessoryScanResultHistoryTracker { get; set; }

		public override BleRequiredAdvertisements HasRequiredAdvertisements
		{
			get
			{
				if (!HasAccessoryId && !HasAccessoryStatus)
				{
					return BleRequiredAdvertisements.SomeExist;
				}
				return BleRequiredAdvertisements.AllExist;
			}
		}

		public bool IsInLinkMode
		{
			get
			{
				if (_lastMessage.Length > 2)
				{
					return (_lastMessage[2] & 0x40) != 0;
				}
				return false;
			}
		}

		public int LastMessageLength => _lastMessage.Length;

		public bool HasAccessoryId => AccessoryMacAddress != null;

		public bool HasAccessoryStatus => _lastStatusRawData.Length != 0;

		public bool HasAbridgedStatus => _lastAbridgedStatusRawData.Length != 0;

		public bool HasAccessoryIdsCanExtendedStatus => _lastIdsCanExtendedStatusRawData.Length != 0;

		public bool HasAccessoryIdsCanExtendedStatusEnhanced => _lastIdsCanExtendedStatusWithEnhancedByteRawData.Length != 0;

		public bool HasVersionInfo => _lastVersionInfoRawData.Length != 0;

		public DateTime AccessoryPidDataLastUpdated { get; private set; } = DateTime.MinValue;


		public DateTime AccessoryStatusDataLastUpdated { get; private set; } = DateTime.MinValue;


		public DateTime AccessoryAbridgedStatusDataLastUpdated { get; private set; } = DateTime.MinValue;


		public DateTime AccessoryDataLastUpdated
		{
			get
			{
				if (!(AccessoryStatusDataLastUpdated > AccessoryAbridgedStatusDataLastUpdated))
				{
					return AccessoryAbridgedStatusDataLastUpdated;
				}
				return AccessoryStatusDataLastUpdated;
			}
		}

		public MAC? AccessoryMacAddress { get; private set; }

		public string? SoftwarePartNumber { get; private set; }

		public float? MaxSecondsBetweenConsecutiveAdvertisements { get; private set; }

		public BleDeviceReachability Reachability
		{
			get
			{
				if (!MaxSecondsBetweenConsecutiveAdvertisements.HasValue)
				{
					return BleDeviceReachability.Unknown;
				}
				if ((DateTime.Now - AccessoryStatusDataLastUpdated).TotalSeconds <= (double)MaxSecondsBetweenConsecutiveAdvertisements.Value)
				{
					return BleDeviceReachability.Reachable;
				}
				return BleDeviceReachability.Unreachable;
			}
		}

		public IdsCanAccessoryScanResult(IBleService bleService, Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
			: base(deviceId, defaultDeviceName, rssi, advertisementRecords)
		{
			_bleService = bleService;
			_linkManager = new IdsCanAccessoryLinkManager(bleService);
		}

		protected override void RawManufacturerSpecificDataUpdated(byte[] manufacturerSpecificData)
		{
			base.RawManufacturerSpecificDataUpdated(manufacturerSpecificData);
			if (!manufacturerSpecificData.IsLciManufacturerSpecificData() || manufacturerSpecificData.Length < 3)
			{
				return;
			}
			_lastMessage = manufacturerSpecificData;
			byte b = manufacturerSpecificData[2];
			if ((b & 0x80) == 0)
			{
				return;
			}
			IdsCanAccessoryMessageType idsCanAccessoryMessageType = (IdsCanAccessoryMessageType)(b & 0x3F);
			AccessoryScanResultHistoryTracker?.TrackRawMessage(base.DeviceId, idsCanAccessoryMessageType, manufacturerSpecificData);
			switch (idsCanAccessoryMessageType)
			{
			case IdsCanAccessoryMessageType.AccessoryStatus:
				_lastStatusRawData = manufacturerSpecificData;
				AccessoryStatusDataLastUpdated = DateTime.Now;
				IdsCanAccessoryScanResultStatisticsTracker?.GetStatisticsForDeviceId(base.DeviceId).UpdateAccessoryStatus(manufacturerSpecificData, this);
				break;
			case IdsCanAccessoryMessageType.AccessoryId:
			{
				if (manufacturerSpecificData.Length != 15)
				{
					break;
				}
				AccessoryMacAddress = new MAC((UInt48)manufacturerSpecificData.GetValueUInt48(3));
				byte[] array = new byte[6];
				Buffer.BlockCopy(manufacturerSpecificData, 9, array, 0, 6);
				try
				{
					SoftwarePartNumber = Encoding.ASCII.GetString(array);
					if (SoftwarePartNumber!.Length == 6)
					{
						SoftwarePartNumber = SoftwarePartNumber!.Insert(5, "-");
					}
				}
				catch
				{
				}
				IdsCanAccessoryScanResultStatisticsTracker?.GetStatisticsForDeviceId(base.DeviceId).UpdateAccessoryId(manufacturerSpecificData, this);
				break;
			}
			case IdsCanAccessoryMessageType.IdsCanExtendedStatus:
				_lastIdsCanExtendedStatusRawData = manufacturerSpecificData;
				IdsCanAccessoryScanResultStatisticsTracker?.GetStatisticsForDeviceId(base.DeviceId).UpdateExtendedStatus(manufacturerSpecificData, this);
				break;
			case IdsCanAccessoryMessageType.AccessoryConfigStatus:
				_lastPidStatusRawData = manufacturerSpecificData;
				AccessoryPidDataLastUpdated = DateTime.Now;
				IdsCanAccessoryScanResultStatisticsTracker?.GetStatisticsForDeviceId(base.DeviceId).UpdateAccessoryConfigStatus(manufacturerSpecificData, this);
				break;
			case IdsCanAccessoryMessageType.AccessoryAbridgedStatus:
				_lastAbridgedStatusRawData = manufacturerSpecificData;
				AccessoryAbridgedStatusDataLastUpdated = DateTime.Now;
				IdsCanAccessoryScanResultStatisticsTracker?.GetStatisticsForDeviceId(base.DeviceId).UpdateAccessoryAbridgedStatus(manufacturerSpecificData, this);
				break;
			case IdsCanAccessoryMessageType.VersionInfo:
				_lastVersionInfoRawData = manufacturerSpecificData;
				IdsCanAccessoryScanResultStatisticsTracker?.GetStatisticsForDeviceId(base.DeviceId).UpdateAccessoryVersion(manufacturerSpecificData, this);
				break;
			case IdsCanAccessoryMessageType.ExtendedStatusWithEnhancedByte:
				_lastIdsCanExtendedStatusWithEnhancedByteRawData = manufacturerSpecificData;
				IdsCanAccessoryScanResultStatisticsTracker?.GetStatisticsForDeviceId(base.DeviceId).UpdateExtendedStatusWithEnhancedByte(manufacturerSpecificData, this);
				break;
			default:
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(47, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Accessory Scan Result Message Type Unknown 0x");
				defaultInterpolatedStringHandler.AppendFormatted(idsCanAccessoryMessageType, "X");
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(manufacturerSpecificData.DebugDump());
				TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				IdsCanAccessoryScanResultStatisticsTracker?.GetStatisticsForDeviceId(base.DeviceId).UpdateAccessoryInvalid(manufacturerSpecificData, this);
				break;
			}
			}
		}

		public void SetMac(MAC accessoryMac)
		{
			if (AccessoryMacAddress == null)
			{
				AccessoryMacAddress = accessoryMac;
			}
			else if (AccessoryMacAddress != accessoryMac)
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Trying to override existing mac of ");
				defaultInterpolatedStringHandler.AppendFormatted(AccessoryMacAddress);
				defaultInterpolatedStringHandler.AppendLiteral(" to ");
				defaultInterpolatedStringHandler.AppendFormatted(accessoryMac);
				defaultInterpolatedStringHandler.AppendLiteral(" not allowed");
				TaggedLog.Error(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
			}
		}

		private static byte[]? DecodeEncryptedRawData(MAC? macAddress, IReadOnlyList<byte>? rawData, int encryptedDataStartIndex)
		{
			if ((object)macAddress == null)
			{
				return null;
			}
			if (rawData == null || rawData!.Count == 0)
			{
				return Array.Empty<byte>();
			}
			try
			{
				byte[] buffer = new byte[rawData!.Count];
				rawData.ToExistingArray(buffer);
				new IdsCanAccessoryCrypto(macAddress).DecryptPacket(ref buffer, encryptedDataStartIndex);
				return buffer;
			}
			catch (Exception ex)
			{
				TaggedLog.Debug(LogTag, "Unable to decode accessory status: " + rawData.DebugDump(0, rawData!.Count) + " " + ex.Message);
				return Array.Empty<byte>();
			}
		}

		public static byte[]? DecodeEncryptedRawData(MAC macAddress, IReadOnlyList<byte> rawData)
		{
			return DecodeEncryptedRawData(macAddress, rawData, 3);
		}

		public static byte[]? GetDecodedDeviceStatus(MAC macAddress, IReadOnlyList<byte> rawData)
		{
			byte[] array = DecodeEncryptedRawData(macAddress, rawData);
			if (array == null)
			{
				return null;
			}
			try
			{
				int num = array.Length - 17;
				return new ArraySegment<byte>(array, 12, num).ToArray();
			}
			catch (Exception ex)
			{
				TaggedLog.Debug(LogTag, "Unable to decode device status: " + rawData.DebugDump(0, rawData.Count) + " " + ex.Message);
				return null;
			}
		}

		public static byte[]? GetDecodedExtendedDeviceStatus(MAC macAddress, IReadOnlyList<byte> rawData)
		{
			byte[] array = DecodeEncryptedRawData(macAddress, rawData);
			if (array == null)
			{
				return null;
			}
			try
			{
				int num = array.Length - 8;
				return new ArraySegment<byte>(array, 3, num).ToArray();
			}
			catch (Exception ex)
			{
				TaggedLog.Debug(LogTag, "Unable to decode extended device status: " + rawData.DebugDump(0, rawData.Count) + " " + ex.Message);
				return null;
			}
		}

		public IdsCanAccessoryStatus? GetAccessoryStatus(MAC macAddress)
		{
			byte[] array = DecodeEncryptedRawData(macAddress, _lastStatusRawData);
			int num = ((array != null) ? array.Length : 0);
			if (num < 17 || array == null)
			{
				return null;
			}
			int num2 = num - 17;
			if (num2 > 8)
			{
				return null;
			}
			int num3 = num - 4 - 1;
			if (array.GetValueUInt16(0, ArrayExtension.Endian.Little) != 1479)
			{
				return null;
			}
			if ((array[2] & 0x3F) != 1)
			{
				return null;
			}
			float num4 = (float)(int)array.GetValueUInt16(3) / 64f;
			PRODUCT_ID productId = array.GetValueUInt16(5);
			DEVICE_TYPE deviceType = array[7];
			FUNCTION_NAME functionName = array.GetValueUInt16(8);
			byte functionInstance = (byte)(array[10] & 0xFu);
			byte rawCapability = array[11];
			IReadOnlyList<byte> readOnlyList2;
			if (num2 > 0)
			{
				IReadOnlyList<byte> readOnlyList = new ArraySegment<byte>(array, 12, num2);
				readOnlyList2 = readOnlyList;
			}
			else
			{
				IReadOnlyList<byte> readOnlyList = Array.Empty<byte>();
				readOnlyList2 = readOnlyList;
			}
			IReadOnlyList<byte> status = readOnlyList2;
			byte crc = array[num3];
			uint valueUInt = array.GetValueUInt32(12);
			float? maxSecondsBetweenConsecutiveAdvertisements = MaxSecondsBetweenConsecutiveAdvertisements;
			float valueOrDefault = maxSecondsBetweenConsecutiveAdvertisements.GetValueOrDefault();
			if (!maxSecondsBetweenConsecutiveAdvertisements.HasValue)
			{
				valueOrDefault = num4;
				float? num6 = (MaxSecondsBetweenConsecutiveAdvertisements = valueOrDefault);
			}
			if (!FloatExtension.Equals(MaxSecondsBetweenConsecutiveAdvertisements.Value, num4, 0.01f))
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Scan Result Changing ");
				defaultInterpolatedStringHandler.AppendFormatted("MaxSecondsBetweenConsecutiveAdvertisements");
				defaultInterpolatedStringHandler.AppendLiteral(" from ");
				defaultInterpolatedStringHandler.AppendFormatted(MaxSecondsBetweenConsecutiveAdvertisements.Value);
				defaultInterpolatedStringHandler.AppendLiteral(" to ");
				defaultInterpolatedStringHandler.AppendFormatted(num4);
				defaultInterpolatedStringHandler.AppendLiteral(" which is unexpected");
				TaggedLog.Warning(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				MaxSecondsBetweenConsecutiveAdvertisements = num4;
			}
			return new IdsCanAccessoryStatus(macAddress, num4, productId, deviceType, functionName, functionInstance, rawCapability, status, crc, valueUInt);
		}

		public byte[]? GetAccessoryAbridgedStatus()
		{
			if (_lastAbridgedStatusRawData == null)
			{
				TaggedLog.Debug(LogTag, "Unable to GetAccessoryAbridgedStatus as too many bytes were returned. AbridgeStatus data is null.");
				return null;
			}
			int num = _lastAbridgedStatusRawData.Length - 3;
			if (num > 5 && num > 0)
			{
				string logTag = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(109, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Unable to GetAccessoryAbridgedStatus as too many bytes were returned. Max sixe: ");
				defaultInterpolatedStringHandler.AppendFormatted(5);
				defaultInterpolatedStringHandler.AppendLiteral(", Number of Bytes to Return: ");
				defaultInterpolatedStringHandler.AppendFormatted(num);
				TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
				return null;
			}
			return _lastAbridgedStatusRawData.ToNewArray(_lastAbridgedStatusRawData.Length - num, num);
		}

		public byte[]? GetAccessoryIdsCanExtendedStatus(MAC macAddress)
		{
			byte[] array = DecodeEncryptedRawData(macAddress, _lastIdsCanExtendedStatusRawData, 3);
			if (array == null)
			{
				return null;
			}
			int num = array.Length;
			if (num < 9)
			{
				return null;
			}
			if (num > 16)
			{
				return null;
			}
			int count = num - 8;
			if ((array[2] & 0x3F) != 3)
			{
				return null;
			}
			return array.ToNewArray(3, count);
		}

		public (byte?, byte[]?) GetAccessoryIdsCanExtendedStatusEnhanced(MAC macAddress)
		{
			byte[] array = DecodeEncryptedRawData(macAddress, _lastIdsCanExtendedStatusWithEnhancedByteRawData, 3);
			if (array == null)
			{
				return (null, null);
			}
			int num = array.Length;
			if (num < 9)
			{
				return (null, null);
			}
			if (num > 16)
			{
				return (null, null);
			}
			int count = num - 8;
			if ((array[2] & 0x3F) != 7)
			{
				return (null, null);
			}
			return (array[3], array.ToNewArray(4, count));
		}

		public IEnumerable<AccessoryPidStatus> GetAccessoryPidStatus(MAC macAddress)
		{
			DateTime accessoryPidDataLastUpdated = AccessoryPidDataLastUpdated;
			if (accessoryPidDataLastUpdated == DateTime.MinValue)
			{
				yield break;
			}
			byte[] lastPidStatusData = DecodeEncryptedRawData(macAddress, _lastPidStatusRawData);
			if (lastPidStatusData == null)
			{
				yield break;
			}
			int num = lastPidStatusData.Length;
			if (num < 10 || num > 34)
			{
				yield break;
			}
			int num2 = num - 8;
			int index = 3;
			int endIndex = index + num2;
			if ((lastPidStatusData[2] & 0x3F) != 4)
			{
				yield break;
			}
			while (index < endIndex)
			{
				byte b = lastPidStatusData[index++];
				Pid pid = (Pid)(345 + ((b & 0xF0) >> 4));
				UInt48 value = (byte)0;
				try
				{
					int num3 = b & 7;
					if (index + num3 > endIndex)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(69, 4);
						defaultInterpolatedStringHandler.AppendLiteral("Pid ");
						defaultInterpolatedStringHandler.AppendFormatted(pid);
						defaultInterpolatedStringHandler.AppendLiteral(" with size ");
						defaultInterpolatedStringHandler.AppendFormatted(num3);
						defaultInterpolatedStringHandler.AppendLiteral(" would run past end of buffer.  Pid value starts at ");
						defaultInterpolatedStringHandler.AppendFormatted(index);
						defaultInterpolatedStringHandler.AppendLiteral(": ");
						defaultInterpolatedStringHandler.AppendFormatted(lastPidStatusData.DebugDump());
						throw new IndexOutOfRangeException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					UInt48 uInt;
					switch (num3)
					{
					case 1:
						uInt = lastPidStatusData[index];
						break;
					case 2:
						uInt = lastPidStatusData.GetValueUInt16(index);
						break;
					case 3:
						uInt = (UInt48)((lastPidStatusData[index] << 16) | (lastPidStatusData[index + 1] << 8) | lastPidStatusData[index + 2]);
						break;
					case 4:
						uInt = lastPidStatusData.GetValueUInt32(index);
						break;
					case 5:
						uInt = (UInt48)(((ulong)lastPidStatusData[index] << 32) | ((ulong)lastPidStatusData[index + 1] << 24) | ((ulong)lastPidStatusData[index + 2] << 16) | ((ulong)lastPidStatusData[index + 3] << 8) | lastPidStatusData[index + 4]);
						break;
					case 6:
						uInt = (UInt48)lastPidStatusData.GetValueUInt48(index);
						break;
					default:
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(26, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Pid ");
						defaultInterpolatedStringHandler.AppendFormatted(pid);
						defaultInterpolatedStringHandler.AppendLiteral(" with size ");
						defaultInterpolatedStringHandler.AppendFormatted(num3);
						defaultInterpolatedStringHandler.AppendLiteral(" is invalid");
						throw new IndexOutOfRangeException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					}
					value = uInt;
					index += num3;
				}
				catch (Exception ex)
				{
					TaggedLog.Error(LogTag, "Unable to parse PID data " + lastPidStatusData.DebugDump() + ": " + ex.Message);
				}
				yield return new AccessoryPidStatus(pid, value, accessoryPidDataLastUpdated);
			}
		}

		public IdsCanAccessoryVersionInfo GetVersionInfo()
		{
			return new IdsCanAccessoryVersionInfo(_lastVersionInfoRawData);
		}

		public async Task<BleDeviceKeySeedExchangeResult> TryLinkVerificationAsync(bool requireLinkMode, CancellationToken cancellationToken, Plugin.BLE.Abstractions.Contracts.IDevice? device = null)
		{
			_ = 1;
			try
			{
				if (!HasAccessoryId || (!IsInLinkMode && requireLinkMode))
				{
					return BleDeviceKeySeedExchangeResult.Failed;
				}
				Plugin.BLE.Abstractions.Contracts.IDevice device2 = device;
				if (device2 == null)
				{
					device2 = await _bleService.Manager.TryConnectToDeviceAsync(base.DeviceId, cancellationToken);
				}
				using Plugin.BLE.Abstractions.Contracts.IDevice bleDevice = device2;
				if (bleDevice == null)
				{
					string logTag = LogTag;
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 1);
					defaultInterpolatedStringHandler.AppendLiteral("Unable to connect to accessory ");
					defaultInterpolatedStringHandler.AppendFormatted(base.DeviceId);
					TaggedLog.Debug(logTag, defaultInterpolatedStringHandler.ToStringAndClear());
					return BleDeviceKeySeedExchangeResult.Failed;
				}
				return await _linkManager.TryLinkVerificationAsync(bleDevice, 2645682455u, cancellationToken);
			}
			catch (Exception ex)
			{
				string logTag2 = LogTag;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(31, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Link Verification Failed for ");
				defaultInterpolatedStringHandler.AppendFormatted(base.DeviceId);
				defaultInterpolatedStringHandler.AppendLiteral(": ");
				defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
				TaggedLog.Debug(logTag2, defaultInterpolatedStringHandler.ToStringAndClear());
				return BleDeviceKeySeedExchangeResult.Failed;
			}
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(IsInLinkMode ? "LINK Mode" : "ACCESSORY Mode");
			if (HasAccessoryId)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder3 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, stringBuilder2);
				handler.AppendLiteral(" ");
				handler.AppendFormatted(AccessoryMacAddress);
				handler.AppendLiteral("/");
				handler.AppendFormatted(SoftwarePartNumber);
				stringBuilder3.Append(ref handler);
			}
			if (HasAccessoryStatus)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
				handler.AppendFormatted(Environment.NewLine);
				stringBuilder4.Append(ref handler);
				stringBuilder.Append(" Accessory Status: ");
				MAC accessoryMacAddress = AccessoryMacAddress;
				if ((object)accessoryMacAddress != null)
				{
					IdsCanAccessoryStatus? accessoryStatus = GetAccessoryStatus(accessoryMacAddress);
					if (accessoryStatus.HasValue)
					{
						IdsCanAccessoryStatus valueOrDefault = accessoryStatus.GetValueOrDefault();
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder5 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, stringBuilder2);
						handler.AppendFormatted(valueOrDefault);
						handler.AppendLiteral(": ");
						handler.AppendFormatted(_lastStatusRawData.DebugDump());
						stringBuilder5.Append(ref handler);
						goto IL_013e;
					}
				}
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder6 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
				handler.AppendFormatted(_lastStatusRawData.DebugDump());
				stringBuilder6.Append(ref handler);
			}
			goto IL_013e;
			IL_013e:
			if (HasAccessoryIdsCanExtendedStatusEnhanced)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder7 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
				handler.AppendFormatted(Environment.NewLine);
				stringBuilder7.Append(ref handler);
				stringBuilder.Append(" Extended Status with Enhanced Byte: ");
				MAC accessoryMacAddress2 = AccessoryMacAddress;
				if ((object)accessoryMacAddress2 != null)
				{
					(byte?, byte[]) accessoryIdsCanExtendedStatusEnhanced = GetAccessoryIdsCanExtendedStatusEnhanced(accessoryMacAddress2);
					var (b, _) = accessoryIdsCanExtendedStatusEnhanced;
					if (b.HasValue)
					{
						byte valueOrDefault2 = b.GetValueOrDefault();
						byte[] item = accessoryIdsCanExtendedStatusEnhanced.Item2;
						if (item != null)
						{
							stringBuilder2 = stringBuilder;
							StringBuilder stringBuilder8 = stringBuilder2;
							handler = new StringBuilder.AppendInterpolatedStringHandler(17, 3, stringBuilder2);
							handler.AppendLiteral("EnhancedByte: ");
							handler.AppendFormatted(valueOrDefault2);
							handler.AppendLiteral(" ");
							handler.AppendFormatted(item.DebugDump());
							handler.AppendLiteral(": ");
							handler.AppendFormatted(_lastIdsCanExtendedStatusWithEnhancedByteRawData.DebugDump());
							stringBuilder8.Append(ref handler);
							goto IL_0258;
						}
					}
				}
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder9 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
				handler.AppendFormatted(_lastIdsCanExtendedStatusWithEnhancedByteRawData.DebugDump());
				stringBuilder9.Append(ref handler);
			}
			goto IL_0258;
			IL_0258:
			if (HasAccessoryIdsCanExtendedStatus)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder10 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
				handler.AppendFormatted(Environment.NewLine);
				stringBuilder10.Append(ref handler);
				stringBuilder.Append(" Extended Status: ");
				MAC accessoryMacAddress3 = AccessoryMacAddress;
				if ((object)accessoryMacAddress3 != null)
				{
					byte[] accessoryIdsCanExtendedStatus = GetAccessoryIdsCanExtendedStatus(accessoryMacAddress3);
					if (accessoryIdsCanExtendedStatus != null)
					{
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder11 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, stringBuilder2);
						handler.AppendFormatted(accessoryIdsCanExtendedStatus.DebugDump());
						handler.AppendLiteral(": ");
						handler.AppendFormatted(_lastIdsCanExtendedStatusRawData.DebugDump());
						stringBuilder11.Append(ref handler);
						goto IL_0326;
					}
				}
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder12 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
				handler.AppendFormatted(_lastIdsCanExtendedStatusRawData.DebugDump());
				stringBuilder12.Append(ref handler);
			}
			goto IL_0326;
			IL_04ab:
			return stringBuilder.ToString();
			IL_0326:
			if (AccessoryPidDataLastUpdated != DateTime.MinValue)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder13 = stringBuilder2;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
				handler.AppendFormatted(Environment.NewLine);
				stringBuilder13.Append(ref handler);
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder14 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(43, 1, stringBuilder2);
				handler.AppendLiteral("  LAST RECEIVED PID Data (last updated ");
				handler.AppendFormatted((DateTime.Now - AccessoryPidDataLastUpdated).TotalSeconds);
				handler.AppendLiteral("s): ");
				stringBuilder14.Append(ref handler);
				MAC accessoryMacAddress4 = AccessoryMacAddress;
				if ((object)accessoryMacAddress4 != null)
				{
					IEnumerable<AccessoryPidStatus> accessoryPidStatus = GetAccessoryPidStatus(accessoryMacAddress4);
					if (accessoryPidStatus != null)
					{
						foreach (AccessoryPidStatus item2 in accessoryPidStatus)
						{
							stringBuilder2 = stringBuilder;
							StringBuilder stringBuilder15 = stringBuilder2;
							handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder2);
							handler.AppendFormatted(item2.Id);
							handler.AppendLiteral(": 0x");
							handler.AppendFormatted(item2.Value, "X");
							handler.AppendLiteral(" ");
							stringBuilder15.Append(ref handler);
						}
						stringBuilder2 = stringBuilder;
						StringBuilder stringBuilder16 = stringBuilder2;
						handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
						handler.AppendLiteral(" Raw PidData: ");
						handler.AppendFormatted(_lastPidStatusRawData.DebugDump());
						stringBuilder16.Append(ref handler);
						goto IL_04ab;
					}
				}
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder17 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, stringBuilder2);
				handler.AppendFormatted(_lastPidStatusRawData.DebugDump());
				stringBuilder17.Append(ref handler);
			}
			goto IL_04ab;
		}
	}
}
