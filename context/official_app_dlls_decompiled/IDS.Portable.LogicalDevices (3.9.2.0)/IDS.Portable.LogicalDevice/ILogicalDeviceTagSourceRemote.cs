using System;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceTagSourceRemote : ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
		LogicalDeviceTagSourceRemoteType RemoteType { get; }
	}
}
