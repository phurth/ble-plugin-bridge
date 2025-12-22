using System;
using System.Runtime.CompilerServices;

namespace IDS.Core.Events
{
	internal abstract class Subscription
	{
		private readonly EventPublisher Publisher;

		private readonly Type EventType;

		public bool IsDisposed { get; private set; }

		public abstract bool IsAlive { get; }

		public abstract bool DoInvoke(object e);

		protected Subscription(EventPublisher publisher, Type event_type)
		{
			Publisher = publisher;
			EventType = event_type;
			IsDisposed = false;
		}

		public bool Invoke(object e)
		{
			if (!IsDisposed)
			{
				if (DoInvoke(e))
				{
					return true;
				}
				IsDisposed = true;
			}
			return false;
		}

		public void Dispose()
		{
			if (!IsDisposed)
			{
				IsDisposed = true;
				Publisher.RequestPurge(EventType);
			}
		}
	}
	internal abstract class Subscription<T> : Subscription where T : Event
	{
		protected abstract bool TypedInvoke(T e);

		protected Subscription(EventPublisher publisher)
			: base(publisher, typeof(T))
		{
		}

		public sealed override bool DoInvoke(object e)
		{
			if (e is T e2)
			{
				return TypedInvoke(e2);
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(19, 1);
			defaultInterpolatedStringHandler.AppendLiteral("Unexpected event <");
			defaultInterpolatedStringHandler.AppendFormatted<object>(e);
			defaultInterpolatedStringHandler.AppendLiteral(">");
			throw new Exception(defaultInterpolatedStringHandler.ToStringAndClear());
		}
	}
}
