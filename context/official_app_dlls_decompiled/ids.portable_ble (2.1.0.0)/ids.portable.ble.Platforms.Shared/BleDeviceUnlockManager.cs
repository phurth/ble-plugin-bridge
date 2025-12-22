using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ids.portable.ble.BleManager;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using Plugin.BLE.Abstractions.Contracts;

namespace ids.portable.ble.Platforms.Shared
{
	public static class BleDeviceUnlockManager
	{
		public static readonly Guid KeySeedExchangeServiceGuidDefault = Guid.Parse("00000010-0200-a58e-e411-afe28044e62c");

		public static readonly Guid SeedCharacteristicGuidDefault = Guid.Parse("00000012-0200-a58e-e411-afe28044e62c");

		public static readonly Guid KeyCharacteristicGuidDefault = Guid.Parse("00000013-0200-a58e-e411-afe28044e62c");

		private const int BleCharacteristicWriteDelayMs = 500;

		private const int BleBondingDelayMs = 5000;

		private const int ServiceRetryDelayMs = 500;

		private const string LogTag = "BleDeviceUnlockManager";

		private const string Unlocked = "unlocked";

		private static readonly ASCIIEncoding AsciiEncoder = new ASCIIEncoding();

		public static uint Encrypt(uint cypher, uint seed)
		{
			uint num = 2654435769u;
			for (int i = 0; i < 32; i++)
			{
				seed += ((cypher << 4) + 1131376761) ^ (cypher + num) ^ ((cypher >> 5) + 1919510376);
				cypher += ((seed << 4) + 1948272964) ^ (seed + num) ^ ((seed >> 5) + 1400073827);
				num += 2654435769u;
			}
			return seed;
		}

		public static Task<BleDeviceKeySeedExchangeResult> PerformKeySeedExchange(this IBleManager bleManager, IDevice? device, uint cypher, CancellationToken cancellationToken)
		{
			return bleManager.PerformKeySeedExchange(device, cypher, KeySeedExchangeServiceGuidDefault, SeedCharacteristicGuidDefault, KeyCharacteristicGuidDefault, cancellationToken);
		}

		public static async Task<BleDeviceKeySeedExchangeResult> PerformKeySeedExchange(this IBleManager bleManager, IDevice? device, uint cypher, Guid serviceGuid, Guid seedCharacteristicGuid, Guid keyCharacteristicGuid, CancellationToken ct)
		{
			if (device == null)
			{
				TaggedLog.Debug("BleDeviceUnlockManager", "PerformKeySeedExchange: IDevice is null, cannot perform key/seed exchange");
				return BleDeviceKeySeedExchangeResult.Failed;
			}
			IService service = null;
			int attempts = 0;
			while (service == null && attempts++ < 3)
			{
				service = await bleManager.GetServiceAsync(device, serviceGuid, ct);
				try
				{
					await Task.Delay(500, ct);
				}
				catch (Exception ex)
				{
					service?.TryDispose();
					TaggedLog.Warning("BleDeviceUnlockManager", "PerformKeySeedExchange: failed to get service - " + ex.Message);
					return BleDeviceKeySeedExchangeResult.Failed;
				}
			}
			if (service == null)
			{
				TaggedLog.Warning("BleDeviceUnlockManager", "PerformKeySeedExchange: service is null, key/seed exchange is not supported");
				return BleDeviceKeySeedExchangeResult.Unsupported;
			}
			using (service)
			{
				ICharacteristic keyCharacteristic = await bleManager.GetCharacteristicAsync(device, service, keyCharacteristicGuid, ct);
				ICharacteristic seedCharacteristic = await bleManager.GetCharacteristicAsync(device, service, seedCharacteristicGuid, ct);
				if (keyCharacteristic == null || seedCharacteristic == null)
				{
					TaggedLog.Warning("BleDeviceUnlockManager", "PerformKeySeedExchange: could not read key/seed characteristics");
					return BleDeviceKeySeedExchangeResult.Failed;
				}
				for (attempts = 0; attempts < 3; attempts++)
				{
					byte[] buffer2;
					try
					{
						buffer2 = await bleManager.ReadCharacteristicAsync(seedCharacteristic, ct);
					}
					catch (Exception ex2)
					{
						TaggedLog.Debug("BleDeviceUnlockManager", "PerformKeySeedExchange: Read Buffer Failed, on iOS this likely means that bonding was lost - " + ex2.Message);
						if (ct.IsCancellationRequested)
						{
							return BleDeviceKeySeedExchangeResult.Cancelled;
						}
						await TaskExtension.TryDelay(5000, ct);
						continue;
					}
					if (buffer2 == null)
					{
						TaggedLog.Debug("BleDeviceUnlockManager", "PerformKeySeedExchange: Read Buffer Empty");
						continue;
					}
					if (IsUnlocked(buffer2))
					{
						return BleDeviceKeySeedExchangeResult.Succeeded;
					}
					if (buffer2.Length < 4)
					{
						continue;
					}
					uint valueUInt = buffer2.GetValueUInt32(0);
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 3);
					defaultInterpolatedStringHandler.AppendFormatted("PerformKeySeedExchange");
					defaultInterpolatedStringHandler.AppendLiteral(": Read Buffer ");
					defaultInterpolatedStringHandler.AppendFormatted(buffer2.DebugDump());
					defaultInterpolatedStringHandler.AppendLiteral(" => 0x");
					defaultInterpolatedStringHandler.AppendFormatted(valueUInt);
					defaultInterpolatedStringHandler.AppendLiteral(" ");
					TaggedLog.Debug("BleDeviceUnlockManager", defaultInterpolatedStringHandler.ToStringAndClear());
					if (valueUInt != 0)
					{
						try
						{
							uint value = Encrypt(cypher, valueUInt);
							byte[] array = new byte[4];
							array.SetValueUInt32(value, 0);
							await bleManager.WriteCharacteristicAsync(keyCharacteristic, array, ct);
							await TaskExtension.TryDelay(500, ct);
						}
						catch (Exception ex3)
						{
							TaggedLog.Debug("BleDeviceUnlockManager", "PerformKeySeedExchange: Write Failed, unable to unlock device - " + ex3.Message);
							return BleDeviceKeySeedExchangeResult.Failed;
						}
					}
				}
				return BleDeviceKeySeedExchangeResult.Failed;
			}
			static bool IsUnlocked(byte[]? buffer)
			{
				if (buffer != null && buffer!.Length == "unlocked".Length)
				{
					return string.Equals("unlocked", AsciiEncoder.GetString(buffer), StringComparison.OrdinalIgnoreCase);
				}
				return false;
			}
		}
	}
}
