using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice
{
	[AttributeUsage(AttributeTargets.Field)]
	public class FunctionClassExclusiveDevicesAttribute : Attribute
	{
		public readonly ReadOnlyCollection<DEVICE_TYPE> DeviceTypes;

		public FunctionClassExclusiveDevicesAttribute()
			: this(new byte[0])
		{
		}

		public FunctionClassExclusiveDevicesAttribute(params byte[] deviceTypeByteList)
		{
			List<DEVICE_TYPE> list = new List<DEVICE_TYPE>();
			try
			{
				if (deviceTypeByteList != null)
				{
					foreach (byte b in deviceTypeByteList)
					{
						if (!list.Contains(b))
						{
							list.Add(b);
						}
					}
				}
			}
			catch
			{
			}
			if (list.Count == 0)
			{
				list.Add((byte)0);
			}
			DeviceTypes = new ReadOnlyCollection<DEVICE_TYPE>(list);
		}
	}
}
