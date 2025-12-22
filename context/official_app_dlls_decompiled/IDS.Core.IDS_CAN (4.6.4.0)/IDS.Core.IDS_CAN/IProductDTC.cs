namespace IDS.Core.IDS_CAN
{
	public interface IProductDTC
	{
		IRemoteProduct Product { get; }

		DTC_ID ID { get; }

		bool IsActive { get; }

		bool IsStored { get; }

		int PowerCyclesCounter { get; }

		string Name { get; }
	}
}
