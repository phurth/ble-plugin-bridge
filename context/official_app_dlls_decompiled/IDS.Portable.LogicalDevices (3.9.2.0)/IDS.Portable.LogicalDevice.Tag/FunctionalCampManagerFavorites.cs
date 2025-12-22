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
	public class FunctionalCampManagerFavorites : JsonSerializable<FunctionalCampManagerFavorites>, IToggleableCampManagerFavorite, ICampManagerFavorites, ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass, INotifyPropertyChanged, IEquatable<ICampManagerFavorites>
	{
		private int _order;

		private CampManagerFavoriteType _type;

		[JsonProperty]
		public FunctionalCampManagerFavoriteType FunctionType { get; private set; }

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
		public FunctionalCampManagerFavorites(string name, int order, CampManagerFavoriteType type, FunctionalCampManagerFavoriteType favoriteType)
			: this(Guid.NewGuid(), name, order, type, favoriteType)
		{
		}

		private FunctionalCampManagerFavorites(Guid favoriteId, string name, int order, CampManagerFavoriteType type, FunctionalCampManagerFavoriteType favoriteType)
		{
			FavoriteId = favoriteId;
			Name = name;
			Order = order;
			Type = type;
			FunctionType = favoriteType;
		}

		void IToggleableCampManagerFavorite.SetOrder(int order)
		{
			Order = order;
		}

		public bool Equals(ILogicalDeviceTag other)
		{
			if (other is FunctionalCampManagerFavorites functionalCampManagerFavorites)
			{
				return string.Equals(Name, functionalCampManagerFavorites.Name);
			}
			return false;
		}

		public bool Equals(ICampManagerFavorites other)
		{
			if (other is FunctionalCampManagerFavorites functionalCampManagerFavorites)
			{
				return string.Equals(Name, functionalCampManagerFavorites.Name);
			}
			return false;
		}

		public override bool Equals(object? other)
		{
			if (other is FunctionalCampManagerFavorites functionalCampManagerFavorites)
			{
				return string.Equals(Name, functionalCampManagerFavorites.Name);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public override string ToString()
		{
			return Name;
		}

		public ICampManagerFavorites Clone()
		{
			return new FunctionalCampManagerFavorites(FavoriteId, Name, Order, Type, FunctionType);
		}

		static FunctionalCampManagerFavorites()
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
