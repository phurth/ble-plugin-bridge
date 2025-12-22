using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults.Statistics
{
	public class IdsCanAccessoryStatisticsConfigStatus : IdsCanAccessoryStatistics
	{
		private List<AccessoryPidStatus> _configStatsPids = new List<AccessoryPidStatus>();

		public override IdsCanAccessoryMessageType MessageType => IdsCanAccessoryMessageType.AccessoryConfigStatus;

		public byte[]? DecodedData { get; private set; }

		public IReadOnlyList<AccessoryPidStatus> ConfigStatsPids => _configStatsPids;

		public override void UpdateScanResultMetadata(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult)
		{
			MAC accessoryMacAddress = accessoryScanResult.AccessoryMacAddress;
			if ((object)accessoryMacAddress != null)
			{
				DecodedData = IdsCanAccessoryScanResult.DecodeEncryptedRawData(accessoryMacAddress, manufacturerSpecificData);
				_configStatsPids = Enumerable.ToList(accessoryScanResult.GetAccessoryPidStatus(accessoryMacAddress));
			}
			else
			{
				DecodedData = null;
				_configStatsPids = new List<AccessoryPidStatus>();
			}
			base.UpdateScanResultMetadata(manufacturerSpecificData, accessoryScanResult);
			NotifyPropertyChanged("DecodedData");
			NotifyPropertyChanged("ConfigStatsPids");
		}

		public override string ToString()
		{
			byte[] decodedData = DecodedData;
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder3 = stringBuilder2;
			StringBuilder.AppendInterpolatedStringHandler appendInterpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(12, 2, stringBuilder2);
			appendInterpolatedStringHandler.AppendFormatted(base.ToString());
			appendInterpolatedStringHandler.AppendLiteral(" DecodeData:");
			appendInterpolatedStringHandler.AppendFormatted(decodedData?.DebugDump(0, decodedData.Length));
			stringBuilder3.AppendLine(ref appendInterpolatedStringHandler);
			foreach (AccessoryPidStatus configStatsPid in ConfigStatsPids)
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				appendInterpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(24, 3, stringBuilder2);
				appendInterpolatedStringHandler.AppendLiteral("    Pid ");
				appendInterpolatedStringHandler.AppendFormatted(configStatsPid.Id);
				appendInterpolatedStringHandler.AppendLiteral(" = 0x");
				appendInterpolatedStringHandler.AppendFormatted(configStatsPid.Value, "X");
				appendInterpolatedStringHandler.AppendLiteral(" timestamp:");
				appendInterpolatedStringHandler.AppendFormatted(configStatsPid.ReceivedTimeStamp);
				stringBuilder4.AppendLine(ref appendInterpolatedStringHandler);
			}
			return stringBuilder.ToString();
		}
	}
}
