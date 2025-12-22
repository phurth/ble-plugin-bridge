using System;
using System.Collections.Generic;

namespace IDS.Portable.Common.Manifest
{
	public interface IManifestLogEntry
	{
		DateTime Timestamp { get; }

		IManifest? Manifest { get; }

		string? ProductUniqueID { get; }

		ManifestDTCListType DTCsType { get; }

		IEnumerable<IManifestDTC>? DTCs { get; }
	}
}
