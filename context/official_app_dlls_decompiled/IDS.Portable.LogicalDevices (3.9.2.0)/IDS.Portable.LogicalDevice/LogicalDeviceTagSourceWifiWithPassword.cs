using System;
using System.Reflection;
using IDS.Portable.Common;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public class LogicalDeviceTagSourceWifiWithPassword : LogicalDeviceTagSourceWifi
	{
		[JsonProperty]
		public string Password { get; }

		public new bool Equals(ILogicalDeviceTag other)
		{
			if (other is LogicalDeviceTagSourceWifiWithPassword logicalDeviceTagSourceWifiWithPassword && string.Equals(base.Ssid, logicalDeviceTagSourceWifiWithPassword.Ssid, StringComparison.Ordinal))
			{
				return string.Equals(Password, logicalDeviceTagSourceWifiWithPassword.Password, StringComparison.Ordinal);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LogicalDeviceTagSourceWifi logicalDeviceTagSourceWifi)
			{
				return string.Equals(base.Ssid, logicalDeviceTagSourceWifi.Ssid, StringComparison.Ordinal);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return base.Ssid?.GetHashCode() ?? 0;
		}

		[JsonConstructor]
		public LogicalDeviceTagSourceWifiWithPassword(string ssid, string password)
			: base(ssid)
		{
			Password = password;
		}

		static LogicalDeviceTagSourceWifiWithPassword()
		{
			Type declaringType = MethodBase.GetCurrentMethod().DeclaringType;
			TypeRegistry.Register(declaringType.Name, declaringType);
		}

		public override string ToString()
		{
			return base.Ssid ?? "";
		}
	}
}
