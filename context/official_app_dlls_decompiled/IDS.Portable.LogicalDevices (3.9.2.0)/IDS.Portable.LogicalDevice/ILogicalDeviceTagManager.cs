using System.Collections.Generic;
using DynamicData;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceTagManager
	{
		void AddTag(ILogicalDeviceTag tag, ILogicalDevice logicalDevice);

		void RemoveTag(ILogicalDeviceTag tag, ILogicalDevice logicalDevice);

		bool ContainsTag(ILogicalDeviceTag tag, ILogicalDevice logicalDevice);

		void AddTags(IEnumerable<ILogicalDeviceTag> tags, ILogicalDevice logicalDevice);

		bool ContainsAllTags(IEnumerable<ILogicalDeviceTag> tags, ILogicalDevice logicalDevice);

		bool ContainsAnyMatchingTag(IEnumerable<ILogicalDeviceTag> tags, ILogicalDevice logicalDevice);

		List<ILogicalDeviceTag> GetAllTags(ILogicalDevice logicalDevice);

		IEnumerable<TLogicalDeviceTag> GetTags<TLogicalDeviceTag>(ILogicalDevice logicalDevice) where TLogicalDeviceTag : ILogicalDeviceTag;

		IObservableCache<ILogicalDeviceTag, string> ObserveTags(ILogicalDevice logicalDevice);

		IObservableCache<(ILogicalDevice device, TLogicalDeviceTag tag), string> ObserveDevices<TLogicalDeviceTag>() where TLogicalDeviceTag : ILogicalDeviceTag;
	}
}
