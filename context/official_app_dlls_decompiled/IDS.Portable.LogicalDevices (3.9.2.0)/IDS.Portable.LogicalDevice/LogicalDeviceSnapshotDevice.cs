using System;
using System.Collections.Generic;
using System.Linq;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using IDS.Portable.LogicalDevice.LogicalDevice;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceSnapshotDevice : JsonSerializable<LogicalDeviceSnapshotDevice>, IComparable
	{
		[JsonProperty("Alerts", NullValueHandling = NullValueHandling.Ignore)]
		private Dictionary<string, LogicalDeviceAlert>? _alerts;

		[JsonProperty("DeviceTags")]
		private Dictionary<string, LogicalDeviceSnapshotDeviceTag> _deviceTags;

		[JsonProperty("Metadata", Required = Required.Default)]
		private Dictionary<string, object> _customSnapshotDataDict;

		[JsonProperty]
		public string Description { get; }

		[JsonProperty]
		[JsonConverter(typeof(LogicalDeviceIdConverter))]
		public ILogicalDeviceId LogicalId { get; }

		[JsonProperty]
		public byte DeviceCapabilityRawValue { get; }

		[JsonProperty]
		public DateTime? LastUpdatedTimestamp { get; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		[JsonConverter(typeof(ByteArrayJsonHexStringConverter))]
		public byte[]? StatusData { get; }

		public IReadOnlyDictionary<string, LogicalDeviceAlert>? Alerts => _alerts;

		public IReadOnlyDictionary<string, LogicalDeviceSnapshotDeviceTag> DeviceTags => _deviceTags;

		public LogicalDeviceSnapshotMetaDataReadOnly CustomSnapshotData => LogicalDeviceSnapshotMetaDataReadOnly.MakeFromDictionary(_customSnapshotDataDict);

		[JsonConstructor]
		public LogicalDeviceSnapshotDevice(string description, ILogicalDeviceId logicalId, byte deviceCapabilityRawValue, DateTime? lastUpdatedTimestamp, byte[]? statusData, Dictionary<string, LogicalDeviceAlert>? alerts, Dictionary<string, LogicalDeviceSnapshotDeviceTag> deviceTags, Dictionary<string, object> metaData)
		{
			Description = description ?? MakeDeviceDescription(logicalId);
			LogicalId = logicalId.Clone();
			DeviceCapabilityRawValue = deviceCapabilityRawValue;
			LastUpdatedTimestamp = lastUpdatedTimestamp;
			if (statusData == null)
			{
				StatusData = Array.Empty<byte>();
			}
			else
			{
				int num = statusData!.Length;
				StatusData = new byte[num];
				if (num > 0)
				{
					Buffer.BlockCopy(statusData, 0, StatusData, 0, num);
				}
			}
			_alerts = alerts;
			_deviceTags = deviceTags;
			_customSnapshotDataDict = metaData ?? new Dictionary<string, object>();
		}

		public LogicalDeviceSnapshotDevice(ILogicalDevice logicalDevice)
			: this(MakeDeviceDescription(logicalDevice.LogicalId), logicalDevice.LogicalId, logicalDevice.DeviceCapabilityBasic.GetRawValue(), GetLastUpdatedTimestamp(logicalDevice), CopyRawDeviceStatus(logicalDevice), MakeAlertsDict(logicalDevice), MakeSnapshotTags(logicalDevice), logicalDevice.CustomSnapshotData.ToDictionary())
		{
		}

		public static Dictionary<string, LogicalDeviceAlert>? MakeAlertsDict(ILogicalDevice logicalDevice)
		{
			if (!(logicalDevice is ILogicalDeviceWithStatusAlerts logicalDeviceWithStatusAlerts))
			{
				return null;
			}
			return Enumerable.ToDictionary(Enumerable.OfType<LogicalDeviceAlert>(logicalDeviceWithStatusAlerts.Alerts), (LogicalDeviceAlert alert) => alert.Name, (LogicalDeviceAlert alert) => alert._003CClone_003E_0024());
		}

		public static string MakeDeviceDescription(ILogicalDeviceId logicalId)
		{
			try
			{
				return logicalId.ToString(LogicalDeviceIdFormat.FunctionNameFull) + " (" + logicalId.DeviceType.Name + ", " + logicalId.ProductId.Name + ")";
			}
			catch (Exception)
			{
				return "Unknown";
			}
		}

		public static DateTime? GetLastUpdatedTimestamp(ILogicalDevice logicalDevice)
		{
			if (!(logicalDevice is ILogicalDeviceWithStatus logicalDeviceWithStatus))
			{
				return null;
			}
			return logicalDeviceWithStatus.LastUpdatedTimestamp;
		}

		public static byte[]? CopyRawDeviceStatus(ILogicalDevice logicalDevice)
		{
			if (!(logicalDevice is ILogicalDeviceWithStatus logicalDeviceWithStatus))
			{
				return null;
			}
			return logicalDeviceWithStatus.RawDeviceStatus.CopyCurrentData();
		}

		public static Dictionary<string, LogicalDeviceSnapshotDeviceTag> MakeSnapshotTags(ILogicalDevice logicalDevice)
		{
			Dictionary<string, LogicalDeviceSnapshotDeviceTag> dictionary = new Dictionary<string, LogicalDeviceSnapshotDeviceTag>();
			foreach (KeyValuePair<string, object> customDatum in logicalDevice.CustomData)
			{
				if (customDatum.Value is List<ILogicalDeviceTag> tagList)
				{
					dictionary[customDatum.Key] = new LogicalDeviceSnapshotDeviceTag(tagList);
				}
			}
			return dictionary;
		}

		public bool HasSameDevices(LogicalDeviceSnapshotDevice other, bool withSameStatusData)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			if (!LogicalId.Equals(other.LogicalId))
			{
				return false;
			}
			if (DeviceCapabilityRawValue != other.DeviceCapabilityRawValue)
			{
				return false;
			}
			if (DeviceTags.Count != other.DeviceTags.Count)
			{
				return false;
			}
			foreach (KeyValuePair<string, LogicalDeviceSnapshotDeviceTag> deviceTag in DeviceTags)
			{
				LogicalDeviceSnapshotDeviceTag value = deviceTag.Value;
				LogicalDeviceSnapshotDeviceTag logicalDeviceSnapshotDeviceTag = other.DeviceTags.TryGetValue(deviceTag.Key);
				if (logicalDeviceSnapshotDeviceTag == null)
				{
					return false;
				}
				if (!value.HasSameTags(logicalDeviceSnapshotDeviceTag))
				{
					return false;
				}
			}
			if (Alerts != other.Alerts)
			{
				if (Alerts == null || other.Alerts == null)
				{
					return false;
				}
				if (Alerts!.Count != other.Alerts!.Count)
				{
					return false;
				}
				foreach (KeyValuePair<string, LogicalDeviceAlert> item in Alerts!)
				{
					if (!other.Alerts!.TryGetValue(item.Key, out var other2))
					{
						return false;
					}
					if (!item.Value.Equals(other2))
					{
						return false;
					}
				}
			}
			if (_customSnapshotDataDict.Count != other._customSnapshotDataDict.Count)
			{
				return false;
			}
			LogicalDeviceSnapshotMetaDataReadOnly logicalDeviceSnapshotMetaDataReadOnly = LogicalDeviceSnapshotMetaDataReadOnly.MakeFromDictionary(_customSnapshotDataDict);
			LogicalDeviceSnapshotMetaDataReadOnly other3 = LogicalDeviceSnapshotMetaDataReadOnly.MakeFromDictionary(other._customSnapshotDataDict);
			if (!logicalDeviceSnapshotMetaDataReadOnly.Equals(other3))
			{
				return false;
			}
			if (withSameStatusData && !Enumerable.SequenceEqual(StatusData, other.StatusData))
			{
				return false;
			}
			return true;
		}

		public int CompareTo(object obj)
		{
			if (obj == null || !(obj is LogicalDeviceSnapshotDevice logicalDeviceSnapshotDevice))
			{
				return 1;
			}
			return LogicalId.CompareTo(logicalDeviceSnapshotDevice.LogicalId);
		}
	}
}
