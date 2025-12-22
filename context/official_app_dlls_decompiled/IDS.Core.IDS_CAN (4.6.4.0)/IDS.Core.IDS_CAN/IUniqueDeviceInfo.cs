namespace IDS.Core.IDS_CAN
{
	public interface IUniqueDeviceInfo : IUniqueProductInfo
	{
		DEVICE_TYPE DeviceType { get; }

		int DeviceInstance { get; }
	}
}
