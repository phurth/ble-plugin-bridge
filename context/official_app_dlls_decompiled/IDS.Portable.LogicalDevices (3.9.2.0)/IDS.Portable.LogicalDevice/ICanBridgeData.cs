using System;

namespace IDS.Portable.LogicalDevice
{
	public interface ICanBridgeData : IComparable<ICanBridgeData>
	{
		string Name { get; }

		string Address { get; }

		int Port { get; }

		bool IsExpired { get; }

		void Update(string name, int port);
	}
}
