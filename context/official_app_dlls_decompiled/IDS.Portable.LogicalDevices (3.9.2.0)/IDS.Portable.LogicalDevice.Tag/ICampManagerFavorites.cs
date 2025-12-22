using System;
using System.ComponentModel;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice.Tag
{
	public interface ICampManagerFavorites : ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass, INotifyPropertyChanged, IEquatable<ICampManagerFavorites>
	{
		Guid FavoriteId { get; }

		string Name { get; }

		int Order { get; }

		CampManagerFavoriteType Type { get; set; }

		ICampManagerFavorites Clone();
	}
}
