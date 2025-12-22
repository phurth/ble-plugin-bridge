using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceSnapshotMetaData
	{
		public string? CustomDeviceName;

		public string? CustomDeviceNameShort;

		public string? CustomDeviceNameShortAbbreviated;

		public readonly ConcurrentDictionary<string, LogicalDeviceSourceSnapshot> DeviceSourceSnapshotDict = new ConcurrentDictionary<string, LogicalDeviceSourceSnapshot>();

		public readonly ConcurrentDictionary<Pid, LogicalDevicePidSnapshot> PidValueSnapshotDict = new ConcurrentDictionary<Pid, LogicalDevicePidSnapshot>();

		public string? SoftwarePartNumber { get; set; }

		public Version? ProtocolVersion { get; set; }

		public LogicalDeviceSnapshotMetaDataReadOnly ToReadOnly()
		{
			return new LogicalDeviceSnapshotMetaDataReadOnly(SoftwarePartNumber, ProtocolVersion, DeviceSourceSnapshotDict.Values, PidValueSnapshotDict.Values, CustomDeviceName, CustomDeviceNameShort, CustomDeviceNameShortAbbreviated);
		}

		public void UpdateWithNewMetaData(LogicalDeviceSnapshotMetaDataReadOnly metaData)
		{
			if (SoftwarePartNumber == null && metaData.SoftwarePartNumber != null)
			{
				SoftwarePartNumber = metaData.SoftwarePartNumber;
			}
			if ((object)ProtocolVersion == null && (object)metaData.ProtocolVersion != null)
			{
				ProtocolVersion = metaData.ProtocolVersion;
			}
			if (CustomDeviceName == null && metaData.CustomDeviceName != null)
			{
				CustomDeviceName = metaData.CustomDeviceName;
			}
			if (CustomDeviceNameShort == null && metaData.CustomDeviceNameShort != null)
			{
				CustomDeviceName = metaData.CustomDeviceNameShort;
			}
			if (CustomDeviceNameShortAbbreviated == null && metaData.CustomDeviceNameShortAbbreviated != null)
			{
				CustomDeviceName = metaData.CustomDeviceNameShortAbbreviated;
			}
			foreach (KeyValuePair<string, LogicalDeviceSourceSnapshot> item in metaData.DeviceSourceSnapshotDict)
			{
				if (!DeviceSourceSnapshotDict.ContainsKey(item.Key))
				{
					DeviceSourceSnapshotDict[item.Key] = item.Value;
				}
			}
			foreach (KeyValuePair<Pid, LogicalDevicePidSnapshot> item2 in metaData.PidValueSnapshotDict)
			{
				if (!PidValueSnapshotDict.ContainsKey(item2.Key))
				{
					PidValueSnapshotDict[item2.Key] = item2.Value;
				}
			}
		}

		public static implicit operator LogicalDeviceSnapshotMetaDataReadOnly(LogicalDeviceSnapshotMetaData metaData)
		{
			return metaData.ToReadOnly();
		}

		public void AddDeviceSource(ILogicalDeviceSource deviceSource)
		{
			DeviceSourceSnapshotDict[deviceSource.DeviceSourceToken] = new LogicalDeviceSourceSnapshot(deviceSource.DeviceSourceToken, deviceSource.GetType().Name);
		}

		public bool RemoveDeviceSourceToken(string deviceSourceToken)
		{
			if (!HasDeviceSourceToken(deviceSourceToken))
			{
				return false;
			}
			DeviceSourceSnapshotDict.TryRemove(deviceSourceToken);
			return true;
		}

		public void AddOrUpdatePidValue(Pid pid, UInt48 value)
		{
			PidValueSnapshotDict[pid] = new LogicalDevicePidSnapshot(pid, value);
		}

		public bool HasDeviceSourceToken(string deviceSourceToken)
		{
			return DeviceSourceSnapshotDict.ContainsKey(deviceSourceToken);
		}
	}
}
