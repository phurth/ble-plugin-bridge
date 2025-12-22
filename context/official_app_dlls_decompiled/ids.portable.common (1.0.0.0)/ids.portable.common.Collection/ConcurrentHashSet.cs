using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ids.portable.common.Collection
{
	public class ConcurrentHashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable
	{
		private readonly ConcurrentDictionary<T, byte> _collection = new ConcurrentDictionary<T, byte>();

		public int Count => _collection.Count;

		public bool IsReadOnly => false;

		public bool Contains(T item)
		{
			return _collection.ContainsKey(item);
		}

		public void Clear()
		{
			_collection.Clear();
		}

		public void Add(T item)
		{
			_collection.AddOrUpdate(item, 0, (T k, byte v) => 0);
		}

		public bool Remove(T item)
		{
			byte b;
			return _collection.TryRemove(item, out b);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _collection.Keys.GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _collection.Keys.GetEnumerator();
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_collection.Keys.CopyTo(array, arrayIndex);
		}
	}
}
