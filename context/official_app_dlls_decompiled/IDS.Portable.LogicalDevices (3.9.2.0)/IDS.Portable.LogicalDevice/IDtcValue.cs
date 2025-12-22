namespace IDS.Portable.LogicalDevice
{
	public interface IDtcValue
	{
		bool IsActive { get; }

		bool IsStored { get; }

		byte PowerCyclesCounter { get; }
	}
}
