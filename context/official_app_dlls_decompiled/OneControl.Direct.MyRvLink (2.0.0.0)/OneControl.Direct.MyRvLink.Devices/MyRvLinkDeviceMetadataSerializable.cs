using IDS.Portable.LogicalDevice.Json;
using Newtonsoft.Json;

namespace OneControl.Direct.MyRvLink.Devices
{
	[JsonObject(MemberSerialization.OptIn)]
	public class MyRvLinkDeviceMetadataSerializable
	{
		[JsonProperty]
		[JsonConverter(typeof(ByteArrayJsonHexStringConverter))]
		public byte[] RawData { get; }

		public MyRvLinkDeviceMetadataSerializable(IMyRvLinkDeviceMetadata device)
		{
			RawData = new byte[device.EncodeSize];
			device.EncodeIntoBuffer(RawData, 0);
		}

		[JsonConstructor]
		public MyRvLinkDeviceMetadataSerializable(byte[] rawData)
		{
			RawData = rawData;
		}

		public IMyRvLinkDeviceMetadata TryDecode()
		{
			return MyRvLinkDeviceMetadata.TryDecodeFromRawBuffer(RawData);
		}
	}
}
