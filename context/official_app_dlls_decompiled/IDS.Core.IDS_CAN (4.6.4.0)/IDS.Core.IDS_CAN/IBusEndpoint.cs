namespace IDS.Core.IDS_CAN
{
	public interface IBusEndpoint
	{
		IAdapter Adapter { get; }

		ADDRESS Address { get; }

		bool IsOnline { get; }
	}
}
