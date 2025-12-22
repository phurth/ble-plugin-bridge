using IDS.Core.IDS_CAN;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public interface IMyRvLinkDeviceForLogicalDevice : IMyRvLinkDevice
	{
		int DeviceInstance { get; }

		DEVICE_TYPE DeviceType { get; }

		PRODUCT_ID ProductId { get; }

		MAC ProductMacAddress { get; }

		ILogicalDeviceId? LogicalDeviceId { get; }

		byte RawDefaultCapability { get; }
	}
}
