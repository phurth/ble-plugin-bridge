using System;

namespace IDS.Portable.LogicalDevice
{
	[Flags]
	public enum LogicalDeviceSnapshotDeserializeTagOption
	{
		None = 0,
		OverwriteExistingMatchingClass = 1
	}
}
