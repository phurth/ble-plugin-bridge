using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice.Tag
{
	[JsonObject(MemberSerialization.OptIn)]
	public class DeviceCampManagerFavorites : JsonSerializable<DeviceCampManagerFavorites>, IToggleableCampManagerFavorite, ICampManagerFavorites, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass, INotifyPropertyChanged, IEquatable<ICampManagerFavorites>
	{
		private int _order;

		private CampManagerFavoriteType _type;

		[JsonProperty]
		[JsonConverter(typeof(LogicalDeviceIdConverter))]
		public ILogicalDeviceId LogicalId { get; }

		[JsonProperty]
		public Guid FavoriteId { get; private set; }

		[JsonProperty]
		public string Name { get; private set; }

		[JsonProperty]
		public int Order
		{
			get
			{
				return _order;
			}
			private set
			{
				SetField(ref _order, value, "Order");
			}
		}

		[JsonProperty]
		public CampManagerFavoriteType Type
		{
			get
			{
				return _type;
			}
			set
			{
				SetField(ref _type, value, "Type");
			}
		}

		[JsonProperty]
		public string SerializerClass => GetType().Name;

		public event PropertyChangedEventHandler PropertyChanged;

		[JsonConstructor]
		public DeviceCampManagerFavorites(ILogicalDeviceId logicalId, string name, int order, CampManagerFavoriteType type)
			: this(Guid.NewGuid(), logicalId, name, order, type)
		{
		}

		private DeviceCampManagerFavorites(Guid favoriteId, ILogicalDeviceId logicalId, string name, int order, CampManagerFavoriteType type)
		{
			FavoriteId = favoriteId;
			LogicalId = logicalId;
			Name = name;
			Order = order;
			Type = type;
		}

		void IToggleableCampManagerFavorite.SetOrder(int order)
		{
			Order = order;
		}

		public bool Equals(ILogicalDeviceTag other)
		{
			if (other is DeviceCampManagerFavorites deviceCampManagerFavorites)
			{
				return FavoriteId == deviceCampManagerFavorites.FavoriteId;
			}
			return false;
		}

		public bool Equals(ICampManagerFavorites other)
		{
			if (other is DeviceCampManagerFavorites deviceCampManagerFavorites)
			{
				return FavoriteId == deviceCampManagerFavorites.FavoriteId;
			}
			return false;
		}

		public override bool Equals(object? other)
		{
			if (other is DeviceCampManagerFavorites deviceCampManagerFavorites)
			{
				return FavoriteId == deviceCampManagerFavorites.FavoriteId;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return FavoriteId.GetHashCode();
		}

		public override string ToString()
		{
			return Name;
		}

		public ICampManagerFavorites Clone()
		{
			return new DeviceCampManagerFavorites(FavoriteId, LogicalId, Name, Order, Type);
		}

		static DeviceCampManagerFavorites()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
			{
				return false;
			}
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}
	}
}
