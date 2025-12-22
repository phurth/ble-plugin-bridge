using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults.Statistics
{
	public abstract class IdsCanAccessoryStatistics : CommonNotifyPropertyChanged
	{
		private readonly IdsCanAccessoryStatisticsFrequencyMetrics _frequencyMetrics = new IdsCanAccessoryStatisticsFrequencyMetrics();

		public abstract IdsCanAccessoryMessageType MessageType { get; }

		public DateTime LastMessageReceivedTimestamp { get; private set; } = DateTime.MaxValue;


		public IReadOnlyList<byte> RawData { get; private set; } = Array.Empty<byte>();


		public IdsCanAccessoryStatisticsFrequencyMetrics FrequencyMetrics => _frequencyMetrics;

		public virtual void UpdateScanResultMetadata(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult)
		{
			RawData = manufacturerSpecificData;
			LastMessageReceivedTimestamp = DateTime.Now;
			_frequencyMetrics.Update();
			NotifyPropertyChanged("RawData");
			NotifyPropertyChanged("LastMessageReceivedTimestamp");
			NotifyPropertyChanged("FrequencyMetrics");
		}

		public virtual void Clear()
		{
			LastMessageReceivedTimestamp = DateTime.MaxValue;
			RawData = Array.Empty<byte>();
			_frequencyMetrics.Clear();
		}

		public override string ToString()
		{
			IReadOnlyList<byte> rawData = RawData;
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(12, 3);
			defaultInterpolatedStringHandler.AppendFormatted(GetType().Name);
			defaultInterpolatedStringHandler.AppendLiteral(": ");
			defaultInterpolatedStringHandler.AppendFormatted(FrequencyMetrics);
			defaultInterpolatedStringHandler.AppendLiteral(" RawData: ");
			defaultInterpolatedStringHandler.AppendFormatted(rawData.DebugDump(0, rawData.Count));
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
