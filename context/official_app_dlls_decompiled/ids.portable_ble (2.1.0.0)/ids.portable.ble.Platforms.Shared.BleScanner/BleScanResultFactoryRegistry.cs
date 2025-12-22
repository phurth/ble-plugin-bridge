using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ids.portable.ble.Exceptions;
using ids.portable.ble.Extensions;
using ids.portable.ble.ScanResults;
using IDS.Portable.Common;
using Plugin.BLE.Abstractions.Contracts;

namespace ids.portable.ble.Platforms.Shared.BleScanner
{
	public class BleScanResultFactoryRegistry : IBleScanResultFactoryRegistry
	{
		private const string LogTag = "BleScanResultFactoryRegistry";

		private readonly ConcurrentDictionary<Guid, IBleScanResultFactoryDeviceId> _scanResultFactoryDeviceIdDict = new ConcurrentDictionary<Guid, IBleScanResultFactoryDeviceId>();

		private readonly ConcurrentDictionary<Guid, IBleScanResultFactoryPrimaryService> _scanResultFactoryPrimaryServiceDict = new ConcurrentDictionary<Guid, IBleScanResultFactoryPrimaryService>();

		private readonly ConcurrentDictionary<ushort, IBleScanResultFactoryManufacturerId> _scanResultFactoryManufacturerIdDict = new ConcurrentDictionary<ushort, IBleScanResultFactoryManufacturerId>();

		public IEnumerable<Guid> RegisteredServiceUuids => Enumerable.Concat(Enumerable.Where(Enumerable.Select(_scanResultFactoryPrimaryServiceDict.Values, (IBleScanResultFactoryPrimaryService primaryServiceFactory) => primaryServiceFactory.BleScanResultKey), (Guid serviceUuid) => serviceUuid != Guid.Empty), ManufacturerIdRegisteredServiceUuids);

		public IEnumerable<Guid> ManufacturerIdRegisteredServiceUuids => Enumerable.SelectMany(Enumerable.Where(Enumerable.Select(_scanResultFactoryManufacturerIdDict.Values, (IBleScanResultFactoryManufacturerId primaryServiceFactory) => primaryServiceFactory.PrimaryServiceUuids), (IEnumerable<Guid> serviceUuid) => Enumerable.Count(serviceUuid) > 0), (IEnumerable<Guid> x) => x);

		public void Register<TKey>(IBleScanResultFactory<TKey> scanResultFactory) where TKey : notnull
		{
			if (!(scanResultFactory is IBleScanResultFactoryDeviceId bleScanResultFactoryDeviceId))
			{
				if (!(scanResultFactory is IBleScanResultFactoryPrimaryService bleScanResultFactoryPrimaryService))
				{
					if (!(scanResultFactory is IBleScanResultFactoryManufacturerId bleScanResultFactoryManufacturerId))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(52, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Unable to register factory of type ");
						defaultInterpolatedStringHandler.AppendFormatted(scanResultFactory.GetType());
						defaultInterpolatedStringHandler.AppendLiteral(" (not supported).");
						throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "scanResultFactory");
					}
					if (_scanResultFactoryManufacturerIdDict.ContainsKey(bleScanResultFactoryManufacturerId.BleScanResultKey))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(88, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Service ");
						defaultInterpolatedStringHandler.AppendFormatted(bleScanResultFactoryManufacturerId.BleScanResultFactoryName);
						defaultInterpolatedStringHandler.AppendLiteral(" can't be registered with Manufacturer ID ");
						defaultInterpolatedStringHandler.AppendFormatted(bleScanResultFactoryManufacturerId.BleScanResultKey);
						defaultInterpolatedStringHandler.AppendLiteral(" because Device ID already registered.");
						throw new BleScannerServiceAlreadyRegisteredException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					_scanResultFactoryManufacturerIdDict[bleScanResultFactoryManufacturerId.BleScanResultKey] = bleScanResultFactoryManufacturerId;
				}
				else
				{
					if (_scanResultFactoryPrimaryServiceDict.ContainsKey(bleScanResultFactoryPrimaryService.BleScanResultKey))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(88, 2);
						defaultInterpolatedStringHandler.AppendLiteral("Service ");
						defaultInterpolatedStringHandler.AppendFormatted(bleScanResultFactoryPrimaryService.BleScanResultFactoryName);
						defaultInterpolatedStringHandler.AppendLiteral(" can't be registered with Primary Service GUID ");
						defaultInterpolatedStringHandler.AppendFormatted(bleScanResultFactoryPrimaryService.BleScanResultKey);
						defaultInterpolatedStringHandler.AppendLiteral(" because GUID already registered.");
						throw new BleScannerServiceAlreadyRegisteredException(defaultInterpolatedStringHandler.ToStringAndClear());
					}
					_scanResultFactoryPrimaryServiceDict[bleScanResultFactoryPrimaryService.BleScanResultKey] = bleScanResultFactoryPrimaryService;
				}
			}
			else
			{
				if (_scanResultFactoryDeviceIdDict.ContainsKey(bleScanResultFactoryDeviceId.BleScanResultKey))
				{
					DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(82, 2);
					defaultInterpolatedStringHandler.AppendLiteral("Service ");
					defaultInterpolatedStringHandler.AppendFormatted(bleScanResultFactoryDeviceId.BleScanResultFactoryName);
					defaultInterpolatedStringHandler.AppendLiteral(" can't be registered with Device ID ");
					defaultInterpolatedStringHandler.AppendFormatted(bleScanResultFactoryDeviceId.BleScanResultKey);
					defaultInterpolatedStringHandler.AppendLiteral(" because Device ID already registered.");
					throw new BleScannerServiceAlreadyRegisteredException(defaultInterpolatedStringHandler.ToStringAndClear());
				}
				_scanResultFactoryDeviceIdDict[bleScanResultFactoryDeviceId.BleScanResultKey] = bleScanResultFactoryDeviceId;
			}
		}

		public void UnRegister<TKey>(IBleScanResultFactory<TKey> scanResultFactory) where TKey : notnull
		{
			if (!(scanResultFactory is IBleScanResultFactoryDeviceId bleScanResultFactoryDeviceId))
			{
				if (!(scanResultFactory is IBleScanResultFactoryPrimaryService bleScanResultFactoryPrimaryService))
				{
					if (!(scanResultFactory is IBleScanResultFactoryManufacturerId bleScanResultFactoryManufacturerId))
					{
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(55, 1);
						defaultInterpolatedStringHandler.AppendLiteral("Unable to un-register factory of type ");
						defaultInterpolatedStringHandler.AppendFormatted(scanResultFactory.GetType());
						defaultInterpolatedStringHandler.AppendLiteral(" (not supported).");
						throw new ArgumentException(defaultInterpolatedStringHandler.ToStringAndClear(), "scanResultFactory");
					}
					_scanResultFactoryManufacturerIdDict.TryRemove(bleScanResultFactoryManufacturerId.BleScanResultKey);
				}
				else
				{
					_scanResultFactoryPrimaryServiceDict.TryRemove(bleScanResultFactoryPrimaryService.BleScanResultKey);
				}
			}
			else
			{
				_scanResultFactoryDeviceIdDict.TryRemove(bleScanResultFactoryDeviceId.BleScanResultKey);
			}
		}

		internal bool TryMakeScanResult(IDevice bleDevice, out IBleScanResult? scanResult)
		{
			try
			{
				if (MakeScanResultFromDeviceId(bleDevice, out scanResult))
				{
					return true;
				}
				if (MakeScanResultFromPrimaryServiceGuid(bleDevice, out scanResult))
				{
					return true;
				}
				if (MakeScanResultFromManufacturerId(bleDevice, out scanResult))
				{
					return true;
				}
				scanResult = null;
				return false;
			}
			catch (BleScannerScanResultParseException)
			{
				scanResult = null;
				return false;
			}
			catch (Exception ex2)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(62, 3);
				defaultInterpolatedStringHandler.AppendLiteral("Could not create scan result for BLE device ");
				defaultInterpolatedStringHandler.AppendFormatted(bleDevice.Id);
				defaultInterpolatedStringHandler.AppendLiteral(" with exception: ");
				defaultInterpolatedStringHandler.AppendFormatted(ex2.Message);
				defaultInterpolatedStringHandler.AppendLiteral("\n");
				defaultInterpolatedStringHandler.AppendFormatted(ex2.StackTrace);
				TaggedLog.Warning("BleScanResultFactoryRegistry", defaultInterpolatedStringHandler.ToStringAndClear());
				scanResult = null;
				return false;
			}
		}

		private bool MakeScanResultFromDeviceId(IDevice bleDevice, out IBleScanResult? scanResult)
		{
			if (_scanResultFactoryDeviceIdDict.TryGetValue(bleDevice.Id, out var bleScanResultFactoryDeviceId))
			{
				scanResult = bleScanResultFactoryDeviceId.MakeBleScanResult(bleDevice.Id, bleDevice.Name ?? string.Empty, bleDevice.Rssi, bleDevice.AdvertisementRecords);
				return scanResult != null;
			}
			scanResult = null;
			return false;
		}

		private bool MakeScanResultFromPrimaryServiceGuid(IDevice bleDevice, out IBleScanResult? scanResult)
		{
			if (_scanResultFactoryPrimaryServiceDict.Count > 0)
			{
				Guid? guid = bleDevice.AdvertisementRecords.TryGetPrimaryServiceGuid();
				if (guid.HasValue && _scanResultFactoryPrimaryServiceDict.TryGetValue(guid.Value, out var bleScanResultFactoryPrimaryService))
				{
					scanResult = bleScanResultFactoryPrimaryService.MakeBleScanResult(bleDevice.Id, bleDevice.Name ?? string.Empty, bleDevice.Rssi, bleDevice.AdvertisementRecords);
					return scanResult != null;
				}
			}
			scanResult = null;
			return false;
		}

		private bool MakeScanResultFromManufacturerId(IDevice bleDevice, out IBleScanResult? scanResult)
		{
			scanResult = null;
			if (_scanResultFactoryManufacturerIdDict.Count <= 0)
			{
				return false;
			}
			ushort? firstManufacturerId = bleDevice.AdvertisementRecords.GetFirstManufacturerId();
			if (!firstManufacturerId.HasValue || !_scanResultFactoryManufacturerIdDict.TryGetValue(firstManufacturerId.Value, out var bleScanResultFactoryManufacturerId))
			{
				return false;
			}
			scanResult = bleScanResultFactoryManufacturerId.MakeBleScanResult(bleDevice.Id, bleDevice.Name ?? string.Empty, bleDevice.Rssi, bleDevice.AdvertisementRecords);
			return scanResult != null;
		}

		public bool IsPrimaryServiceGuidRegistered(Guid? guid)
		{
			if (guid.HasValue)
			{
				return _scanResultFactoryPrimaryServiceDict.ContainsKey(guid.Value);
			}
			return false;
		}
	}
}
