using System;

namespace IDS.Core.Events
{
	public class RoundRobinPublisher : Disposable
	{
		private SubscriptionList List;

		private Type EventType;

		private int Index;

		public int SubscriberCount => List.Count;

		internal RoundRobinPublisher(SubscriptionList subscriberList, Type eventType)
		{
			List = subscriberList;
			EventType = eventType;
		}

		public void PublishNext(Event e)
		{
			if (base.IsDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
			if (e != null && !List.IsEmpty)
			{
				Subscription nextSubscription = List.GetNextSubscription(ref Index);
				if (nextSubscription == null || !nextSubscription.Invoke(e))
				{
					List.RequestPurge();
				}
			}
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				List = null;
				EventType = null;
			}
		}
	}
}
