using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice.Tag
{
	public interface IToggleableCampManagerFavorite : ICampManagerFavorites, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass, INotifyPropertyChanged, IEquatable<ICampManagerFavorites>
	{
		void SetOrder(int order);
	}
}
