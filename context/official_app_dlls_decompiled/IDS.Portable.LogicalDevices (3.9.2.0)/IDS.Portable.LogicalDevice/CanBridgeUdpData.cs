using Newtonsoft.Json;

namespace IDS.Portable.LogicalDevice
{
	public readonly struct CanBridgeUdpData
	{
		public string Mfg { get; }

		public string Product { get; }

		public string Name { get; }

		public string Port { get; }

		[JsonConstructor]
		public CanBridgeUdpData(string mfg, string product, string name, string port)
		{
			Mfg = mfg;
			Product = product;
			Name = name;
			Port = port;
		}

		public override string ToString()
		{
			return Name + ":" + Port;
		}
	}
}
