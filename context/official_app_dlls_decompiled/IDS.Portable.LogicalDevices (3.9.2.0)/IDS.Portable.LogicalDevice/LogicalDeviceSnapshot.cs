using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IDS.Core;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceSnapshot : JsonSerializable<LogicalDeviceSnapshot>
	{
		[JsonProperty]
		public readonly ReadOnlyCollection<LogicalDeviceSnapshotDevice> Devices;

		[JsonProperty]
		[JsonConverter(typeof(VersionConverter))]
		public Version SnapshotFormat { get; }

		[JsonProperty]
		public DateTime Timestamp { get; }

		[JsonProperty]
		public uint? FunctionNameCrc { get; }

		[JsonConstructor]
		public LogicalDeviceSnapshot(Version snapshotFormat, DateTime timestamp, uint? functionNameCrc, ReadOnlyCollection<LogicalDeviceSnapshotDevice> devices)
		{
			SnapshotFormat = snapshotFormat;
			Timestamp = timestamp;
			Devices = devices;
			FunctionNameCrc = functionNameCrc;
		}

		public LogicalDeviceSnapshot(IEnumerable<ILogicalDevice> logicalDevices)
		{
			SnapshotFormat = new Version(2, 0);
			Timestamp = DateTime.Now;
			List<LogicalDeviceSnapshotDevice> list = new List<LogicalDeviceSnapshotDevice>();
			foreach (ILogicalDevice logicalDevice in logicalDevices)
			{
				list.Add(new LogicalDeviceSnapshotDevice(logicalDevice));
			}
			list.Sort();
			FunctionNameCrc = MakeFunctionNameCrc(list);
			Devices = new ReadOnlyCollection<LogicalDeviceSnapshotDevice>(list);
		}

		public bool HasSameDevices(LogicalDeviceSnapshot other, bool withSameStatusData)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			if (Devices.Count != other.Devices.Count)
			{
				return false;
			}
			if (FunctionNameCrc != other.FunctionNameCrc)
			{
				return false;
			}
			for (int i = 0; i < Devices.Count; i++)
			{
				if (!Devices[i].HasSameDevices(other.Devices[i], withSameStatusData))
				{
					return false;
				}
			}
			return true;
		}

		private uint MakeFunctionNameCrc(IReadOnlyList<LogicalDeviceSnapshotDevice> deviceList)
		{
			byte[] buffer = new byte[2];
			CRC32 cRC = new CRC32();
			foreach (LogicalDeviceSnapshotDevice device in deviceList)
			{
				buffer.SetValueUInt16(device.LogicalId.FunctionName, 0);
				cRC.Update(buffer);
				buffer.SetValueUInt16((ushort)device.LogicalId.FunctionInstance, 0);
				cRC.Update(buffer);
			}
			return cRC.Value;
		}
	}
}
