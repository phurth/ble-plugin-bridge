using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace IDS.Portable.Common.Manifest
{
	[JsonObject(MemberSerialization.OptIn)]
	public class ManifestDevice : IManifestDevice, IComparable<IManifestDevice>
	{
		[JsonProperty]
		public string Name { get; private set; }

		[JsonProperty]
		public ushort TypeID { get; private set; }

		[JsonProperty]
		public byte Instance { get; private set; }

		[JsonProperty]
		public string FunctionName { get; private set; }

		[JsonProperty]
		public ushort FunctionTypeID { get; private set; }

		[JsonProperty]
		public string FunctionClass { get; private set; }

		[JsonProperty]
		public byte FunctionInstance { get; private set; }

		[JsonProperty]
		public int Capabilities { get; private set; }

		[JsonProperty]
		public uint Circuit { get; private set; }

		[JsonProperty]
		public bool IsOnline { get; private set; }

		[JsonProperty("CustomAttribute", NullValueHandling = NullValueHandling.Ignore)]
		public Dictionary<string, string>? CustomAttribute { get; set; }

		[JsonProperty("Pids", NullValueHandling = NullValueHandling.Ignore)]
		public Dictionary<ushort, IManifestPid>? Pids { get; set; }

		[JsonConstructor]
		public ManifestDevice(string name, ushort typeID, byte instance, string functionName, ushort functionTypeID, string functionClass, byte functionInstance, int capabilities, uint circuit, bool isOnline, Dictionary<string, string>? customAttribute = null, Dictionary<ushort, ManifestPid>? pids = null)
		{
			Name = name;
			TypeID = typeID;
			Instance = instance;
			FunctionName = functionName;
			FunctionTypeID = functionTypeID;
			FunctionClass = functionClass;
			FunctionInstance = functionInstance;
			Capabilities = capabilities;
			Circuit = circuit;
			IsOnline = isOnline;
			CustomAttribute = customAttribute;
			Pids = null;
			if (pids == null)
			{
				return;
			}
			Pids = new Dictionary<ushort, IManifestPid>();
			foreach (KeyValuePair<ushort, ManifestPid> item in pids!)
			{
				Pids!.Add(item.Key, item.Value);
			}
		}

		public void SetCustomAttribute(string attribute, string value)
		{
			if (CustomAttribute == null)
			{
				CustomAttribute = new Dictionary<string, string>();
			}
			CustomAttribute![attribute] = value;
		}

		public string? TryGetCustomAttribute(string attribute)
		{
			if (CustomAttribute == null || !CustomAttribute!.ContainsKey(attribute))
			{
				return null;
			}
			return CustomAttribute![attribute];
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

		public int CompareTo(IManifestDevice other)
		{
			if (this == other)
			{
				return 0;
			}
			if (other == null)
			{
				return 1;
			}
			int num = string.Compare(Name, other.Name, StringComparison.Ordinal);
			if (num != 0)
			{
				return num;
			}
			return Instance.CompareTo(other.Instance);
		}
	}
}
