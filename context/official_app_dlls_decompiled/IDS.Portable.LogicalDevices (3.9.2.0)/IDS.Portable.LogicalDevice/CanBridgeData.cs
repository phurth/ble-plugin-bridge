using System;
using System.Diagnostics;

namespace IDS.Portable.LogicalDevice
{
	public class CanBridgeData : ICanBridgeData, IComparable<ICanBridgeData>, IComparable<CanBridgeData>
	{
		public const int UpdateTimeoutMs = 5000;

		private readonly Stopwatch _timer = Stopwatch.StartNew();

		public string Name { get; private set; }

		public string Address { get; private set; }

		public int Port { get; private set; }

		public string FullName => $"{Name} ({Address}:{Port})";

		public bool IsExpired => _timer.ElapsedMilliseconds > 5000;

		public static CanBridgeData MakeCanBridgeData(string name, string address, int port)
		{
			return new CanBridgeData(name, address, port);
		}

		public CanBridgeData(string name, string address, string port)
		{
			Name = name;
			Address = address;
			if (int.TryParse(port, out var result))
			{
				Port = result;
			}
		}

		public CanBridgeData(string name, string address, int port)
		{
			Name = name;
			Address = address;
			Port = port;
		}

		public override string ToString()
		{
			return Name + " (" + Address.ToString() + ":" + Port + ")";
		}

		public void Update(string name, int port)
		{
			Name = name;
			Port = port;
			_timer.Restart();
		}

		public int CompareTo(CanBridgeData other)
		{
			return CompareTo((ICanBridgeData)other);
		}

		public int CompareTo(ICanBridgeData other)
		{
			return string.CompareOrdinal($"{Name}.{Address}.{Port})", $"{other?.Name}.{other?.Address}.{other?.Port})");
		}
	}
}
