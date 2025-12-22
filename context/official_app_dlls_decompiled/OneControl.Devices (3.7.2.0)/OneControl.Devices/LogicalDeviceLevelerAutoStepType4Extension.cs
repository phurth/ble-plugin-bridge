using System;
using IDS.Portable.Common.Extensions;

namespace OneControl.Devices
{
	public static class LogicalDeviceLevelerAutoStepType4Extension
	{
		public static string TryGetUserVisibleTextEnglish(this LogicalDeviceLevelerAutoStepType4 autoStep)
		{
			if (!Enum.IsDefined(typeof(LogicalDeviceLevelerAutoStepType4), autoStep))
			{
				return null;
			}
			if (!autoStep.TryGetDescription(out var description))
			{
				return null;
			}
			return description;
		}
	}
}
