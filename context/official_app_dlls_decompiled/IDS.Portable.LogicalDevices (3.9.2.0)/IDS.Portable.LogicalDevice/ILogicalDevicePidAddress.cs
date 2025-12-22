using System;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidAddress : ILogicalDevicePid
	{
		PidAccess PidAccess { get; }
	}
	public interface ILogicalDevicePidAddress<TPidAddress> : ILogicalDevicePidAddress, ILogicalDevicePid where TPidAddress : Enum, IConvertible
	{
		TPidAddress PidAddress { get; }
	}
	public interface ILogicalDevicePidAddress<TValue, TPidAddress> : ILogicalDevicePidAddressValue<TValue>, ILogicalDevicePidAddress, ILogicalDevicePid, ILogicalDevicePid<TValue>, ILogicalDevicePidAddress<TPidAddress> where TPidAddress : Enum, IConvertible
	{
	}
}
