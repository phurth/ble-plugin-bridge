using System.Collections.Generic;
using IDS.Portable.LogicalDevice;

namespace OneControl.Direct.MyRvLink
{
	public interface IMyRvLinkDevice
	{
		MyRvLinkDeviceProtocol Protocol { get; }

		byte EncodeSize { get; }

		int EncodeIntoBuffer(byte[] buffer, int offset);

		IEnumerable<ILogicalDevice> FindLogicalDevicesMatchingPhysicalHardware(ILogicalDeviceService deviceService);
	}
}
