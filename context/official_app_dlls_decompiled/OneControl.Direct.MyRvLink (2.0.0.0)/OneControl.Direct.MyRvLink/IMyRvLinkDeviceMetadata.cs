namespace OneControl.Direct.MyRvLink
{
	public interface IMyRvLinkDeviceMetadata
	{
		MyRvLinkDeviceProtocol Protocol { get; }

		byte EncodeSize { get; }

		int EncodeIntoBuffer(byte[] buffer, int offset);
	}
}
