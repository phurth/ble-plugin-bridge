using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using ids.portable.ble.BleManager;
using IDS.Portable.Common;
using Newtonsoft.Json;

namespace ids.portable.ble.Platforms.Shared.BleManager
{
	public class BleManagerConnectionCache : Singleton<BleManagerConnectionCache>
	{
		private const string LogTag = "BleManagerConnectionCache";

		public const string CacheFilename = "BleConnectionCacheV1.json";

		private ConcurrentDictionary<Guid, BleManagerConnectionParametersSerializable> _connectionParameterDict;

		private BleManagerConnectionCache()
		{
			_connectionParameterDict = TryLoad();
		}

		public bool TryGetValue(Guid deviceId, out IBleManagerConnectionParameters connectionParameters)
		{
			if (!_connectionParameterDict.TryGetValue(deviceId, out var bleManagerConnectionParametersSerializable))
			{
				connectionParameters = default(BleManagerConnectionParameters);
				return false;
			}
			connectionParameters = bleManagerConnectionParametersSerializable;
			return true;
		}

		public void CacheConnectionParameters(IBleManagerConnectionParameters connectionParameters)
		{
			BleManagerConnectionParametersSerializable bleManagerConnectionParametersSerializable = new BleManagerConnectionParametersSerializable(connectionParameters);
			_connectionParameterDict.TryAdd(connectionParameters.DeviceId, bleManagerConnectionParametersSerializable);
			TrySave();
		}

		private bool TrySave()
		{
			try
			{
				lock (_connectionParameterDict)
				{
					string text = JsonConvert.SerializeObject(_connectionParameterDict, Formatting.Indented);
					TaggedLog.Debug("BleManagerConnectionCache", text ?? "");
					File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "BleConnectionCacheV1.json"), text);
					return true;
				}
			}
			catch (Exception ex)
			{
				TaggedLog.Error("BleManagerConnectionCache", "Unable to save connection cache BleConnectionCacheV1.json: " + ex.Message);
				return false;
			}
		}

		private ConcurrentDictionary<Guid, BleManagerConnectionParametersSerializable> TryLoad()
		{
			try
			{
				string value = File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "BleConnectionCacheV1.json"));
				if (string.IsNullOrWhiteSpace(value))
				{
					throw new Exception("json is null or empty");
				}
				return new ConcurrentDictionary<Guid, BleManagerConnectionParametersSerializable>(JsonConvert.DeserializeObject<Dictionary<Guid, BleManagerConnectionParametersSerializable>>(value));
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("BleManagerConnectionCache", "Unable to load connection cache BleConnectionCacheV1.json: " + ex.Message);
			}
			return new ConcurrentDictionary<Guid, BleManagerConnectionParametersSerializable>();
		}
	}
}
