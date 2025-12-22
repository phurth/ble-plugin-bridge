using System.Collections.Generic;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice.Json
{
	public interface ILogicalDeviceStatusExtendedSerializable : IJsonSerializerClass
	{
		IReadOnlyDictionary<byte, byte[]> MakeRawDataExtended();
	}
}
