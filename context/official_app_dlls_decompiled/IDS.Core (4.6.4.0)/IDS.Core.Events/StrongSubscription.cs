using System;

namespace IDS.Core.Events
{
	internal sealed class StrongSubscription<T> : Subscription<T> where T : Event
	{
		private readonly Action<T> Action;

		public sealed override bool IsAlive => true;

		public StrongSubscription(EventPublisher publisher, Action<T> action)
			: base(publisher)
		{
			Action = action;
		}

		protected sealed override bool TypedInvoke(T e)
		{
			Action?.Invoke(e);
			return true;
		}
	}
}
