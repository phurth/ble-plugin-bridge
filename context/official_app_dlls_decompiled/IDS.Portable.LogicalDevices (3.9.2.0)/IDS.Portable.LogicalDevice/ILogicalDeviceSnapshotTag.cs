using System;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSnapshotTag : ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
	}
}
