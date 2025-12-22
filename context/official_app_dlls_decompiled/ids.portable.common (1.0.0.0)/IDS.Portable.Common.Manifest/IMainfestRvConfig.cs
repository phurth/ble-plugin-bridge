using Newtonsoft.Json;

namespace IDS.Portable.Common.Manifest
{
	public interface IMainfestRvConfig
	{
		[JsonProperty]
		bool MediaDisabled { get; set; }
	}
}
