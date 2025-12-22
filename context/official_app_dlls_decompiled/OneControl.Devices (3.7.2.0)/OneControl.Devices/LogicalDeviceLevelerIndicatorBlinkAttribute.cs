using System;
using System.Diagnostics;
using IDS.Portable.Common.Extensions;

namespace OneControl.Devices
{
	[AttributeUsage(AttributeTargets.Field)]
	public class LogicalDeviceLevelerIndicatorBlinkAttribute : Attribute
	{
		private const string LogTag = "LogicalDeviceLevelerIndicatorBlinkAttribute";

		public static readonly Stopwatch BlinkTimer = Stopwatch.StartNew();

		public bool InverseBlink;

		public uint BlinkToggleMs { get; }

		public double BlinkToggleDutyCycle { get; }

		public LogicalDeviceLevelerStatusIndicatorBlinkCycle CurrentBlinkCycle => CalculateCurrentBlinkCycle(BlinkToggleMs, BlinkToggleDutyCycle, InverseBlink);

		public static LogicalDeviceLevelerStatusIndicatorBlinkCycle CalculateCurrentBlinkCycle(uint toggleMs, double dutyCycle, bool inverseBlink)
		{
			if (toggleMs == 0)
			{
				return LogicalDeviceLevelerStatusIndicatorBlinkCycle.Unknown;
			}
			if (inverseBlink)
			{
				if ((((double)BlinkTimer.ElapsedMilliseconds / (double)toggleMs).RoundUsingThreshold(dutyCycle) & 1) != 1)
				{
					return LogicalDeviceLevelerStatusIndicatorBlinkCycle.Off;
				}
				return LogicalDeviceLevelerStatusIndicatorBlinkCycle.On;
			}
			if ((((double)BlinkTimer.ElapsedMilliseconds / (double)toggleMs).RoundUsingThreshold(dutyCycle) & 1) != 1)
			{
				return LogicalDeviceLevelerStatusIndicatorBlinkCycle.On;
			}
			return LogicalDeviceLevelerStatusIndicatorBlinkCycle.Off;
		}

		public LogicalDeviceLevelerIndicatorBlinkAttribute(uint blinkToggleMs, double blinkToggleDutyCycle, bool inverseBlink)
		{
			BlinkToggleMs = blinkToggleMs;
			BlinkToggleDutyCycle = blinkToggleDutyCycle;
			InverseBlink = inverseBlink;
		}
	}
}
