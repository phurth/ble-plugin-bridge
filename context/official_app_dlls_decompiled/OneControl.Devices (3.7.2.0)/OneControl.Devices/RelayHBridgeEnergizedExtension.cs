using System;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public static class RelayHBridgeEnergizedExtension
	{
		public static RelayHBridgeEnergized ConvertToRelayEnergized(bool isForward, bool isReverse, ILogicalDeviceId logicalId)
		{
			if (isForward == isReverse)
			{
				return RelayHBridgeEnergized.None;
			}
			FUNCTION_CLASS fUNCTION_CLASS = logicalId?.FunctionClass ?? FUNCTION_CLASS.UNKNOWN;
			if (isReverse)
			{
				switch (fUNCTION_CLASS)
				{
				case FUNCTION_CLASS.DOOR:
					if ((logicalId?.FunctionName?.Name ?? "").IndexOf("ramp", StringComparison.OrdinalIgnoreCase) < 0)
					{
						return RelayHBridgeEnergized.Relay2;
					}
					return RelayHBridgeEnergized.Relay1;
				case FUNCTION_CLASS.STABILIZER:
					return RelayHBridgeEnergized.Relay1;
				case FUNCTION_CLASS.GENERATOR:
					return RelayHBridgeEnergized.Relay1;
				case FUNCTION_CLASS.LANDING_GEAR:
					return RelayHBridgeEnergized.Relay1;
				case FUNCTION_CLASS.VENT_COVER:
					return RelayHBridgeEnergized.Relay1;
				default:
					return RelayHBridgeEnergized.Relay2;
				}
			}
			if (isForward)
			{
				switch (fUNCTION_CLASS)
				{
				case FUNCTION_CLASS.DOOR:
					if ((logicalId?.FunctionName?.Name ?? "").IndexOf("ramp", StringComparison.OrdinalIgnoreCase) < 0)
					{
						return RelayHBridgeEnergized.Relay1;
					}
					return RelayHBridgeEnergized.Relay2;
				case FUNCTION_CLASS.STABILIZER:
					return RelayHBridgeEnergized.Relay2;
				case FUNCTION_CLASS.GENERATOR:
					return RelayHBridgeEnergized.Relay2;
				case FUNCTION_CLASS.LANDING_GEAR:
					return RelayHBridgeEnergized.Relay2;
				case FUNCTION_CLASS.VENT_COVER:
					return RelayHBridgeEnergized.Relay2;
				default:
					return RelayHBridgeEnergized.Relay1;
				}
			}
			return RelayHBridgeEnergized.None;
		}

		public static RelayHBridgeDirectionVerbose ConvertToVerboseDirection(this RelayHBridgeEnergized relayEnergized, ILogicalDeviceId logicalId)
		{
			if (relayEnergized == RelayHBridgeEnergized.None)
			{
				return RelayHBridgeDirectionVerbose.None;
			}
			FUNCTION_CLASS fUNCTION_CLASS = logicalId?.FunctionClass ?? FUNCTION_CLASS.UNKNOWN;
			switch (relayEnergized)
			{
			case RelayHBridgeEnergized.Relay1:
				switch (fUNCTION_CLASS)
				{
				case FUNCTION_CLASS.AWNING:
					return RelayHBridgeDirectionVerbose.Extend;
				case FUNCTION_CLASS.LIFT:
					return RelayHBridgeDirectionVerbose.Raise;
				case FUNCTION_CLASS.DOOR:
					if ((logicalId?.FunctionName?.Name ?? "").IndexOf("ramp", StringComparison.OrdinalIgnoreCase) < 0)
					{
						return RelayHBridgeDirectionVerbose.Lock;
					}
					return RelayHBridgeDirectionVerbose.Open;
				case FUNCTION_CLASS.LOCK:
					return RelayHBridgeDirectionVerbose.Lock;
				case FUNCTION_CLASS.STABILIZER:
					return RelayHBridgeDirectionVerbose.Retract;
				case FUNCTION_CLASS.GENERATOR:
					return RelayHBridgeDirectionVerbose.Stop;
				case FUNCTION_CLASS.LANDING_GEAR:
					return RelayHBridgeDirectionVerbose.Retract;
				case FUNCTION_CLASS.SLIDE:
					return RelayHBridgeDirectionVerbose.Out;
				case FUNCTION_CLASS.VENT_COVER:
					return RelayHBridgeDirectionVerbose.Open;
				default:
					return RelayHBridgeDirectionVerbose.Foward;
				}
			case RelayHBridgeEnergized.Relay2:
				switch (fUNCTION_CLASS)
				{
				case FUNCTION_CLASS.AWNING:
					return RelayHBridgeDirectionVerbose.Retract;
				case FUNCTION_CLASS.LIFT:
					return RelayHBridgeDirectionVerbose.Lower;
				case FUNCTION_CLASS.DOOR:
					if ((logicalId?.FunctionName?.Name ?? "").IndexOf("ramp", StringComparison.OrdinalIgnoreCase) < 0)
					{
						return RelayHBridgeDirectionVerbose.Unlock;
					}
					return RelayHBridgeDirectionVerbose.Close;
				case FUNCTION_CLASS.LOCK:
					return RelayHBridgeDirectionVerbose.Unlock;
				case FUNCTION_CLASS.STABILIZER:
					return RelayHBridgeDirectionVerbose.Extend;
				case FUNCTION_CLASS.GENERATOR:
					return RelayHBridgeDirectionVerbose.Start;
				case FUNCTION_CLASS.LANDING_GEAR:
					return RelayHBridgeDirectionVerbose.Extend;
				case FUNCTION_CLASS.SLIDE:
					return RelayHBridgeDirectionVerbose.In;
				case FUNCTION_CLASS.VENT_COVER:
					return RelayHBridgeDirectionVerbose.Close;
				default:
					return RelayHBridgeDirectionVerbose.Reverse;
				}
			default:
				return RelayHBridgeDirectionVerbose.None;
			}
		}

		public static RelayHBridgeDirection ConvertToDirection(this RelayHBridgeEnergized relayEnergized, ILogicalDeviceId logicalId)
		{
			return RelayHBridgeDirectionExtension.ConvertToHBridgeDirection(relayEnergized == RelayHBridgeEnergized.Relay1, relayEnergized == RelayHBridgeEnergized.Relay2, logicalId);
		}
	}
}
