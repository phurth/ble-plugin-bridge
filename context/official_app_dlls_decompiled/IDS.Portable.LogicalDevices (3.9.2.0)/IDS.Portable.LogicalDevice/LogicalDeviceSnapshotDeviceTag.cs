using System;
using System.Collections.Generic;
using IDS.Portable.Common;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceSnapshotDeviceTag
	{
		[JsonProperty("Tags", NullValueHandling = NullValueHandling.Ignore)]
		[JsonConverter(typeof(LogicalDeviceSnapshotTagListConverter))]
		public List<ILogicalDeviceSnapshotTag> Tags;

		[JsonConstructor]
		public LogicalDeviceSnapshotDeviceTag(List<ILogicalDeviceSnapshotTag> tagList)
		{
			Tags = new List<ILogicalDeviceSnapshotTag>();
			if (tagList == null)
			{
				return;
			}
			foreach (ILogicalDeviceSnapshotTag tag in tagList)
			{
				if (tag != null && !Tags.Contains(tag))
				{
					Tags.Add(tag);
				}
			}
		}

		public LogicalDeviceSnapshotDeviceTag(List<ILogicalDeviceTag> tagList)
		{
			Tags = new List<ILogicalDeviceSnapshotTag>();
			if (tagList == null)
			{
				return;
			}
			foreach (ILogicalDeviceTag tag in tagList)
			{
				if (tag is ILogicalDeviceSnapshotTag logicalDeviceSnapshotTag && !Tags.Contains(logicalDeviceSnapshotTag))
				{
					Tags.Add(logicalDeviceSnapshotTag);
				}
			}
		}

		public bool HasSameTags(LogicalDeviceSnapshotDeviceTag other)
		{
			if (other == null)
			{
				return false;
			}
			if (this == other)
			{
				return true;
			}
			HashSet<ILogicalDeviceSnapshotTag> hashSet = new HashSet<ILogicalDeviceSnapshotTag>(Tags);
			HashSet<ILogicalDeviceSnapshotTag> hashSet2 = new HashSet<ILogicalDeviceSnapshotTag>(other.Tags);
			if (!Collection.HashSetEquals(hashSet, hashSet2))
			{
				return false;
			}
			return true;
		}

		public void AddTagsToLogicalDevice(ILogicalDevice logicalDevice, string tagKey)
		{
			if (Tags == null || Tags.Count == 0)
			{
				return;
			}
			List<ILogicalDeviceTag> list = logicalDevice.CustomData.TryGetValue(tagKey) as List<ILogicalDeviceTag>;
			if (list == null)
			{
				list = new List<ILogicalDeviceTag>();
				logicalDevice.CustomData[tagKey] = list;
			}
			foreach (ILogicalDeviceSnapshotTag tag in Tags)
			{
				if (tag is ILogicalDeviceSnapshotTagWithOptions logicalDeviceSnapshotTagWithOptions && logicalDeviceSnapshotTagWithOptions.DeserializeTagOption.HasFlag(LogicalDeviceSnapshotDeserializeTagOption.OverwriteExistingMatchingClass))
				{
					Type lookingForTagType = tag.GetType();
					list.Remove((ILogicalDeviceTag item) => item.GetType() == lookingForTagType);
				}
				if (!list.Contains(tag))
				{
					list.Add(tag);
				}
			}
		}
	}
}
