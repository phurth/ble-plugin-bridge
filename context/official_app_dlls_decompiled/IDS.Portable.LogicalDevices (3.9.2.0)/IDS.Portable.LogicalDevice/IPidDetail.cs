using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	public interface IPidDetail<in TValueMax>
	{
		Pid Pid { get; }

		PidCategory Category { get; }

		PidUnits Units { get; }

		bool Deprecated { get; }

		bool IsUndefinedPid { get; }

		string FriendlyName { get; }

		int MinimumBytes { get; }

		int DecimalPlacesOfInterest { get; }

		LogicalDeviceSessionType? WriteSession { get; }

		PidValueCheck CheckValue(TValueMax value, IDevicePID? canPid = null);

		string FormattedValue(TValueMax rawPidValue, IDevicePID? canPid = null);
	}
	public interface IPidDetail : IPidDetail<ulong>
	{
	}
}
