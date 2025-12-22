using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common.Manifest
{
	public interface IManifestLogReceiverPlayer
	{
		void LoadWebServiceLog(IEnumerable<IManifestLogEntry> webServiceLogEntries);

		Task<uint> Replay(IManifestLogReceiver manifestLogReceiver, CancellationToken cancellationToken, float speedFactor = 1f);
	}
}
