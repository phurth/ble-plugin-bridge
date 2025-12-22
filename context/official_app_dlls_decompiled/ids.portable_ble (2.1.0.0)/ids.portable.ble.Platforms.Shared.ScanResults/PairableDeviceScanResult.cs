using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ids.portable.ble.Extensions;
using ids.portable.ble.Platforms.Shared.ManufacturingData;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions;

namespace ids.portable.ble.Platforms.Shared.ScanResults
{
	public abstract class PairableDeviceScanResult : BleScanResult, IPairableDeviceScanResult, IBleScanResultWithIdsManufacturingData, IBleScanResult
	{
		public PairingMethod PairingMethod
		{
			get
			{
				ConnectionInfo? connectionInfo = ConnectionInfo;
				if (!connectionInfo.HasValue || !connectionInfo.GetValueOrDefault().IsValid)
				{
					return PairingMethod.Unknown;
				}
				if (ConnectionInfo.Value.BleCapability != 0)
				{
					if (ConnectionInfo.Value.BleCapability != BleCapability.NoIO)
					{
						return PairingMethod.None;
					}
					return PairingMethod.PushButton;
				}
				return PairingMethod.Pin;
			}
		}

		public bool PairingEnabled
		{
			get
			{
				if (PairingMethod != PairingMethod.None && PairingMethod != 0)
				{
					return ConnectionInfo?.PairingAvailableNow ?? false;
				}
				return false;
			}
		}

		public ConnectionInfo? ConnectionInfo { get; private set; }

		public PairingInfo? PairingInfo { get; private set; }

		public override BleRequiredAdvertisements HasRequiredAdvertisements
		{
			get
			{
				if (base.HasDeviceName && PrimaryServiceGuid.HasValue)
				{
					ConnectionInfo? connectionInfo = ConnectionInfo;
					if (connectionInfo.HasValue && connectionInfo.GetValueOrDefault().IsValid)
					{
						return BleRequiredAdvertisements.AllExist;
					}
				}
				if (!base.HasDeviceName && !PrimaryServiceGuid.HasValue)
				{
					ConnectionInfo? connectionInfo = ConnectionInfo;
					if (!connectionInfo.HasValue || !connectionInfo.GetValueOrDefault().IsValid)
					{
						return BleRequiredAdvertisements.NoneExist;
					}
				}
				return BleRequiredAdvertisements.SomeExist;
			}
		}

		protected PairableDeviceScanResult(Guid deviceId, string defaultDeviceName, int rssi, IEnumerable<AdvertisementRecord> advertisementRecords)
			: base(deviceId, defaultDeviceName, rssi, advertisementRecords)
		{
		}

		protected override void RawManufacturerSpecificDataUpdated(byte[] manufacturerSpecificData)
		{
			base.RawManufacturerSpecificDataUpdated(manufacturerSpecificData);
			if (manufacturerSpecificData.IsLciManufacturerSpecificData())
			{
				ConnectionInfo? connectionInfo = manufacturerSpecificData.TryGetIdsManufacturerSpecificData<ConnectionInfo>();
				if (connectionInfo.HasValue)
				{
					ConnectionInfo = connectionInfo;
				}
				PairingInfo? pairingInfo = manufacturerSpecificData.TryGetIdsManufacturerSpecificData<PairingInfo>();
				if (pairingInfo.HasValue)
				{
					PairingInfo = pairingInfo;
				}
			}
		}

		public override string ToString()
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 6);
			defaultInterpolatedStringHandler.AppendFormatted(GetType().Name);
			defaultInterpolatedStringHandler.AppendLiteral(": ");
			defaultInterpolatedStringHandler.AppendFormatted(base.DeviceId);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(base.DeviceName);
			defaultInterpolatedStringHandler.AppendLiteral(" ");
			defaultInterpolatedStringHandler.AppendFormatted(HasRequiredAdvertisements);
			defaultInterpolatedStringHandler.AppendLiteral(" PairingEnabled:");
			defaultInterpolatedStringHandler.AppendFormatted(PairingEnabled);
			defaultInterpolatedStringHandler.AppendLiteral("/");
			defaultInterpolatedStringHandler.AppendFormatted(PairingMethod);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}
	}
}
