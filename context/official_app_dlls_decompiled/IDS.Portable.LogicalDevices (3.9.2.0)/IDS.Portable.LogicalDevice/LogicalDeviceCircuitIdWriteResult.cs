namespace IDS.Portable.LogicalDevice
{
	public enum LogicalDeviceCircuitIdWriteResult
	{
		Completed,
		Preempted,
		PreemptedWithSameValue,
		CancelledViaDispose,
		Cancelled,
		Failed
	}
}
