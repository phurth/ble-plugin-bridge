using System.ComponentModel;
using Newtonsoft.Json;

namespace IDS.Portable.Common.Manifest
{
	[JsonObject(MemberSerialization.OptIn)]
	public class MainfestRvConfig : IMainfestRvConfig
	{
		[JsonProperty]
		[DefaultValue(false)]
		public bool MediaDisabled { get; set; }

		public MainfestRvConfig()
			: this(mediaDisable: false)
		{
		}

		[JsonConstructor]
		public MainfestRvConfig(bool mediaDisable)
		{
			MediaDisabled = mediaDisable;
		}
	}
}
