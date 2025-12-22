namespace IDS.Core.IDS_CAN
{
	public static class ProductExtensions
	{
		public static ulong GetProductUniqueID(this IUniqueProductInfo product)
		{
			return ((((((((((((ulong)product.MAC[0] << 8) | product.MAC[1]) << 8) | product.MAC[2]) << 8) | product.MAC[3]) << 8) | product.MAC[4]) << 8) | product.MAC[5]) << 16) | (ushort)product.ProductID;
		}
	}
}
