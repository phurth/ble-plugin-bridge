using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class ProductDTCsChangedEvent : Event
	{
		public readonly IRemoteProduct Product;

		public IProductDTC DTC { get; private set; }

		public ProductDTCsChangedEvent(IRemoteProduct product)
			: base(product.Adapter)
		{
			Product = product;
		}

		public void Publish(IProductDTC dtc)
		{
			DTC = dtc;
			Publish();
		}
	}
}
