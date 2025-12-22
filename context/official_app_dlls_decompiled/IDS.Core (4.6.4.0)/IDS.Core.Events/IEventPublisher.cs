using System;

namespace IDS.Core.Events
{
	public interface IEventPublisher : IDisposable, System.IDisposable
	{
		RoundRobinPublisher CreateRoundRobinPublisher<T>() where T : Event;

		SubscriptionToken Subscribe<T>(Action<T> deliveryAction, SubscriptionType reference) where T : Event;

		void Subscribe<T>(Action<T> deliveryAction, SubscriptionType reference, SubscriptionManager manager) where T : Event;

		bool HasSubscriptionsFor<T>() where T : Event;

		int CountSubscriptionsFor<T>() where T : Event;

		void Publish<T>(T e) where T : Event;

		void Publish(Event e);

		void Publish(Event e, Type eventType);

		void RequestPurge(Type eventType);

		void RequestPurgeAll();
	}
}
