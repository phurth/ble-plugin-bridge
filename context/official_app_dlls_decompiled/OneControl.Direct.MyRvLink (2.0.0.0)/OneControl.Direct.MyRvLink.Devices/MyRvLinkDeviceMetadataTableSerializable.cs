using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using IDS.Portable.Common;
using ids.portable.common.Extensions;
using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Direct.MyRvLink.Devices
{
	[JsonObject(MemberSerialization.OptIn)]
	public class MyRvLinkDeviceMetadataTableSerializable : JsonSerializable<MyRvLinkDeviceTableSerializable>
	{
		public const string LogTag = "MyRvLinkDeviceMetadataTableSerializable";

		public static string BaseFilename = "MyRvLinkDeviceTable";

		public static string BaseFilenameExtension = "json";

		private const string PrefixFilename = "MetadataV1";

		public static readonly string SaveFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

		[JsonProperty]
		public uint DeviceMetadataTableCrc { get; }

		[JsonProperty]
		public IReadOnlyList<MyRvLinkDeviceMetadataSerializable> DevicesMetadataSerializable { get; }

		private static string FilenamePattern => BaseFilename + "MetadataV1*." + BaseFilenameExtension;

		[JsonConstructor]
		public MyRvLinkDeviceMetadataTableSerializable(uint deviceMetadataTableCrc, IReadOnlyList<MyRvLinkDeviceMetadataSerializable> devicesMetadataSerializable)
		{
			DeviceMetadataTableCrc = deviceMetadataTableCrc;
			DevicesMetadataSerializable = Enumerable.ToList(devicesMetadataSerializable);
		}

		public IReadOnlyList<IMyRvLinkDeviceMetadata> TryDecode()
		{
			return Enumerable.ToList(Enumerable.Select(DevicesMetadataSerializable, (MyRvLinkDeviceMetadataSerializable device) => device.TryDecode()));
		}

		private static string MakeFilename(string deviceSourceToken, uint deviceTableCrc)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(3, 5);
			defaultInterpolatedStringHandler.AppendFormatted(BaseFilename);
			defaultInterpolatedStringHandler.AppendFormatted("MetadataV1");
			defaultInterpolatedStringHandler.AppendLiteral("_");
			defaultInterpolatedStringHandler.AppendFormatted(deviceSourceToken);
			defaultInterpolatedStringHandler.AppendLiteral("_");
			defaultInterpolatedStringHandler.AppendFormatted(deviceTableCrc, "x8");
			defaultInterpolatedStringHandler.AppendLiteral(".");
			defaultInterpolatedStringHandler.AppendFormatted(BaseFilenameExtension);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		public bool TrySave(string deviceSourceToken)
		{
			string text = MakeFilename(deviceSourceToken, DeviceMetadataTableCrc);
			try
			{
				TaggedLog.Information("MyRvLinkDeviceMetadataTableSerializable", "Saving MyRvLink Device Metadata Table " + text);
				string text2 = JsonConvert.SerializeObject(this, Formatting.Indented);
				text.SaveText(text2);
				return true;
			}
			catch (Exception ex)
			{
				TaggedLog.Error("MyRvLinkDeviceMetadataTableSerializable", "Unable to save MyRvLink Device Metadata Table " + text + ": " + ex.Message);
				return false;
			}
		}

		public static bool TryLoad(string deviceSourceToken, uint deviceMetadataTableCrc, out MyRvLinkDeviceMetadataTableSerializable? deviceMetadataTableSerializable)
		{
			string text = MakeFilename(deviceSourceToken, deviceMetadataTableCrc);
			try
			{
				deviceMetadataTableSerializable = null;
				string value = text.LoadText();
				if (string.IsNullOrWhiteSpace(value))
				{
					throw new Exception("json is null or empty");
				}
				TaggedLog.Information("MyRvLinkDeviceMetadataTableSerializable", "Loaded MyRvLink Device Metadata Table " + text);
				deviceMetadataTableSerializable = JsonConvert.DeserializeObject<MyRvLinkDeviceMetadataTableSerializable>(value);
			}
			catch (FileNotFoundException)
			{
				deviceMetadataTableSerializable = null;
			}
			catch (Exception ex2)
			{
				TaggedLog.Warning("MyRvLinkDeviceMetadataTableSerializable", "Unable to load MyRvLink Device Metadata Table: " + ex2.Message);
				deviceMetadataTableSerializable = null;
			}
			return deviceMetadataTableSerializable != null;
		}

		public static void TryClearCache()
		{
			try
			{
				FileInfo[] files = new DirectoryInfo(SaveFolder).GetFiles(FilenamePattern);
				foreach (FileInfo fileInfo in files)
				{
					TaggedLog.Information("MyRvLinkDeviceMetadataTableSerializable", "Removing MyRvLink Device Metadata Table `" + fileInfo.Name + "`");
					fileInfo.Name.TryDelete();
				}
			}
			catch (Exception ex)
			{
				TaggedLog.Information("MyRvLinkDeviceMetadataTableSerializable", "Unable to copy all MyRvLink Metadata files " + ex.Message);
			}
		}
	}
}
