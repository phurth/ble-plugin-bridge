using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common.Extensions;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults.Statistics
{
	public class IdsCanAccessoryStatisticsStatus : IdsCanAccessoryStatistics
	{
		public override IdsCanAccessoryMessageType MessageType => IdsCanAccessoryMessageType.AccessoryStatus;

		public byte[]? DecodedData { get; private set; }

		public IdsCanAccessoryStatus? AccessoryStatus { get; private set; }

		public override void UpdateScanResultMetadata(IReadOnlyList<byte> manufacturerSpecificData, IdsCanAccessoryScanResult accessoryScanResult)
		{
			MAC accessoryMacAddress = accessoryScanResult.AccessoryMacAddress;
			if ((object)accessoryMacAddress != null)
			{
				DecodedData = IdsCanAccessoryScanResult.DecodeEncryptedRawData(accessoryMacAddress, manufacturerSpecificData);
				AccessoryStatus = accessoryScanResult.GetAccessoryStatus(accessoryMacAddress);
			}
			else
			{
				DecodedData = null;
				AccessoryStatus = null;
			}
			base.UpdateScanResultMetadata(manufacturerSpecificData, accessoryScanResult);
			NotifyPropertyChanged("DecodedData");
			NotifyPropertyChanged("AccessoryStatus");
		}

		public override void Clear()
		{
			base.Clear();
			AccessoryStatus = null;
			DecodedData = null;
		}

		public override string ToString()
		{
			byte[] decodedData = DecodedData;
			IdsCanAccessoryStatus? accessoryStatus = AccessoryStatus;
			if (!accessoryStatus.HasValue)
			{
				return base.ToString();
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 6);
			defaultInterpolatedStringHandler.AppendFormatted(base.ToString());
			defaultInterpolatedStringHandler.AppendLiteral(" DecodeData:");
			defaultInterpolatedStringHandler.AppendFormatted(decodedData?.DebugDump(0, decodedData.Length));
			defaultInterpolatedStringHandler.AppendLiteral(" ProductId(0x");
			defaultInterpolatedStringHandler.AppendFormatted((ushort)accessoryStatus.Value.ProductId, "X");
			defaultInterpolatedStringHandler.AppendLiteral("):");
			defaultInterpolatedStringHandler.AppendFormatted(accessoryStatus.Value.ProductId);
			defaultInterpolatedStringHandler.AppendLiteral(" DeviceType(0x");
			defaultInterpolatedStringHandler.AppendFormatted((byte)accessoryStatus.Value.DeviceType, "X");
			defaultInterpolatedStringHandler.AppendLiteral("):");
			defaultInterpolatedStringHandler.AppendFormatted(accessoryStatus.Value.DeviceType);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
