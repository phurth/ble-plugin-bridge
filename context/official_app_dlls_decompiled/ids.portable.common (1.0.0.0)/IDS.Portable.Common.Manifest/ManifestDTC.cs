using Newtonsoft.Json;

namespace IDS.Portable.Common.Manifest
{
	[JsonObject(MemberSerialization.OptIn)]
	public class ManifestDTC : IManifestDTC
	{
		[JsonProperty]
		public ushort TypeID { get; private set; }

		[JsonProperty]
		public string Name { get; private set; }

		[JsonProperty]
		public bool IsActive { get; private set; }

		[JsonProperty]
		public bool IsStored { get; private set; }

		[JsonProperty]
		public int PowerCyclesCounter { get; private set; }

		[JsonConstructor]
		public ManifestDTC(ushort typeID, string name, bool isActive, bool isStored, int powerCyclesCounter)
		{
			TypeID = typeID;
			Name = name;
			IsActive = isActive;
			IsStored = isStored;
			PowerCyclesCounter = powerCyclesCounter;
		}

		public override string ToString()
		{
			try
			{
				return JsonConvert.SerializeObject(this);
			}
			catch
			{
				return base.ToString();
			}
		}
	}
}
