using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.Manifest;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceExManifest : LogicalDeviceExBase<ILogicalDevice>
	{
		public static IManifestLogReceiver? LogReceiver;

		private Func<ILogicalDevice, bool>? _deviceFilter;

		private bool _enableDtc;

		private const int WaitTimeForManifestGenerationMs = 10000;

		private const int WaitTimeForManifestReGenerationMs = 60000;

		private const int ManifestGenerationTimeoutMs = 4000;

		private const int DtcGenerationTimeoutMs = 10000;

		private readonly Watchdog _manifestGenerationWatchdog;

		private readonly Watchdog _manifestReGenerationWatchdog;

		private bool _needToRegenerateDeviceManifest = true;

		private int _generatingManifest;

		private IManifest? _manifest;

		private readonly Dictionary<ILogicalDeviceProduct, IReadOnlyDictionary<DTC_ID, DtcValue>> _cachedProductDtcDict = new Dictionary<ILogicalDeviceProduct, IReadOnlyDictionary<DTC_ID, DtcValue>>();

		public const string DeviceManifestFilename = "DeviceManifestV1.json";

		protected override string LogTag => "LogicalDeviceExManifest";

		public static LogicalDeviceExManifest? SharedExtension => LogicalDeviceExBase<ILogicalDevice>.GetSharedExtension<LogicalDeviceExManifest>(autoCreate: true);

		public bool AutoSaveManifest { get; set; }

		public Func<ILogicalDevice, bool>? DeviceFilter
		{
			get
			{
				return _deviceFilter;
			}
			set
			{
				_deviceFilter = value;
				_needToRegenerateDeviceManifest = value != null;
				if (_needToRegenerateDeviceManifest)
				{
					_manifestGenerationWatchdog.TryPet(autoReset: true);
				}
			}
		}

		public bool EnableDtc
		{
			get
			{
				return _enableDtc;
			}
			set
			{
				_enableDtc = value;
				if (_enableDtc && DeviceFilter != null)
				{
					_needToRegenerateDeviceManifest = true;
					_manifestGenerationWatchdog.TryPet(autoReset: true);
				}
			}
		}

		public LogicalDeviceExManifest()
		{
			_manifestGenerationWatchdog = new Watchdog(10000, GenerateManifest, autoStartOnFirstPet: true);
			_manifestReGenerationWatchdog = new Watchdog(60000, GenerateManifest, autoStartOnFirstPet: true);
		}

		public static ILogicalDeviceEx? LogicalDeviceExFactory(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExBase<ILogicalDevice>.LogicalDeviceExFactory<LogicalDeviceExManifest>(logicalDevice, GetLogicalDeviceScope);
		}

		protected static LogicalDeviceExScope GetLogicalDeviceScope(ILogicalDevice logicalDevice)
		{
			return LogicalDeviceExScope.Shared;
		}

		public override void LogicalDeviceOnlineChanged(ILogicalDevice logicalDevice)
		{
			base.LogicalDeviceOnlineChanged(logicalDevice);
			LogicalDeviceActiveConnection activeConnection = logicalDevice.ActiveConnection;
			if ((activeConnection == LogicalDeviceActiveConnection.Direct || activeConnection == LogicalDeviceActiveConnection.Cloud) ? true : false)
			{
				_needToRegenerateDeviceManifest = true;
			}
			_manifestGenerationWatchdog.TryPet(autoReset: true);
		}

		private void GenerateManifest()
		{
			if (Interlocked.Exchange(ref _generatingManifest, 1) != 1)
			{
				Task.Run(async delegate
				{
					await GenerateManifestAsync();
					_generatingManifest = 0;
				});
			}
		}

		private async Task GenerateManifestAsync()
		{
			_ = 2;
			try
			{
				IManifestLogReceiver logReceiver = LogReceiver;
				if (logReceiver == null)
				{
					TaggedLog.Debug(LogTag, "Generate Manifest/DTC SKIPPED because no Log Receiver");
					return;
				}
				Func<ILogicalDevice, bool> deviceFilter = DeviceFilter;
				if (deviceFilter == null)
				{
					TaggedLog.Debug(LogTag, "Generate Manifest/DTC SKIPPED because no device filter setup");
					return;
				}
				List<ILogicalDevice> devicesInManifest2 = Enumerable.ToList(Enumerable.Where(GetAttachedLogicalDevices(), (ILogicalDevice ld) => deviceFilter(ld)));
				if (devicesInManifest2.Count == 0)
				{
					TaggedLog.Debug(LogTag, "Generate Manifest/DTC SKIPPED because no devices match current filter.");
					return;
				}
				if (Enumerable.FirstOrDefault(devicesInManifest2)?.DeviceService.FirmwareUpdateManager.IsFirmwareUpdateSessionStarted ?? false)
				{
					TaggedLog.Information(LogTag, "Skipping manifest generation because firmware update is in process");
					_manifestGenerationWatchdog.TryPet(autoReset: true);
					return;
				}
				devicesInManifest2 = Enumerable.ToList(Enumerable.ThenBy(Enumerable.ThenBy(Enumerable.OrderBy(devicesInManifest2, (ILogicalDevice logicalDevice) => logicalDevice.LogicalId.ProductMacAddress), (ILogicalDevice logicalDevice) => logicalDevice.LogicalId.DeviceType.Value), (ILogicalDevice logicalDevice) => logicalDevice.LogicalId.DeviceInstance));
				if (_needToRegenerateDeviceManifest)
				{
					_manifest = null;
				}
				await ValidateRefreshBootLoaderFirmwareDataAsync(devicesInManifest2);
				if (devicesInManifest2.Count == 0)
				{
					TaggedLog.Debug(LogTag, "Generate Manifest/DTC SKIPPED because no devices match current filter.");
					return;
				}
				if (_manifest == null)
				{
					TaggedLog.Debug(LogTag, $"Generate Manifest for {devicesInManifest2.Count} devices");
					using CancellationTokenSource cts = new CancellationTokenSource(4000);
					_manifest = await ManifestBuilder.MakeManifestAsync(devicesInManifest2, cts.Token);
					if (_manifest != null && AutoSaveManifest)
					{
						TrySaveManifest(_manifest);
					}
					_needToRegenerateDeviceManifest = false;
					logReceiver.LogManifest(_manifest);
				}
				if (EnableDtc && _manifest != null)
				{
					await GenerateDtcAsync(_manifest, devicesInManifest2);
				}
			}
			catch (Exception arg)
			{
				TaggedLog.Debug(LogTag, $"Unable to generate manifest {arg}");
			}
			finally
			{
				if (EnableDtc && DeviceFilter != null)
				{
					_manifestReGenerationWatchdog.TryPet(autoReset: true);
				}
			}
		}

		private async Task ValidateRefreshBootLoaderFirmwareDataAsync(List<ILogicalDevice> devicesInManifest)
		{
			if (devicesInManifest.Count == 0)
			{
				return;
			}
			for (int i = devicesInManifest.Count - 1; i > -1; i--)
			{
				if (devicesInManifest[i].LogicalId.ProductId == PRODUCT_ID.CAN_RE_FLASH_BOOTLOADER)
				{
					TaggedLog.Warning(LogTag, $"Invalid Product Id \"{devicesInManifest[i].LogicalId.ProductId}\" for the device: {devicesInManifest[i].LogicalId}");
					devicesInManifest.RemoveAt(i);
				}
				else
				{
					string text = await devicesInManifest[i].GetSoftwarePartNumberAsync(CancellationToken.None);
					if (string.IsNullOrWhiteSpace(text))
					{
						TaggedLog.Warning(LogTag, $"Invalid Software Part Number \"{text}\" for the device: {devicesInManifest[i].LogicalId}");
						devicesInManifest.RemoveAt(i);
					}
				}
			}
		}

		private async Task GenerateDtcAsync(IManifest manifest, List<ILogicalDevice> devicesInManifest)
		{
			if (!Enumerable.Any(devicesInManifest, delegate(ILogicalDevice ld)
			{
				LogicalDeviceActiveConnection activeConnection = ld.ActiveConnection;
				return (activeConnection == LogicalDeviceActiveConnection.Direct || activeConnection == LogicalDeviceActiveConnection.Cloud) ? true : false;
			}))
			{
				TaggedLog.Debug(LogTag, $"Generate DTC skipped because none of the {devicesInManifest.Count} devices are online");
				return;
			}
			TaggedLog.Debug(LogTag, $"Generate DTC for {devicesInManifest.Count} devices");
			foreach (IManifestProduct manifestProduct in manifest.Products)
			{
				ILogicalDevice logicalDevice = Enumerable.FirstOrDefault(devicesInManifest, (ILogicalDevice ld) => string.Equals(ld.Product?.MacAddress?.ToString(), manifestProduct.UniqueID));
				if (logicalDevice == null)
				{
					TaggedLog.Warning(LogTag, $"Generate DTC unable to find logical device associated with {manifestProduct} that has a product");
					continue;
				}
				ILogicalDeviceProduct product = logicalDevice.Product;
				if (product == null)
				{
					TaggedLog.Debug(LogTag, $"Generate DTC for {logicalDevice.LogicalId} failed because it has no product");
				}
				else
				{
					if (product.ProductId == PRODUCT_ID.SIMULATED_PRODUCT || (logicalDevice.ActiveConnection != LogicalDeviceActiveConnection.Direct && logicalDevice.ActiveConnection != LogicalDeviceActiveConnection.Cloud))
					{
						continue;
					}
					using CancellationTokenSource cts = new CancellationTokenSource();
					cts.CancelAfter(10000);
					try
					{
						DtcResult dtcResult = MakeIotDtcResultFromDtcList(await product.GetProductDtcDictAsync(LogicalDeviceDtcFilter.ActiveOrStoredDtc, cts.Token), product);
						if (dtcResult.ListType == DtcListType.Current)
						{
							TaggedLog.Debug(LogTag, $"Calling LogCurrentDTCs: {dtcResult.DtcList.Count}");
							LogReceiver?.LogCurrentDTCs(manifestProduct, dtcResult.DtcList);
						}
						else if (dtcResult.ListType == DtcListType.Delta && dtcResult.DtcList.Count > 0)
						{
							TaggedLog.Debug(LogTag, $"Calling LogChangedDTCs: {dtcResult.DtcList.Count}");
							LogReceiver?.LogChangedDTCs(manifestProduct, dtcResult.DtcList);
						}
					}
					catch (Exception ex)
					{
						TaggedLog.Debug(LogTag, "Unable to get DTC so we need to abort the manifest generation: " + ex.Message);
						break;
					}
				}
			}
		}

		private DtcResult MakeIotDtcResultFromDtcList(IReadOnlyDictionary<DTC_ID, DtcValue> dtcDict, ILogicalDeviceProduct product)
		{
			if (product == null || dtcDict.Count == 0)
			{
				return new DtcResult(new List<IManifestDTC>(), DtcListType.None);
			}
			IReadOnlyDictionary<DTC_ID, DtcValue> readOnlyDictionary2;
			if (!_cachedProductDtcDict.ContainsKey(product))
			{
				IReadOnlyDictionary<DTC_ID, DtcValue> readOnlyDictionary = new Dictionary<DTC_ID, DtcValue>(dtcDict.Count);
				readOnlyDictionary2 = readOnlyDictionary;
			}
			else
			{
				readOnlyDictionary2 = _cachedProductDtcDict[product];
			}
			IReadOnlyDictionary<DTC_ID, DtcValue> readOnlyDictionary3 = readOnlyDictionary2;
			Dictionary<DTC_ID, DtcValue> dictionary = new Dictionary<DTC_ID, DtcValue>(dtcDict.Count);
			List<IManifestDTC> list = new List<IManifestDTC>(dtcDict.Count);
			DtcListType dtcListType = ((readOnlyDictionary3.Count == 0) ? DtcListType.Current : DtcListType.Delta);
			foreach (KeyValuePair<DTC_ID, DtcValue> item in dtcDict)
			{
				DTC_ID key = item.Key;
				DtcValue value = item.Value;
				if (!Enum.IsDefined(typeof(DTC_ID), key))
				{
					continue;
				}
				dictionary[key] = value;
				if (dtcListType == DtcListType.Current)
				{
					if (value.IsActive || value.IsStored)
					{
						list.Add(MakeManifestDtc(key, value));
					}
				}
				else if (!readOnlyDictionary3.ContainsKey(key))
				{
					list.Add(MakeManifestDtc(key, value));
				}
				else if (readOnlyDictionary3[key] != value)
				{
					list.Add(MakeManifestDtc(key, value));
				}
			}
			_cachedProductDtcDict[product] = dictionary;
			foreach (IManifestDTC item2 in list)
			{
				TaggedLog.Debug(LogTag, $"DTC Change {product}: {item2.Name} isActive={item2.IsActive} isStored={item2.IsStored}");
			}
			return new DtcResult(list, dtcListType);
		}

		private IManifestDTC MakeManifestDtc(DTC_ID id, IDtcValue dtc)
		{
			DTC_ID typeID = id;
			string name = id.ToString();
			bool isActive = dtc.IsActive;
			bool isStored = dtc.IsStored;
			int powerCyclesCounter = dtc.PowerCyclesCounter;
			return new ManifestDTC((ushort)typeID, name, isActive, isStored, powerCyclesCounter);
		}

		public bool TrySaveManifest(IManifest manifest)
		{
			try
			{
				string text = manifest.ToJSON();
				string text2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "DeviceManifestV1.json");
				File.WriteAllText(text2, text);
				if (!File.Exists(text2))
				{
					throw new FileNotFoundException("Unable to save Manifest file at " + text2);
				}
				TaggedLog.Debug(LogTag, "Saved manifest to DeviceManifestV1.json");
				return true;
			}
			catch (Exception ex)
			{
				TaggedLog.Error(LogTag, "Unable to save Manifest: " + ex.Message);
				return false;
			}
		}
	}
}
