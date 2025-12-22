using System;
using IDS.Core;

namespace IDS.Portable.LogicalDevice.Extensions
{
	public static class IPhysicalAddress
	{
		public static bool Equals(this Comm.PhysicalAddress address, Comm.PhysicalAddress other)
		{
			return address.CompareTo(other) == 0;
		}

		public static ulong ToLong(this Comm.PhysicalAddress address)
		{
			return address;
		}

		public static Comm.PhysicalAddress ToPhysicalAddress(this long value)
		{
			return new Comm.PhysicalAddress(BitConverter.GetBytes(value));
		}
	}
}
