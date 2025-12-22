using System;

namespace IDS.Core.Events
{
	internal sealed class WeakSubscription<T> : Subscription<T> where T : Event
	{
		private readonly WeakReference<Action<T>> wr;

		public sealed override bool IsAlive
		{
			get
			{
				if (!wr.TryGetTarget(out var action))
				{
					return false;
				}
				return action != null;
			}
		}

		public WeakSubscription(EventPublisher publisher, Action<T> action)
			: base(publisher)
		{
			wr = new WeakReference<Action<T>>(action);
		}

		protected sealed override bool TypedInvoke(T e)
		{
			if (!wr.TryGetTarget(out var action))
			{
				return false;
			}
			if (action == null)
			{
				return false;
			}
			action(e);
			return true;
		}
	}
}
