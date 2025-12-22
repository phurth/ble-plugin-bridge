using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OneControl.Direct.MyRvLink.Devices
{
	[JsonObject(MemberSerialization.OptIn)]
	public class MyRvLinkDeviceTableSerializable
	{
		public const string LogTag = "MyRvLinkDeviceTableSerializable";

		[JsonProperty]
		public DateTime CreatedTimeStamp { get; }

		[JsonProperty]
		public uint DeviceTableCrc { get; }

		[JsonProperty]
		public IReadOnlyList<MyRvLinkDeviceSerializable> DevicesSerializable { get; }

		[JsonConstructor]
		public MyRvLinkDeviceTableSerializable(uint deviceTableCrc, IReadOnlyList<MyRvLinkDeviceSerializable> devicesSerializable, DateTime createdTimeStamp)
		{
			DeviceTableCrc = deviceTableCrc;
			DevicesSerializable = Enumerable.ToList(devicesSerializable);
			CreatedTimeStamp = createdTimeStamp;
		}

		public IReadOnlyList<IMyRvLinkDevice> TryDecode()
		{
			return Enumerable.ToList(Enumerable.Select(DevicesSerializable, (MyRvLinkDeviceSerializable device) => device.TryDecode()));
		}
	}
}
