using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ids.portable.ble.Ble;
using ids.portable.ble.Platforms.Shared;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using Plugin.BLE.Abstractions.Contracts;

namespace OneControl.Direct.IdsCanAccessoryBle.ScanResults
{
	public class IdsCanAccessoryLinkManager
	{
		private readonly IBleService _bleService;

		public static readonly Guid KeySeedExchangeServiceGuidDefault = IdsCanAccessoryBleScanResultPrimaryServiceFactory.AccessoryPrimaryService;

		public static readonly Guid SeedCharacteristicGuidDefault = Guid.Parse("20890001-62e8-4795-9377-b44229c80329");

		public static readonly Guid KeyCharacteristicGuidDefault = Guid.Parse("20890002-62e8-4795-9377-b44229c80329");

		private const int MaxRetries = 3;

		private const int RetryDelayMs = 500;

		private const int WriteDelayMs = 100;

		private const string LogTag = "BleDeviceUnlockManager";

		private static readonly ASCIIEncoding AsciiEncoder = new ASCIIEncoding();

		private static readonly Random RandomNumberGenerator = new Random();

		public Guid KeySeedServiceGuid { get; }

		public Guid SeedCharacteristicGuid { get; }

		public Guid KeyCharacteristicGuid { get; }

		public IdsCanAccessoryLinkManager(IBleService bleService)
			: this(bleService, KeySeedExchangeServiceGuidDefault, SeedCharacteristicGuidDefault, KeyCharacteristicGuidDefault)
		{
		}

		public IdsCanAccessoryLinkManager(IBleService bleService, Guid serviceGuid, Guid seedCharacteristicGuid, Guid keyCharacteristicGuid)
		{
			_bleService = bleService;
			KeySeedServiceGuid = serviceGuid;
			SeedCharacteristicGuid = seedCharacteristicGuid;
			KeyCharacteristicGuid = keyCharacteristicGuid;
		}

		public async Task<BleDeviceKeySeedExchangeResult> TryLinkVerificationAsync(IDevice? device, uint cypher, CancellationToken cancellationToken)
		{
			if (device == null)
			{
				TaggedLog.Debug("BleDeviceUnlockManager", "IDevice is null, cannot perform key/seed exchange");
				return BleDeviceKeySeedExchangeResult.Failed;
			}
			IService service = null;
			int attempts = 0;
			while (service == null && attempts++ < 3)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return BleDeviceKeySeedExchangeResult.Failed;
				}
				try
				{
					service = await _bleService.Manager.GetServiceAsync(device, KeySeedServiceGuid, cancellationToken);
					if (service == null)
					{
						throw new Exception("Unable to get service");
					}
				}
				catch (Exception ex)
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Attempt ");
					defaultInterpolatedStringHandler.AppendFormatted(attempts);
					defaultInterpolatedStringHandler.AppendLiteral(" failed: ");
					defaultInterpolatedStringHandler.AppendFormatted(ex.Message);
					TaggedLog.Debug("BleDeviceUnlockManager", defaultInterpolatedStringHandler.ToStringAndClear());
					if (attempts >= 3)
					{
						return BleDeviceKeySeedExchangeResult.Failed;
					}
					await TaskExtension.TryDelay(500, cancellationToken);
					if (cancellationToken.IsCancellationRequested)
					{
						return BleDeviceKeySeedExchangeResult.Failed;
					}
				}
			}
			if (service == null)
			{
				return BleDeviceKeySeedExchangeResult.Failed;
			}
			uint seed = (uint)RandomNumberGenerator.Next(int.MaxValue);
			byte[] seedBuffer = new byte[4];
			seedBuffer.SetValueUInt32(seed, 0);
			using (service)
			{
				ICharacteristic keyCharacteristic;
				ICharacteristic seedCharacteristic;
				try
				{
					keyCharacteristic = await _bleService.Manager.GetCharacteristicAsync(device, service, KeyCharacteristicGuid, cancellationToken);
					if (keyCharacteristic == null)
					{
						throw new Exception("Unable to get Characteristic for key");
					}
					seedCharacteristic = await _bleService.Manager.GetCharacteristicAsync(device, service, SeedCharacteristicGuid, cancellationToken);
					if (seedCharacteristic == null)
					{
						throw new Exception("Unable to get Characteristic for seed");
					}
				}
				catch (Exception ex2)
				{
					TaggedLog.Debug("BleDeviceUnlockManager", "Unable to get characteristic: " + ex2.Message);
					return BleDeviceKeySeedExchangeResult.Failed;
				}
				attempts = 0;
				byte[] keyBuffer;
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler;
				do
				{
					keyBuffer = null;
					try
					{
						await _bleService.Manager.WriteCharacteristicWithResponse(seedCharacteristic, seedBuffer);
					}
					catch (Exception ex3)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(43, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Write seed characteristic attempt ");
						defaultInterpolatedStringHandler.AppendFormatted(attempts);
						defaultInterpolatedStringHandler.AppendLiteral(" failed: ");
						defaultInterpolatedStringHandler.AppendFormatted(ex3.Message);
						TaggedLog.Debug("BleDeviceUnlockManager", defaultInterpolatedStringHandler.ToStringAndClear());
						break;
					}
					if (cancellationToken.IsCancellationRequested)
					{
						return BleDeviceKeySeedExchangeResult.Failed;
					}
					try
					{
						keyBuffer = await _bleService.Manager.ReadCharacteristicAsync(keyCharacteristic, cancellationToken);
						byte[] array = keyBuffer;
						int num = ((array != null) ? array.Length : 0);
						if (num < 4)
						{
							defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(32, 2);
							defaultInterpolatedStringHandler.AppendLiteral("Invalid key size, expected ");
							defaultInterpolatedStringHandler.AppendFormatted(4);
							defaultInterpolatedStringHandler.AppendLiteral(" got ");
							defaultInterpolatedStringHandler.AppendFormatted(num);
							TaggedLog.Debug("BleDeviceUnlockManager", defaultInterpolatedStringHandler.ToStringAndClear());
							return BleDeviceKeySeedExchangeResult.Failed;
						}
					}
					catch (Exception ex4)
					{
						defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(41, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Read key characteristic attempt ");
						defaultInterpolatedStringHandler.AppendFormatted(attempts);
						defaultInterpolatedStringHandler.AppendLiteral(" failed: ");
						defaultInterpolatedStringHandler.AppendFormatted(ex4.Message);
						TaggedLog.Debug("BleDeviceUnlockManager", defaultInterpolatedStringHandler.ToStringAndClear());
						break;
					}
					if (cancellationToken.IsCancellationRequested)
					{
						return BleDeviceKeySeedExchangeResult.Failed;
					}
				}
				while (attempts++ < 3 && keyBuffer == null);
				if (keyBuffer == null)
				{
					return BleDeviceKeySeedExchangeResult.Failed;
				}
				uint valueUInt = keyBuffer.GetValueUInt32(0);
				defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(21, 2);
				defaultInterpolatedStringHandler.AppendLiteral("Received Key of ");
				defaultInterpolatedStringHandler.AppendFormatted(keyBuffer.DebugDump());
				defaultInterpolatedStringHandler.AppendLiteral(" = 0x");
				defaultInterpolatedStringHandler.AppendFormatted(valueUInt, "X");
				TaggedLog.Debug("BleDeviceUnlockManager", defaultInterpolatedStringHandler.ToStringAndClear());
				uint num2 = BleDeviceUnlockManager.Encrypt(cypher, seed);
				if (valueUInt != num2)
				{
					defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(48, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Received Key of 0x");
					defaultInterpolatedStringHandler.AppendFormatted(valueUInt, "X");
					defaultInterpolatedStringHandler.AppendLiteral(" doesn't match expected key 0x");
					defaultInterpolatedStringHandler.AppendFormatted(num2, "X");
					TaggedLog.Debug("BleDeviceUnlockManager", defaultInterpolatedStringHandler.ToStringAndClear());
					return BleDeviceKeySeedExchangeResult.Failed;
				}
				return BleDeviceKeySeedExchangeResult.Succeeded;
			}
		}
	}
}
