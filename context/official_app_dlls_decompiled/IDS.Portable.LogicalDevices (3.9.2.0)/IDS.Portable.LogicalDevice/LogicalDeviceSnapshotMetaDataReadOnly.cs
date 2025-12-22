using System;
using System.Collections.Generic;
using System.Linq;
using IDS.Portable.Common;
using Newtonsoft.Json.Linq;

namespace IDS.Portable.LogicalDevice
{
	public readonly struct LogicalDeviceSnapshotMetaDataReadOnly : IEquatable<LogicalDeviceSnapshotMetaDataReadOnly>
	{
		private static readonly IReadOnlyDictionary<string, LogicalDeviceSourceSnapshot> _emptyDeviceSourceSnapshotDict = new Dictionary<string, LogicalDeviceSourceSnapshot>();

		private static readonly IReadOnlyDictionary<Pid, LogicalDevicePidSnapshot> _emptyPidValueSnapshotDict = new Dictionary<Pid, LogicalDevicePidSnapshot>();

		private const string LogTag = "LogicalDeviceSnapshotMetaDataReadOnly";

		private const string SoftwarePartNumberKey = "SoftwarePartNumber";

		private const string ProtocolVersionKey = "ProtocolVersion";

		private const string DeviceSourcesKey = "DeviceSourceDataList";

		private const string PidValuesKey = "PidValuesList";

		private const string CustomDeviceNameKey = "CustomDeviceName";

		private const string CustomDeviceNameShortKey = "CustomDeviceNameShort";

		private const string CustomDeviceNameShortAbbreviatedKey = "CustomDeviceNameShortAbbreviated";

		private readonly Dictionary<string, LogicalDeviceSourceSnapshot>? _deviceSourceSnapshotDict;

		private readonly Dictionary<Pid, LogicalDevicePidSnapshot>? _pidValueSnapshotDict;

		public string? SoftwarePartNumber { get; }

		public Version? ProtocolVersion { get; }

		public string? CustomDeviceName { get; }

		public string? CustomDeviceNameShort { get; }

		public string? CustomDeviceNameShortAbbreviated { get; }

		public IReadOnlyDictionary<string, LogicalDeviceSourceSnapshot> DeviceSourceSnapshotDict
		{
			get
			{
				IReadOnlyDictionary<string, LogicalDeviceSourceSnapshot> deviceSourceSnapshotDict = _deviceSourceSnapshotDict;
				return deviceSourceSnapshotDict ?? _emptyDeviceSourceSnapshotDict;
			}
		}

		public IReadOnlyDictionary<Pid, LogicalDevicePidSnapshot> PidValueSnapshotDict
		{
			get
			{
				IReadOnlyDictionary<Pid, LogicalDevicePidSnapshot> pidValueSnapshotDict = _pidValueSnapshotDict;
				return pidValueSnapshotDict ?? _emptyPidValueSnapshotDict;
			}
		}

		public LogicalDeviceSnapshotMetaDataReadOnly(string? softwarePartNumber, Version? protocolVersion, IEnumerable<LogicalDeviceSourceSnapshot>? deviceSourceList, IEnumerable<LogicalDevicePidSnapshot>? pidValueSnapshotList, string? customDeviceName, string? customDeviceNameShort, string? customDeviceNameShortAbbreviated)
		{
			SoftwarePartNumber = softwarePartNumber;
			ProtocolVersion = protocolVersion;
			CustomDeviceName = customDeviceName;
			CustomDeviceNameShort = customDeviceNameShort;
			CustomDeviceNameShortAbbreviated = customDeviceNameShortAbbreviated;
			_deviceSourceSnapshotDict = new Dictionary<string, LogicalDeviceSourceSnapshot>();
			if (deviceSourceList != null)
			{
				foreach (LogicalDeviceSourceSnapshot item in deviceSourceList!)
				{
					_deviceSourceSnapshotDict![item.Token] = item;
				}
			}
			_pidValueSnapshotDict = new Dictionary<Pid, LogicalDevicePidSnapshot>();
			if (pidValueSnapshotList == null)
			{
				return;
			}
			foreach (LogicalDevicePidSnapshot item2 in pidValueSnapshotList!)
			{
				_pidValueSnapshotDict![item2.Id] = item2;
			}
		}

		public Dictionary<string, object> ToDictionary()
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			if (SoftwarePartNumber != null)
			{
				dictionary["SoftwarePartNumber"] = SoftwarePartNumber;
			}
			if ((object)ProtocolVersion != null)
			{
				dictionary["ProtocolVersion"] = ProtocolVersion!.ToString();
			}
			if (CustomDeviceName != null)
			{
				dictionary["CustomDeviceName"] = CustomDeviceName;
			}
			if (CustomDeviceNameShort != null)
			{
				dictionary["CustomDeviceNameShort"] = CustomDeviceNameShort;
			}
			if (CustomDeviceNameShortAbbreviated != null)
			{
				dictionary["CustomDeviceNameShortAbbreviated"] = CustomDeviceNameShortAbbreviated;
			}
			if (DeviceSourceSnapshotDict != null && DeviceSourceSnapshotDict.Count > 0)
			{
				dictionary["DeviceSourceDataList"] = new List<LogicalDeviceSourceSnapshot>(DeviceSourceSnapshotDict.Values);
			}
			if (PidValueSnapshotDict != null && PidValueSnapshotDict.Count > 0)
			{
				dictionary["PidValuesList"] = new List<LogicalDevicePidSnapshot>(PidValueSnapshotDict.Values);
			}
			return dictionary;
		}

		public static LogicalDeviceSnapshotMetaDataReadOnly MakeFromDictionary(Dictionary<string, object>? metaDataDictionary)
		{
			string softwarePartNumber = null;
			Version protocolVersion = null;
			string customDeviceName = null;
			string customDeviceNameShort = null;
			string customDeviceNameShortAbbreviated = null;
			HashSet<LogicalDeviceSourceSnapshot> hashSet = null;
			HashSet<LogicalDevicePidSnapshot> hashSet2 = null;
			if (metaDataDictionary == null)
			{
				return default(LogicalDeviceSnapshotMetaDataReadOnly);
			}
			if (metaDataDictionary!.TryGetValue("SoftwarePartNumber", out var value) && value is string text)
			{
				softwarePartNumber = text;
			}
			if (metaDataDictionary!.TryGetValue("ProtocolVersion", out var value2) && value2 is string text2)
			{
				try
				{
					protocolVersion = new Version(text2);
				}
				catch
				{
					TaggedLog.Warning("LogicalDeviceSnapshotMetaDataReadOnly", "Ignoring invalid version string " + text2);
				}
			}
			if (metaDataDictionary!.TryGetValue("CustomDeviceName", out var value3) && value3 is string text3)
			{
				customDeviceName = text3;
			}
			if (metaDataDictionary!.TryGetValue("CustomDeviceNameShort", out var value4) && value4 is string text4)
			{
				customDeviceNameShort = text4;
			}
			if (metaDataDictionary!.TryGetValue("CustomDeviceNameShortAbbreviated", out var value5) && value5 is string text5)
			{
				customDeviceNameShortAbbreviated = text5;
			}
			if (metaDataDictionary!.TryGetValue("DeviceSourceDataList", out var value6))
			{
				IEnumerable<LogicalDeviceSourceSnapshot> enumerable2;
				if (!(value6 is JArray source))
				{
					enumerable2 = ((!(value6 is IEnumerable<LogicalDeviceSourceSnapshot> enumerable)) ? null : enumerable);
				}
				else
				{
					try
					{
						enumerable2 = Enumerable.Select(source, (JToken jItem) => jItem.ToObject<LogicalDeviceSourceSnapshot>());
					}
					catch (Exception ex)
					{
						TaggedLog.Warning("LogicalDeviceSnapshotMetaDataReadOnly", "Unable to parse Device Source Data from Custom Metadata: " + ex.Message);
						enumerable2 = null;
					}
				}
				if (enumerable2 != null)
				{
					hashSet = new HashSet<LogicalDeviceSourceSnapshot>();
					foreach (LogicalDeviceSourceSnapshot item in enumerable2)
					{
						hashSet.Add(item);
					}
				}
			}
			if (metaDataDictionary!.TryGetValue("PidValuesList", out var value7))
			{
				IEnumerable<LogicalDevicePidSnapshot> enumerable4;
				if (!(value7 is JArray source2))
				{
					enumerable4 = ((!(value7 is IEnumerable<LogicalDevicePidSnapshot> enumerable3)) ? null : enumerable3);
				}
				else
				{
					try
					{
						enumerable4 = Enumerable.Select(source2, (JToken jItem) => jItem.ToObject<LogicalDevicePidSnapshot>());
					}
					catch (Exception ex2)
					{
						TaggedLog.Warning("LogicalDeviceSnapshotMetaDataReadOnly", "Unable to parse Pid Value Data from Custom Metadata: " + ex2.Message);
						enumerable4 = null;
					}
				}
				if (enumerable4 != null)
				{
					hashSet2 = new HashSet<LogicalDevicePidSnapshot>();
					foreach (LogicalDevicePidSnapshot item2 in enumerable4)
					{
						hashSet2.Add(item2);
					}
				}
			}
			return new LogicalDeviceSnapshotMetaDataReadOnly(softwarePartNumber, protocolVersion, hashSet, hashSet2, customDeviceName, customDeviceNameShort, customDeviceNameShortAbbreviated);
		}

		public bool Equals(LogicalDeviceSnapshotMetaDataReadOnly other)
		{
			if (!string.Equals(SoftwarePartNumber, other.SoftwarePartNumber))
			{
				return false;
			}
			if (!object.Equals(ProtocolVersion, other.ProtocolVersion))
			{
				return false;
			}
			if (!string.Equals(CustomDeviceName, other.CustomDeviceName))
			{
				return false;
			}
			if (!string.Equals(CustomDeviceNameShort, other.CustomDeviceNameShort))
			{
				return false;
			}
			if (!string.Equals(CustomDeviceNameShortAbbreviated, other.CustomDeviceNameShortAbbreviated))
			{
				return false;
			}
			if (DeviceSourceSnapshotDict != other.DeviceSourceSnapshotDict)
			{
				if (DeviceSourceSnapshotDict == null || other.DeviceSourceSnapshotDict == null)
				{
					return false;
				}
				if (DeviceSourceSnapshotDict.Count != other.DeviceSourceSnapshotDict.Count)
				{
					return false;
				}
				foreach (KeyValuePair<string, LogicalDeviceSourceSnapshot> item in DeviceSourceSnapshotDict)
				{
					if (!other.DeviceSourceSnapshotDict.TryGetValue(item.Key, out var other2))
					{
						return false;
					}
					if (!item.Value.Equals(other2))
					{
						return false;
					}
				}
			}
			if (PidValueSnapshotDict != other.PidValueSnapshotDict)
			{
				if (PidValueSnapshotDict == null || other.PidValueSnapshotDict == null)
				{
					return false;
				}
				if (PidValueSnapshotDict.Count != other.PidValueSnapshotDict.Count)
				{
					return false;
				}
				foreach (KeyValuePair<Pid, LogicalDevicePidSnapshot> item2 in PidValueSnapshotDict)
				{
					if (!other.PidValueSnapshotDict.TryGetValue(item2.Key, out var other3))
					{
						return false;
					}
					if (!item2.Value.Equals(other3))
					{
						return false;
					}
				}
			}
			return true;
		}

		public override string ToString()
		{
			List<string> list = new List<string>();
			if (SoftwarePartNumber != null)
			{
				list.Add(SoftwarePartNumber ?? "");
			}
			if ((object)ProtocolVersion != null)
			{
				list.Add($"{ProtocolVersion}");
			}
			if (CustomDeviceName != null)
			{
				list.Add(CustomDeviceName ?? "");
			}
			if (CustomDeviceNameShort != null)
			{
				list.Add(CustomDeviceNameShort ?? "");
			}
			if (CustomDeviceNameShortAbbreviated != null)
			{
				list.Add(CustomDeviceNameShortAbbreviated ?? "");
			}
			if (Enumerable.Any(DeviceSourceSnapshotDict))
			{
				string text = string.Join(", ", Enumerable.Select(DeviceSourceSnapshotDict.Values, (LogicalDeviceSourceSnapshot deviceSourceSnapShot) => deviceSourceSnapShot.Token + ":" + deviceSourceSnapShot.Name));
				list.Add("[" + text + "]");
			}
			if (Enumerable.Any(PidValueSnapshotDict))
			{
				string text2 = string.Join(", ", Enumerable.Select(PidValueSnapshotDict.Values, (LogicalDevicePidSnapshot pidValueSnapShot) => $"{pidValueSnapShot.Id}:0x{pidValueSnapShot.Value:X}"));
				list.Add("[" + text2 + "]");
			}
			if (list.Count == 0)
			{
				return "NONE";
			}
			return string.Join(", ", list);
		}
	}
}
