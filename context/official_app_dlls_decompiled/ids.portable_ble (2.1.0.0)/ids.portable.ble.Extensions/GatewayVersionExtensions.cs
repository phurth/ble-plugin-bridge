using System;
using System.Threading;
using System.Threading.Tasks;
using ids.portable.ble.BleManager;
using ids.portable.ble.ScanResults;
using Plugin.BLE.Abstractions.Contracts;

namespace ids.portable.ble.Extensions
{
	public static class GatewayVersionExtensions
	{
		private static Guid BleGatewayCanService = new Guid("00000000-0200-A58E-E411-AFE28044E62C");

		private static Guid SoftwarePartNumber = new Guid("00000004-0200-a58e-e411-afe28044e62c");

		public static readonly BleGatewayInfo[] GatewayVersions = new BleGatewayInfo[3]
		{
			new BleGatewayInfo(20707, 'F', BleGatewayInfo.GatewayVersion.V1),
			new BleGatewayInfo(23357, 'A', BleGatewayInfo.GatewayVersion.V2),
			new BleGatewayInfo(23357, 'D', BleGatewayInfo.GatewayVersion.V2_D)
		};

		public static async Task<BleGatewayInfo.GatewayVersion> GetGatewayVersionFromCharacteristic(this IBleManager? bleManager, IDevice? device, CancellationToken cancellationToken)
		{
			_ = 1;
			try
			{
				if (bleManager == null)
				{
					return BleGatewayInfo.GatewayVersion.Unknown;
				}
				if (device == null)
				{
					return BleGatewayInfo.GatewayVersion.Unknown;
				}
				ICharacteristic characteristic = await bleManager!.GetCharacteristicAsync(device, BleGatewayCanService, SoftwarePartNumber, cancellationToken);
				if (characteristic == null)
				{
					return BleGatewayInfo.GatewayVersion.Unknown;
				}
				byte[] array = await bleManager!.ReadCharacteristicAsync(characteristic, cancellationToken);
				if (array == null || array.Length != 8)
				{
					return BleGatewayInfo.GatewayVersion.Unknown;
				}
				int partNumber = (array[0] & 0xF) * (int)Math.Pow(10.0, 4.0) + (array[1] & 0xF) * (int)Math.Pow(10.0, 3.0) + (array[2] & 0xF) * (int)Math.Pow(10.0, 2.0) + (array[3] & 0xF) * (int)Math.Pow(10.0, 1.0) + (array[4] & 0xF) * (int)Math.Pow(10.0, 0.0);
				char rev = (char)array[6];
				return partNumber.GetGatewayVersion(rev);
			}
			catch (Exception)
			{
				return BleGatewayInfo.GatewayVersion.Unknown;
			}
		}

		public static bool IsLargeMtuSupported(this BleGatewayInfo.GatewayVersion gatewayVersion)
		{
			return gatewayVersion.IsVersionAtLeast(BleGatewayInfo.GatewayVersion.V2_D);
		}

		public static bool IsVersionAtLeast(this BleGatewayInfo.GatewayVersion gatewayVersion, BleGatewayInfo.GatewayVersion minVersion)
		{
			return minVersion >= gatewayVersion;
		}

		public static BleGatewayInfo.GatewayVersion GetGatewayVersion(this int partNumber, char rev)
		{
			BleGatewayInfo.GatewayVersion result = BleGatewayInfo.GatewayVersion.Unknown;
			BleGatewayInfo[] gatewayVersions = GatewayVersions;
			for (int i = 0; i < gatewayVersions.Length; i++)
			{
				BleGatewayInfo bleGatewayInfo = gatewayVersions[i];
				if (bleGatewayInfo.PartNumber == partNumber && bleGatewayInfo.MinRev <= rev)
				{
					result = bleGatewayInfo.Version;
				}
			}
			return result;
		}
	}
}
