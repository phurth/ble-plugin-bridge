using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IDS.Portable.Common
{
	public class FixedSizedConcurrentQueue<TValue> : IProducerConsumerCollection<TValue>, IEnumerable<TValue>, IEnumerable, ICollection, IReadOnlyCollection<TValue>
	{
		private readonly ConcurrentQueue<TValue> _queue;

		private readonly object _syncObject = new object();

		public int LimitSize { get; }

		public int Count => _queue.Count;

		bool ICollection.IsSynchronized => ((ICollection)_queue).IsSynchronized;

		object ICollection.SyncRoot => ((ICollection)_queue).SyncRoot;

		public bool IsEmpty => _queue.IsEmpty;

		public FixedSizedConcurrentQueue(int limit)
		{
			_queue = new ConcurrentQueue<TValue>();
			LimitSize = limit;
		}

		public FixedSizedConcurrentQueue(int limit, IEnumerable<TValue> collection)
		{
			_queue = new ConcurrentQueue<TValue>(collection);
			LimitSize = limit;
		}

		public void CopyTo(TValue[] array, int index)
		{
			_queue.CopyTo(array, index);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			((ICollection)_queue).CopyTo(array, index);
		}

		public void Enqueue(TValue obj)
		{
			_queue.Enqueue(obj);
			lock (_syncObject)
			{
				while (_queue.Count > LimitSize)
				{
					_queue.TryDequeue(out var _);
				}
			}
		}

		public IEnumerator<TValue> GetEnumerator()
		{
			return _queue.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<TValue>)this).GetEnumerator();
		}

		public TValue[] ToArray()
		{
			return _queue.ToArray();
		}

		public bool TryAdd(TValue item)
		{
			Enqueue(item);
			return true;
		}

		bool IProducerConsumerCollection<TValue>.TryTake(out TValue item)
		{
			return TryDequeue(out item);
		}

		public bool TryDequeue(out TValue result)
		{
			return _queue.TryDequeue(out result);
		}

		public bool TryPeek(out TValue result)
		{
			return _queue.TryPeek(out result);
		}
	}
}
