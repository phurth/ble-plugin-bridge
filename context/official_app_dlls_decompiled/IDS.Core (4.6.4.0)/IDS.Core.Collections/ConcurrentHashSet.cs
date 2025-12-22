using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IDS.Core.Collections
{
	public class ConcurrentHashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable
	{
		private readonly ConcurrentDictionary<T, byte> Collection = new ConcurrentDictionary<T, byte>();

		public int Count => Collection.Count;

		public bool IsReadOnly => false;

		public bool Contains(T item)
		{
			return Collection.ContainsKey(item);
		}

		public void Clear()
		{
			Collection.Clear();
		}

		public void Add(T item)
		{
			Collection.AddOrUpdate(item, 0, (T k, byte v) => 0);
		}

		public bool Remove(T item)
		{
			byte b;
			return Collection.TryRemove(item, out b);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Collection.Keys.GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return Collection.Keys.GetEnumerator();
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			Collection.Keys.CopyTo(array, arrayIndex);
		}
	}
}
