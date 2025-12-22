using System;
using System.Net.NetworkInformation;
using IDS.Core.IDS_CAN;
using IDS.Core.Types;

namespace IDS.Portable.LogicalDevice
{
	public static class MacExtension
	{
		public static bool Equals(this MAC mac, MAC other)
		{
			return mac.CompareTo(other) == 0;
		}

		public static long ToLong(this MAC mac)
		{
			return (UInt48)mac;
		}

		public static MAC ToMAC(this long value)
		{
			byte[] bytes = BitConverter.GetBytes(value);
			return new MAC(new byte[6]
			{
				bytes[5],
				bytes[4],
				bytes[3],
				bytes[2],
				bytes[1],
				bytes[0]
			});
		}

		public static MAC ToMAC(this string macString)
		{
			macString = macString.Replace(":", string.Empty).Replace(" ", string.Empty).Trim();
			if (string.IsNullOrWhiteSpace(macString))
			{
				throw new ArgumentException("MAC address string cannot be null, empty or contain only white space characters.");
			}
			try
			{
				return new MAC(PhysicalAddress.Parse(macString.ToUpper()).GetAddressBytes());
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Invalid MAC Address " + macString + ": " + ex.Message);
			}
		}
	}
}
