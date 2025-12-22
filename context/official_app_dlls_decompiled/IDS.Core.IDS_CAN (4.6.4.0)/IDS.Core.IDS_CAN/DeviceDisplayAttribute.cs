using System;

namespace IDS.Core.IDS_CAN
{
	public class DeviceDisplayAttribute : Attribute
	{
		public string DisplayName { get; }

		public DeviceDisplayAttribute(string displayName = null)
		{
			DisplayName = displayName;
		}
	}
}
