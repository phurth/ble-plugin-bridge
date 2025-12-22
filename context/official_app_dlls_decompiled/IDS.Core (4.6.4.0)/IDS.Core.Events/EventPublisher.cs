using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace IDS.Core.Events
{
	public class EventPublisher : Disposable, IEventPublisher, IDisposable, System.IDisposable
	{
		private readonly ConcurrentDictionary<Type, SubscriptionList> SubscriptionLists = new ConcurrentDictionary<Type, SubscriptionList>();

		public readonly string Name;

		public EventPublisher(string name)
		{
			Name = name;
		}

		private SubscriptionList GetSubscriptionList(Type eventType)
		{
			SubscriptionLists.TryGetValue(eventType, out var result);
			return result;
		}

		internal SubscriptionList GetValidSubscriptionList(Type eventType)
		{
			SubscriptionList subscriptionList = GetSubscriptionList(eventType);
			if (subscriptionList != null)
			{
				return subscriptionList;
			}
			if (base.IsDisposed)
			{
				return null;
			}
			return SubscriptionLists.GetOrAdd(eventType, (Type type) => new SubscriptionList(this, eventType));
		}

		public RoundRobinPublisher CreateRoundRobinPublisher<T>() where T : Event
		{
			if (base.IsDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
			return new RoundRobinPublisher(GetValidSubscriptionList(typeof(T)), typeof(T));
		}

		public void Subscribe<T>(Action<T> deliveryAction, SubscriptionType type, SubscriptionManager manager) where T : Event
		{
			manager?.AddSubscription(Subscribe(deliveryAction, type));
		}

		public SubscriptionToken Subscribe<T>(Action<T> deliveryAction, SubscriptionType type) where T : Event
		{
			if (base.IsDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
			if (deliveryAction == null)
			{
				throw new ArgumentNullException("deliveryAction");
			}
			Subscription subscription;
			switch (type)
			{
			case SubscriptionType.Strong:
				subscription = new StrongSubscription<T>(this, deliveryAction);
				break;
			case SubscriptionType.Weak:
				subscription = new WeakSubscription<T>(this, deliveryAction);
				break;
			default:
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(30, 1);
				defaultInterpolatedStringHandler.AppendLiteral("SubscriptionType <");
				defaultInterpolatedStringHandler.AppendFormatted(type);
				defaultInterpolatedStringHandler.AppendLiteral("> unexpected");
				throw new ArgumentOutOfRangeException("type", defaultInterpolatedStringHandler.ToStringAndClear());
			}
			}
			SubscriptionList validSubscriptionList = GetValidSubscriptionList(typeof(T));
			if (validSubscriptionList == null)
			{
				return null;
			}
			validSubscriptionList.Add(subscription);
			return new SubscriptionToken(subscription, deliveryAction);
		}

		public bool HasSubscriptionsFor<T>() where T : Event
		{
			return CountSubscriptionsFor<T>() > 0;
		}

		public int CountSubscriptionsFor<T>() where T : Event
		{
			return GetSubscriptionList(typeof(T))?.Count ?? 0;
		}

		public void Publish<T>(T e) where T : Event
		{
			if (typeof(T) == typeof(Event))
			{
				Publish(e, e.GetType());
			}
			else
			{
				Publish(e, typeof(T));
			}
		}

		public void Publish(Event e)
		{
			Publish(e, e.GetType());
		}

		public void Publish(Event e, Type eventType)
		{
			if (!base.IsDisposed)
			{
				GetSubscriptionList(eventType)?.Publish(e);
			}
		}

		public void RequestPurge(Type eventType)
		{
			if (!base.IsDisposed)
			{
				GetSubscriptionList(eventType)?.RequestPurge();
			}
		}

		public void RequestPurgeAll()
		{
			if (base.IsDisposed)
			{
				return;
			}
			foreach (SubscriptionList value in SubscriptionLists.Values)
			{
				value.RequestPurge();
			}
		}

		public override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			foreach (SubscriptionList value in SubscriptionLists.Values)
			{
				value.Dispose();
			}
			SubscriptionLists.Clear();
		}
	}
}
