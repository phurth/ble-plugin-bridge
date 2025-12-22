using System.Collections.Generic;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalProductStatus
	{
		SoftwareUpdateState SoftwareUpdateStateLastKnown { get; }

		event LogicalDeviceProductStatusChangedEventHandler DeviceProductStatusChanged;

		byte[] CopyRawProductStatus();

		bool UpdateProductStatus(IReadOnlyList<byte> statusData, uint dataLength);

		void OnProductStatusChanged();
	}
}
