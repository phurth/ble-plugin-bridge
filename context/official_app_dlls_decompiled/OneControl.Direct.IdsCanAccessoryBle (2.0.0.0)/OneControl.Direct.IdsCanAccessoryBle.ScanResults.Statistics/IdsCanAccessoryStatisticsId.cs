using System.Collections.Generic;
using IDS.Core.IDS_CAN;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults.Statistics
{
	public class IdsCanAccessoryStatisticsId : IdsCanAccessoryStatistics
	{
		public byte[]? DecodedData { get; private set; }

		public MAC? AccessoryMacAddress { get; private set; }

		public string? SoftwarePartNumber { get; private set; }

		public override IdsCanAccessoryMessageType MessageType => IdsCanAccessoryMessageType.AccessoryId;

		public override void UpdateScanResultMetadata(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult)
		{
			MAC accessoryMacAddress = accessoryScanResult.AccessoryMacAddress;
			if ((object)accessoryMacAddress != null)
			{
				DecodedData = IdsCanAccessoryScanResult.DecodeEncryptedRawData(accessoryMacAddress, manufacturerSpecificData);
				AccessoryMacAddress = accessoryScanResult.AccessoryMacAddress;
				SoftwarePartNumber = accessoryScanResult.SoftwarePartNumber;
			}
			else
			{
				DecodedData = null;
				AccessoryMacAddress = null;
				SoftwarePartNumber = null;
			}
			base.UpdateScanResultMetadata(manufacturerSpecificData, accessoryScanResult);
			NotifyPropertyChanged("DecodedData");
			NotifyPropertyChanged("AccessoryMacAddress");
			NotifyPropertyChanged("SoftwarePartNumber");
		}
	}
}
