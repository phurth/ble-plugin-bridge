using System;

namespace IDS.Core.Events
{
	public abstract class Event
	{
		private SubscriptionList List;

		public object Sender { get; private set; }

		public bool CanSelfPublish => List != null;

		protected Event(object sender)
		{
			if (sender == null)
			{
				throw new ArgumentNullException("sender");
			}
			if (sender is EventPublisher eventPublisher)
			{
				List = eventPublisher.GetValidSubscriptionList(GetType());
			}
			else if (sender is IEventSender eventSender)
			{
				List = (eventSender.Events as EventPublisher)?.GetValidSubscriptionList(GetType());
			}
			else
			{
				List = null;
			}
			Sender = sender;
		}

		public void Publish()
		{
			if (List == null)
			{
				throw new InvalidOperationException(GetType().FullName + " cannot self publish as the sender is not an IEventSender");
			}
			List.Publish(this);
		}
	}
}
