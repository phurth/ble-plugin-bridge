using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceAlert : IJsonSerializerClass
	{
		string Name { get; }

		bool IsActive { get; }

		int? Count { get; }
	}
}
