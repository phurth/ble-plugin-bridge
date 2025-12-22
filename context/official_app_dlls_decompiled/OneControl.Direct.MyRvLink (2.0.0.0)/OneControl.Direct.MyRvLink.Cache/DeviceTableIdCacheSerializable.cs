using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using IDS.Portable.Common;
using ids.portable.common.Extensions;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;
using OneControl.Direct.MyRvLink.Devices;

namespace OneControl.Direct.MyRvLink.Cache
{
	[JsonObject(MemberSerialization.OptIn)]
	public class DeviceTableIdCacheSerializable : JsonSerializable<DeviceTableIdCacheSerializable>
	{
		public const string LogTag = "DeviceTableIdCacheSerializable";

		public static string BaseFilename = "MyRvLinkDeviceTableIdCache";

		public static string BaseFilenameExtension = "json";

		private const string PrefixFilename = "V1";

		[JsonProperty]
		public string DeviceSourceToken { get; }

		[JsonProperty]
		protected ConcurrentDictionary<byte, MyRvLinkDeviceTableSerializable> DeviceTableDictionary { get; }

		private static string FilenamePattern => BaseFilename + "V1*." + BaseFilenameExtension;

		public DeviceTableIdCacheSerializable(string deviceSourceToken)
			: this(deviceSourceToken, new ConcurrentDictionary<byte, MyRvLinkDeviceTableSerializable>())
		{
		}

		[JsonConstructor]
		public DeviceTableIdCacheSerializable(string deviceSourceToken, ConcurrentDictionary<byte, MyRvLinkDeviceTableSerializable> deviceTableDictionary)
		{
			DeviceSourceToken = deviceSourceToken ?? "Unknown";
			DeviceTableDictionary = deviceTableDictionary ?? new ConcurrentDictionary<byte, MyRvLinkDeviceTableSerializable>();
		}

		public MyRvLinkDeviceTableSerializable? GetDeviceTableIdSerializableForTableId(byte deviceTableId)
		{
			return DeviceTableDictionary.TryGetValue(deviceTableId);
		}

		public MyRvLinkDeviceTableSerializable? GetFirstDeviceTableIdSerializableForCrc(uint deviceTableCrc)
		{
			foreach (KeyValuePair<byte, MyRvLinkDeviceTableSerializable> item in DeviceTableDictionary)
			{
				if (item.Value.DeviceTableCrc == deviceTableCrc)
				{
					return item.Value;
				}
			}
			return null;
		}

		public void Update(byte deviceTableId, MyRvLinkDeviceTableSerializable deviceTableSerializable)
		{
			DeviceTableDictionary[deviceTableId] = deviceTableSerializable;
		}

		private static string MakeFilename(string deviceSourceToken)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 4);
			defaultInterpolatedStringHandler.AppendFormatted(BaseFilename);
			defaultInterpolatedStringHandler.AppendFormatted("V1");
			defaultInterpolatedStringHandler.AppendLiteral("_");
			defaultInterpolatedStringHandler.AppendFormatted(deviceSourceToken);
			defaultInterpolatedStringHandler.AppendLiteral(".");
			defaultInterpolatedStringHandler.AppendFormatted(BaseFilenameExtension);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		public async Task<bool> TrySaveAsync()
		{
			string filename = MakeFilename(DeviceSourceToken);
			try
			{
				TaggedLog.Information("DeviceTableIdCacheSerializable", "Saving MyRvLink Device Table " + filename);
				string text = JsonConvert.SerializeObject(this, Formatting.Indented);
				await filename.SaveTextAsync(text);
				return true;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("DeviceTableIdCacheSerializable", "Unable to save MyRvLink Device Table " + filename + ": " + ex.Message);
				return false;
			}
		}

		public static async Task<DeviceTableIdCacheSerializable?> TryLoadAsync(string deviceSourceToken)
		{
			string filename = MakeFilename(deviceSourceToken);
			try
			{
				string value = await filename.LoadTextAsync();
				if (string.IsNullOrWhiteSpace(value))
				{
					throw new Exception("json is null or empty");
				}
				TaggedLog.Information("DeviceTableIdCacheSerializable", "Loaded MyRvLink Device Table " + filename);
				DeviceTableIdCacheSerializable deviceTableIdCacheSerializable = JsonConvert.DeserializeObject<DeviceTableIdCacheSerializable>(value);
				if (deviceTableIdCacheSerializable.DeviceSourceToken != deviceSourceToken)
				{
					throw new Exception("Device source tokens don't match " + deviceTableIdCacheSerializable.DeviceSourceToken + " != " + deviceSourceToken);
				}
				return deviceTableIdCacheSerializable;
			}
			catch (FileNotFoundException)
			{
				return null;
			}
			catch (Exception ex2)
			{
				TaggedLog.Warning("DeviceTableIdCacheSerializable", "Unable to load MyRvLink Device Table: " + ex2.Message);
				return null;
			}
		}
	}
}
