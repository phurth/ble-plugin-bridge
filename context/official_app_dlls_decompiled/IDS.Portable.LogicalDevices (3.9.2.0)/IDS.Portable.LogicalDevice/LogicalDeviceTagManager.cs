using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicData;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceTagManager : ILogicalDeviceTagManager
	{
		private readonly SourceCache<(ILogicalDevice device, ILogicalDeviceTag tag), string> _tags = new SourceCache<(ILogicalDevice, ILogicalDeviceTag), string>(((ILogicalDevice device, ILogicalDeviceTag tag) values) => values.device.ImmutableUniqueId.ToString() + values.tag.GetHashCode());

		public string TagKey { get; }

		public LogicalDeviceTagManager(string tagKey)
		{
			TagKey = tagKey;
			base._002Ector();
		}

		public void AddTag(ILogicalDeviceTag? tag, ILogicalDevice? logicalDevice)
		{
			if (tag == null || (logicalDevice?.IsDisposed ?? true))
			{
				return;
			}
			_tags.AddOrUpdate((logicalDevice, tag));
			lock (logicalDevice!.CustomData)
			{
				List<ILogicalDeviceTag> list = logicalDevice!.CustomData.TryGetValue(TagKey) as List<ILogicalDeviceTag>;
				if (list == null)
				{
					list = new List<ILogicalDeviceTag>();
					logicalDevice!.CustomData[TagKey] = list;
				}
				if (!list.Contains(tag))
				{
					list.Add(tag);
					logicalDevice!.DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
				}
			}
		}

		public void AddTags(IEnumerable<ILogicalDeviceTag>? tags, ILogicalDevice? logicalDevice)
		{
			if (tags == null || logicalDevice == null)
			{
				return;
			}
			foreach (ILogicalDeviceTag item in tags!)
			{
				_tags.AddOrUpdate((logicalDevice, item));
			}
			lock (logicalDevice!.CustomData)
			{
				foreach (ILogicalDeviceTag item2 in tags!)
				{
					AddTag(item2, logicalDevice);
				}
			}
		}

		public void RemoveTag(ILogicalDeviceTag? tag, ILogicalDevice? logicalDevice)
		{
			if (tag == null || logicalDevice == null)
			{
				return;
			}
			_tags.Remove((logicalDevice, tag));
			if (logicalDevice!.IsDisposed)
			{
				return;
			}
			lock (logicalDevice!.CustomData)
			{
				List<ILogicalDeviceTag> list = (logicalDevice!.CustomData.TryGetValue(TagKey) as List<ILogicalDeviceTag>) ?? new List<ILogicalDeviceTag>();
				if (list.Contains(tag))
				{
					list.Remove(tag);
					logicalDevice!.DeviceService.DeviceManager?.ContainerDataSourceSync(batchRequest: true);
				}
			}
		}

		public bool ContainsTag(ILogicalDeviceTag? tag, ILogicalDevice? logicalDevice)
		{
			if (tag == null || !((!(logicalDevice?.IsDisposed)) ?? false))
			{
				return false;
			}
			lock (logicalDevice!.CustomData)
			{
				if (logicalDevice!.CustomData.TryGetValue(TagKey) is List<ILogicalDeviceTag> list)
				{
					return list.Contains(tag);
				}
				return false;
			}
		}

		public bool ContainsAllTags(IEnumerable<ILogicalDeviceTag>? tags, ILogicalDevice? logicalDevice)
		{
			if (tags == null || !((!(logicalDevice?.IsDisposed)) ?? false))
			{
				return false;
			}
			lock (logicalDevice!.CustomData)
			{
				object obj = logicalDevice!.CustomData.TryGetValue(TagKey);
				List<ILogicalDeviceTag> tagList = obj as List<ILogicalDeviceTag>;
				return tagList != null && Enumerable.All(tags, (ILogicalDeviceTag tag) => tagList.Contains(tag));
			}
		}

		public bool ContainsAnyMatchingTag(IEnumerable<ILogicalDeviceTag>? tags, ILogicalDevice? logicalDevice)
		{
			if (tags == null || !((!(logicalDevice?.IsDisposed)) ?? false))
			{
				return false;
			}
			lock (logicalDevice!.CustomData)
			{
				object obj = logicalDevice!.CustomData.TryGetValue(TagKey);
				List<ILogicalDeviceTag> tagList = obj as List<ILogicalDeviceTag>;
				return tagList != null && Enumerable.Any(tags, (ILogicalDeviceTag tag) => tagList.Contains(tag));
			}
		}

		public List<ILogicalDeviceTag> GetAllTags(ILogicalDevice logicalDevice)
		{
			return new List<ILogicalDeviceTag>(GetTags<ILogicalDeviceTag>(logicalDevice));
		}

		public IEnumerable<TLogicalDeviceTag> GetTags<TLogicalDeviceTag>(ILogicalDevice? logicalDevice) where TLogicalDeviceTag : ILogicalDeviceTag
		{
			if (logicalDevice?.IsDisposed ?? true)
			{
				return Array.Empty<TLogicalDeviceTag>();
			}
			lock (logicalDevice!.CustomData)
			{
				if (logicalDevice!.CustomData.TryGetValue(TagKey) is List<ILogicalDeviceTag> source)
				{
					return Enumerable.OfType<TLogicalDeviceTag>(source);
				}
			}
			return Array.Empty<TLogicalDeviceTag>();
		}

		public IObservableCache<ILogicalDeviceTag, string> ObserveTags(ILogicalDevice logicalDevice)
		{
			ILogicalDevice logicalDevice2 = logicalDevice;
			return _tags.Connect().Filter<(ILogicalDevice, ILogicalDeviceTag), string>(((ILogicalDevice device, ILogicalDeviceTag tag) values) => values.device.LogicalId.Equals(logicalDevice2.LogicalId)).Transform<ILogicalDeviceTag, (ILogicalDevice, ILogicalDeviceTag), string>(((ILogicalDevice device, ILogicalDeviceTag tag) values) => values.tag)
				.AsObservableCache();
		}

		public IObservableCache<(ILogicalDevice device, TLogicalDeviceTag tag), string> ObserveDevices<TLogicalDeviceTag>() where TLogicalDeviceTag : ILogicalDeviceTag
		{
			return _tags.Connect().Filter<(ILogicalDevice, ILogicalDeviceTag), string>(((ILogicalDevice device, ILogicalDeviceTag tag) values) => values.tag is TLogicalDeviceTag).Transform<(ILogicalDevice, TLogicalDeviceTag), (ILogicalDevice, ILogicalDeviceTag), string>(((ILogicalDevice device, ILogicalDeviceTag tag) values) => (values.device, (TLogicalDeviceTag)values.tag))
				.AsObservableCache();
		}

		public string DebugTagsAsString(ILogicalDevice logicalDevice)
		{
			return "[" + DebugTagsAsString(GetAllTags(logicalDevice)) + "]";
		}

		public static string DebugTagsAsString(IEnumerable<ILogicalDeviceTag> tagList)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (ILogicalDeviceTag tag in tagList)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(tag.GetType().Name);
				stringBuilder.Append(" (");
				stringBuilder.Append(tag);
				stringBuilder.Append(")");
			}
			return stringBuilder.ToString();
		}
	}
}
