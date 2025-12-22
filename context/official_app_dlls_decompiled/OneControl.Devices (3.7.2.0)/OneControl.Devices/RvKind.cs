using System;
using System.Reflection;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Devices
{
	[JsonObject(MemberSerialization.OptIn)]
	public class RvKind : JsonSerializable<RvKind>, IRvKind, IEquatable<IRvKind>, IJsonSerializable, IJsonSerializerClass
	{
		public static readonly RvKind None;

		public static readonly int RvMinimumManufacturedYear;

		[JsonProperty]
		public string SerializerClass => GetType().Name;

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public RvDetail<int>? Year { get; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public RvDetail<string>? Make { get; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public RvDetail<string>? Model { get; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public RvDetail<string>? FloorPlan { get; }

		public bool IsHardwareRead { get; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string? Vin { get; }

		public bool IsHardwareReadVin { get; }

		[JsonConstructor]
		public RvKind(RvDetail<int>? year, RvDetail<string>? make, RvDetail<string>? model, RvDetail<string>? floorPlan, bool isHardwareRead = false, string? vin = null, bool isHardwareReadVin = false)
		{
			Year = year;
			Make = make;
			Model = model;
			FloorPlan = floorPlan;
			IsHardwareRead = isHardwareRead;
			Vin = vin;
			IsHardwareReadVin = isHardwareReadVin;
		}

		public bool ValuesAreSupplied()
		{
			if (Year?.Value > RvMinimumManufacturedYear && !string.IsNullOrWhiteSpace(Make?.Name) && !string.IsNullOrWhiteSpace(Model?.Name))
			{
				return !string.IsNullOrWhiteSpace(FloorPlan?.Name);
			}
			return false;
		}

		public bool LciIdsAreSupplied()
		{
			if (Year?.LciId > RvMinimumManufacturedYear)
			{
				RvDetail<string>? make = Make;
				if (make != null)
				{
					_ = make!.LciId;
					if (true)
					{
						RvDetail<string>? model = Model;
						if (model != null)
						{
							_ = model!.LciId;
							if (true)
							{
								RvDetail<string>? floorPlan = FloorPlan;
								if (floorPlan == null)
								{
									return false;
								}
								_ = floorPlan!.LciId;
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		internal bool SalesforceIdsAreSupplied()
		{
			if (!string.IsNullOrWhiteSpace(Year?.SalesforceId) && int.TryParse(Year?.SalesforceId, out var result) && result > RvMinimumManufacturedYear && !string.IsNullOrWhiteSpace(Make?.SalesforceId) && !string.IsNullOrWhiteSpace(Model?.SalesforceId))
			{
				RvDetail<string>? floorPlan = FloorPlan;
				if (floorPlan == null || floorPlan!.LciId != -1)
				{
					return !string.IsNullOrWhiteSpace(FloorPlan?.SalesforceId);
				}
				return true;
			}
			return false;
		}

		public bool IsAllInformationSupplied()
		{
			if (ValuesAreSupplied() && LciIdsAreSupplied())
			{
				return SalesforceIdsAreSupplied();
			}
			return false;
		}

		public bool Equals(IRvKind other)
		{
			if (this == other)
			{
				return true;
			}
			if (other == null)
			{
				return false;
			}
			if (object.Equals(Year, other.Year) && object.Equals(Make, other.Make) && object.Equals(Model, other.Model))
			{
				return object.Equals(FloorPlan, other.FloorPlan);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (this == obj)
			{
				return true;
			}
			if (!(obj is IRvKind other))
			{
				return false;
			}
			return Equals(other);
		}

		public override int GetHashCode()
		{
			return 17.Hash(Year).Hash(Make).Hash(Model)
				.Hash(FloorPlan);
		}

		static RvKind()
		{
			None = new RvKind(null, null, null, null);
			RvMinimumManufacturedYear = 1930;
			Type type = MethodBase.GetCurrentMethod()?.DeclaringType;
			if (type != null)
			{
				TypeRegistry.Register(type.Name, type);
			}
		}

		public override string ToString()
		{
			return $"Year({Year}), Make({Make}), Model({Model}), FloorPlan({FloorPlan}), Vin({Vin})";
		}
	}
}
