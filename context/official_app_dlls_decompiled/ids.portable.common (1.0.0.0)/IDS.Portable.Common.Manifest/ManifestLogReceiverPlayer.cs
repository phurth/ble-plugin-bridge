using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Portable.Common.Manifest
{
	public class ManifestLogReceiverPlayer : IManifestLogReceiverPlayer
	{
		private const string LogTag = "ManifestLogReceiverPlayer";

		private IEnumerable<IManifestLogEntry>? _webLogEntries;

		private readonly object _locker = new object();

		public void LoadWebServiceLog(IEnumerable<IManifestLogEntry> webServiceLogEntries)
		{
			if (webServiceLogEntries == null)
			{
				throw new Exception("Invalid manifest");
			}
			lock (_locker)
			{
				_webLogEntries = webServiceLogEntries;
			}
		}

		public async Task<uint> Replay(IManifestLogReceiver manifestLogReceiver, CancellationToken cancellationToken, float speedFactor = 1f)
		{
			cancellationToken.ThrowIfCancellationRequested();
			List<IManifestLogEntry> list;
			lock (_locker)
			{
				list = new List<IManifestLogEntry>(_webLogEntries);
			}
			Dictionary<string, IManifestProduct> lastSeenProductDict = new Dictionary<string, IManifestProduct>();
			uint logEntriesProcessed = 0u;
			DateTime dateTime = default(DateTime);
			foreach (IManifestLogEntry logEntry in list)
			{
				if (logEntry == null)
				{
					continue;
				}
				if (logEntriesProcessed == 0)
				{
					dateTime = logEntry.Timestamp;
				}
				TimeSpan timeSpan = logEntry.Timestamp - dateTime;
				int num = ((!((double)speedFactor < 0.001)) ? ((int)(((timeSpan.TotalMilliseconds < 0.0) ? 0.0 : timeSpan.TotalMilliseconds) / (double)speedFactor)) : 0);
				if (num > 0)
				{
					await Task.Delay(num, cancellationToken).ConfigureAwait(false);
				}
				if (logEntry.Manifest != null)
				{
					foreach (IManifestProduct product2 in logEntry.Manifest!.Products)
					{
						lastSeenProductDict[product2.UniqueID] = product2;
					}
					manifestLogReceiver?.LogManifest(logEntry.Manifest);
				}
				else
				{
					if (logEntry.ProductUniqueID == null)
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(60, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Invalid log entry ");
						defaultInterpolatedStringHandler.AppendFormatted(logEntriesProcessed);
						defaultInterpolatedStringHandler.AppendLiteral(", expected a Manifest or Product specifier");
						throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					if (!lastSeenProductDict.ContainsKey(logEntry.ProductUniqueID))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(53, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Invalid log entry ");
						defaultInterpolatedStringHandler.AppendFormatted(logEntriesProcessed);
						defaultInterpolatedStringHandler.AppendLiteral(", no information found for product ");
						defaultInterpolatedStringHandler.AppendFormatted(logEntry.ProductUniqueID);
						throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					IManifestProduct product = lastSeenProductDict[logEntry.ProductUniqueID];
					switch (logEntry.DTCsType)
					{
					case ManifestDTCListType.Current:
						manifestLogReceiver?.LogCurrentDTCs(product, logEntry.DTCs);
						break;
					case ManifestDTCListType.Delta:
						manifestLogReceiver?.LogChangedDTCs(product, logEntry.DTCs);
						break;
					case ManifestDTCListType.None:
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Invalid log entry ");
						defaultInterpolatedStringHandler.AppendFormatted(logEntriesProcessed);
						defaultInterpolatedStringHandler.AppendLiteral(", expected Current or Delta DTC type");
						throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					}
				}
				dateTime = logEntry.Timestamp;
				logEntriesProcessed++;
			}
			return logEntriesProcessed;
		}
	}
}
