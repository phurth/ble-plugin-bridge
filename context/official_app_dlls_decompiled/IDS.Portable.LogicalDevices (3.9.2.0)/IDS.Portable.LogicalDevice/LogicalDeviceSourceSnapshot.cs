using System;
using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	[JsonObject(MemberSerialization.OptIn)]
	public readonly struct LogicalDeviceSourceSnapshot : IEquatable<LogicalDeviceSourceSnapshot>
	{
		[JsonProperty]
		public string Token { get; }

		[JsonProperty]
		public string Name { get; }

		[JsonConstructor]
		public LogicalDeviceSourceSnapshot(string token, string name)
		{
			Token = token;
			Name = name;
		}

		public bool Equals(LogicalDeviceSourceSnapshot other)
		{
			if (string.Equals(Token, other.Token))
			{
				return Name.Equals(other.Name);
			}
			return false;
		}
	}
}
