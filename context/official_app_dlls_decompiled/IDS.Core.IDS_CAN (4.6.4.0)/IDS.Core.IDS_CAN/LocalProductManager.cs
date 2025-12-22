using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace IDS.Core.IDS_CAN
{
	internal class LocalProductManager : Disposable, ILocalProductManager, IEnumerable<LocalProduct>, IEnumerable
	{
		private Adapter Adapter;

		private ConcurrentDictionary<ulong, LocalProduct> Products = new ConcurrentDictionary<ulong, LocalProduct>();

		public IEnumerator<LocalProduct> GetEnumerator()
		{
			return Products.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public LocalProductManager(Adapter adapter)
		{
			Adapter = adapter;
		}

		public override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			Adapter = null;
			ConcurrentDictionary<ulong, LocalProduct> concurrentDictionary = Interlocked.Exchange(ref Products, null);
			foreach (LocalProduct item in concurrentDictionary?.Values)
			{
				item?.Dispose();
			}
			concurrentDictionary?.Clear();
		}

		public LocalProduct GetProductAtAddress(ADDRESS address)
		{
			using (IEnumerator<LocalProduct> enumerator = GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					LocalProduct current = enumerator.Current;
					if (current?.Address == address)
					{
						return current;
					}
				}
			}
			return null;
		}

		public void Add(LocalProduct product)
		{
			if (product?.Adapter == Adapter)
			{
				Products?.TryAdd(product.GetProductUniqueID(), product);
			}
		}

		public void Remove(LocalProduct product)
		{
			Products?.TryRemove(product.GetProductUniqueID(), out var _);
		}
	}
}
