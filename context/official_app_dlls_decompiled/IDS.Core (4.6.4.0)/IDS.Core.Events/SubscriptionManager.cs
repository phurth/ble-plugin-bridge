using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace IDS.Core.Events
{
	public class SubscriptionManager : Disposable, IEnumerable<SubscriptionToken>, IEnumerable
	{
		private List<SubscriptionToken> Tokens = new List<SubscriptionToken>();

		public int Count => Tokens.Count;

		public void AddSubscription(SubscriptionToken item)
		{
			if (!base.IsDisposed)
			{
				Tokens.Add(item);
			}
		}

		public bool Contains(SubscriptionToken item)
		{
			return Tokens.Contains(item);
		}

		public IEnumerator<SubscriptionToken> GetEnumerator()
		{
			return Tokens.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Tokens.GetEnumerator();
		}

		public void KillSubscription(SubscriptionToken item)
		{
			if (Tokens.Remove(item))
			{
				item.Dispose();
			}
		}

		public void CancelAllSubscriptions()
		{
			if (Tokens.Count <= 0)
			{
				return;
			}
			foreach (SubscriptionToken item in Interlocked.Exchange(ref Tokens, new List<SubscriptionToken>()))
			{
				item.Dispose();
			}
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				CancelAllSubscriptions();
			}
		}
	}
}
