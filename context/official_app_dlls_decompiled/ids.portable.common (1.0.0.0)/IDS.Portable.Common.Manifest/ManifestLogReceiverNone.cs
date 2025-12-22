using System.Collections.Generic;

namespace IDS.Portable.Common.Manifest
{
	public class ManifestLogReceiverNone : IManifestLogReceiver
	{
		public void LogManifest(IManifest manifest)
		{
		}

		public void LogCurrentDTCs(IManifestProduct product, IEnumerable<IManifestDTC>? DTCs = null)
		{
		}

		public void LogChangedDTCs(IManifestProduct product, IEnumerable<IManifestDTC> DTCChanges)
		{
		}
	}
}
