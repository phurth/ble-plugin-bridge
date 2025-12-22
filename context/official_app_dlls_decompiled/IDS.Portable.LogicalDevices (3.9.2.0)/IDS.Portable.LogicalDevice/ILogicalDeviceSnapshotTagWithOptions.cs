using System;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSnapshotTagWithOptions : ILogicalDeviceSnapshotTag, ILogicalDeviceTag, IEquatable<ILogicalDeviceTag>, IJsonSerializerClass
	{
		LogicalDeviceSnapshotDeserializeTagOption DeserializeTagOption { get; }
	}
}
