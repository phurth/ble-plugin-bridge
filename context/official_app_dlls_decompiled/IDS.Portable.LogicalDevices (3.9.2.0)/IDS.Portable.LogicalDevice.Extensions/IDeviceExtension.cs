using IDS.Core.IDS_CAN;

namespace IDS.Portable.LogicalDevice.Extensions
{
	public static class IDeviceExtension
	{
		public static bool IsValid(this IDevice instance)
		{
			if (instance.ProductID.IsValid)
			{
				return instance.DeviceType.IsValid;
			}
			return false;
		}
	}
}
