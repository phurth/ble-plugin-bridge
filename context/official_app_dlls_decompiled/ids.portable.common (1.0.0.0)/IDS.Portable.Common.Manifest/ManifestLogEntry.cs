using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace IDS.Portable.Common.Manifest
{
	[JsonObject(MemberSerialization.OptIn)]
	public struct ManifestLogEntry : IManifestLogEntry
	{
		[JsonProperty("DTCList", NullValueHandling = NullValueHandling.Ignore)]
		private List<IManifestDTC>? _dtcList;

		[JsonProperty]
		public DateTime Timestamp { get; private set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public IManifest? Manifest { get; private set; }

		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public string? ProductUniqueID { get; private set; }

		[JsonProperty("DTCListType")]
		[JsonConverter(typeof(ManifestDTCListTypeEnumConverter))]
		public ManifestDTCListType DTCsType { get; private set; }

		[JsonIgnore]
		public IEnumerable<IManifestDTC>? DTCs => _dtcList;

		[JsonConstructor]
		private ManifestLogEntry(DateTime timestamp, Manifest manifest, ManifestProduct product, ManifestDTCListType dtcListType, List<ManifestDTC> dtcList)
		{
			Timestamp = timestamp;
			Manifest = manifest;
			ProductUniqueID = product?.UniqueID;
			DTCsType = dtcListType;
			_dtcList = ((dtcList != null) ? Enumerable.ToList(Enumerable.Cast<IManifestDTC>(dtcList)) : null);
		}

		public ManifestLogEntry(IManifest manifest)
		{
			Timestamp = DateTime.Now;
			Manifest = manifest;
			ProductUniqueID = null;
			DTCsType = ManifestDTCListType.None;
			_dtcList = null;
		}

		public ManifestLogEntry(IManifestProduct product, IEnumerable<IManifestDTC>? dtcCollection, ManifestDTCListType dtcsType)
		{
			Timestamp = DateTime.Now;
			Manifest = null;
			ProductUniqueID = product.UniqueID;
			DTCsType = dtcsType;
			_dtcList = ((dtcCollection == null) ? new List<IManifestDTC>() : new List<IManifestDTC>(dtcCollection));
		}

		public string ToJSON()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}

		public override string ToString()
		{
			try
			{
				return ToJSON();
			}
			catch
			{
				return base.ToString();
			}
		}
	}
}
