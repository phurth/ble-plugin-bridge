using IDS.Core.Events;

namespace IDS.Core.IDS_CAN
{
	public class ProductListChangedEvent : Event
	{
		public readonly IProductManager Products;

		public ProductListChangedEvent(IProductManager products)
			: base(products.Adapter)
		{
			Products = products;
		}
	}
}
