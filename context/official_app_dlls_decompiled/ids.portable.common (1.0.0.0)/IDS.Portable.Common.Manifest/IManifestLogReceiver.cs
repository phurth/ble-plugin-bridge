using System.Collections.Generic;

namespace IDS.Portable.Common.Manifest
{
	public interface IManifestLogReceiver
	{
		void LogManifest(IManifest manifest);

		void LogCurrentDTCs(IManifestProduct product, IEnumerable<IManifestDTC>? DTCs = null);

		void LogChangedDTCs(IManifestProduct product, IEnumerable<IManifestDTC> DTCChanges);
	}
}
