using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IDS.Core.Events
{
	internal class SubscriptionList : Disposable
	{
		public readonly EventPublisher Publisher;

		public readonly Type EventType;

		private List<Subscription> InUse = new List<Subscription>();

		private List<Subscription> Free = new List<Subscription>();

		private ConcurrentQueue<Subscription> NewItems = new ConcurrentQueue<Subscription>();

		private int Enumerating;

		private int Purging;

		public int Count => InUse.Count + NewItems.Count;

		public bool IsEmpty => Count <= 0;

		public SubscriptionList(EventPublisher publisher, Type event_type)
		{
			Publisher = publisher;
			EventType = event_type;
		}

		public void Clear()
		{
			lock (this)
			{
				InUse.Clear();
				Free.Clear();
			}
			Subscription subscription;
			while (NewItems.TryDequeue(out subscription))
			{
			}
		}

		public void Add(Subscription subscription)
		{
			if (!base.IsDisposed && subscription != null)
			{
				NewItems.Enqueue(subscription);
			}
		}

		private void AddNewSubscriptionsFromQueue()
		{
			if (Enumerating != 0 || NewItems.IsEmpty)
			{
				return;
			}
			Subscription subscription;
			while (NewItems.TryDequeue(out subscription))
			{
				if (subscription != null && subscription.IsAlive && !subscription.IsDisposed)
				{
					InUse.Add(subscription);
				}
			}
		}

		public Subscription GetNextSubscription(ref int index)
		{
			lock (this)
			{
				if (Enumerating == 0)
				{
					AddNewSubscriptionsFromQueue();
				}
				int count = InUse.Count;
				if (index < 0 || index >= count)
				{
					index = 0;
				}
				if (index < count)
				{
					return InUse[index++];
				}
				return null;
			}
		}

		public void Publish(Event e)
		{
			if (base.IsDisposed || IsEmpty)
			{
				return;
			}
			if (e == null)
			{
				throw new ArgumentNullException("e");
			}
			bool flag = true;
			lock (this)
			{
				if (Enumerating == 0)
				{
					AddNewSubscriptionsFromQueue();
				}
				Interlocked.Increment(ref Enumerating);
				try
				{
					foreach (Subscription item in InUse)
					{
						flag &= item.Invoke(e);
					}
				}
				finally
				{
					Interlocked.Decrement(ref Enumerating);
				}
			}
			if (!flag)
			{
				RequestPurge();
			}
		}

		public void RequestPurge()
		{
			if (base.IsDisposed || InUse.Count <= 0 || Interlocked.Exchange(ref Purging, 1) != 0)
			{
				return;
			}
			Task.Run(delegate
			{
				Interlocked.Exchange(ref Purging, 1);
				try
				{
					lock (this)
					{
						Free.Clear();
						foreach (Subscription item in InUse)
						{
							if (item != null && item.IsAlive && !item.IsDisposed)
							{
								Free.Add(item);
							}
						}
						Free = Interlocked.Exchange(ref InUse, Free);
						Free.Clear();
						AddNewSubscriptionsFromQueue();
					}
				}
				finally
				{
					Interlocked.Exchange(ref Purging, 0);
				}
			});
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Clear();
			}
		}
	}
}
