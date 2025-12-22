using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IDS.Portable.Common.Manifest
{
	[JsonObject(MemberSerialization.OptIn)]
	public class ManifestProduct : IManifestProduct, IEnumerable<IManifestDevice>, IEnumerable, IComparable<IManifestProduct>
	{
		[JsonProperty(PropertyName = "DeviceList")]
		private List<IManifestDevice> _deviceList;

		[JsonProperty]
		public string UniqueID { get; private set; }

		[JsonProperty]
		public string Name { get; private set; }

		[JsonProperty]
		public ushort TypeID { get; private set; }

		[JsonProperty]
		public int AssemblyPartNumber { get; private set; }

		[JsonProperty]
		public string SoftwarePartNumber { get; set; }

		[JsonProperty]
		[JsonConverter(typeof(VersionConverter))]
		public Version ProtocolVersion { get; private set; }

		public IEnumerable<IManifestDevice> Devices => _deviceList;

		public IEnumerator<IManifestDevice> GetEnumerator()
		{
			return _deviceList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		[JsonConstructor]
		public ManifestProduct(string uniqueID, string name, ushort typeID, int assemblyPartNumber, string softwarePartNumber, Version protocolVersion, List<ManifestDevice> deviceList)
		{
			UniqueID = uniqueID;
			Name = name;
			TypeID = typeID;
			AssemblyPartNumber = assemblyPartNumber;
			SoftwarePartNumber = softwarePartNumber;
			ProtocolVersion = protocolVersion;
			_deviceList = new List<IManifestDevice>(deviceList);
		}

		public ManifestProduct(string uniqueID, string name, ushort typeID, int assemblyPartNumber, string softwarePartNumber, Version protocolVersion)
			: this(uniqueID, name, typeID, assemblyPartNumber, softwarePartNumber, protocolVersion, new List<ManifestDevice>())
		{
		}

		public void AddManifestDevice(IManifestDevice manifestDevice)
		{
			if (manifestDevice != null)
			{
				_deviceList.Add(manifestDevice);
			}
		}

		public override string ToString()
		{
			try
			{
				return JsonConvert.SerializeObject(this, Formatting.Indented);
			}
			catch
			{
				return base.ToString();
			}
		}

		public int CompareTo(IManifestProduct other)
		{
			if (this == other)
			{
				return 0;
			}
			if (other == null)
			{
				return 1;
			}
			int num = string.Compare(UniqueID, other.UniqueID, StringComparison.Ordinal);
			if (num != 0)
			{
				return num;
			}
			return TypeID.CompareTo(other.TypeID);
		}
	}
}
