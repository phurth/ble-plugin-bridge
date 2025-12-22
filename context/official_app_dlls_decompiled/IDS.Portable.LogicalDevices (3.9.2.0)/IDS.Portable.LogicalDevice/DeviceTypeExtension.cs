using System;
using System.Collections.Generic;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public static class DeviceTypeExtension
	{
		public static DEVICE_TYPE ToDeviceType(this DeviceType deviceType)
		{
			return (byte)deviceType;
		}

		public static DeviceType ToDeviceType(this DEVICE_TYPE deviceType)
		{
			return (DeviceType)(byte)deviceType;
		}

		public static bool ValidateDeviceType(bool verbose)
		{
			List<string> list = new List<string>();
			foreach (DEVICE_TYPE item in DEVICE_TYPE.GetEnumerator())
			{
				if (!Enum.IsDefined(typeof(DeviceType), (byte)item))
				{
					list.Add(string.Format("    {0}({1:X2}) is missing definition in {2}", item, item.Value, "DeviceType"));
				}
			}
			if (list.Count > 0)
			{
				if (verbose)
				{
					TaggedLog.Error("FunctionNameExtension", $"\n**** MISSING DeviceType DEFINITION START ({list.Count}) ****");
					foreach (string item2 in list)
					{
						TaggedLog.Error("FunctionNameExtension", item2);
					}
					TaggedLog.Error("FunctionNameExtension", "\n**** MISSING DeviceType DEFINITION END ****\n");
				}
				else
				{
					TaggedLog.Error("FunctionNameExtension", $"\n**** MISSING DeviceType DEFINITION ({list.Count}) ****");
				}
			}
			return list.Count == 0;
		}
	}
}
