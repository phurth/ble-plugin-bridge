using System;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public interface ISensorConnectionBleLocap : ISensorConnectionBle, ISensorConnection, IComparable, IJsonSerializable, IDirectConnectionSerializable, IDirectConnection, IEndPointConnection, IJsonSerializerClass, IEndPointConnectionBle
	{
		MAC AccessoryMac { get; }

		string SoftwarePartNumber { get; }
	}
}
