using System;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice.Json;

namespace OneControl.Direct.IdsCanAccessoryBle.Connections
{
	public interface ISensorConnection : IComparable, IJsonSerializable, IDirectConnectionSerializable, IDirectConnection, IEndPointConnection, IJsonSerializerClass
	{
	}
}
