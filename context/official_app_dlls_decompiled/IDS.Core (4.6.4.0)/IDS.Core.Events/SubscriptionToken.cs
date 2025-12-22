using System;

namespace IDS.Core.Events
{
	public sealed class SubscriptionToken : Disposable
	{
		private Subscription mSubscription;

		private readonly object[] DependentObjects;

		internal SubscriptionToken(Subscription subscription, params object[] dependentObjects)
		{
			mSubscription = subscription;
			DependentObjects = dependentObjects;
		}

		public override void Dispose(bool disposing)
		{
			if (disposing)
			{
				mSubscription.Dispose();
				mSubscription = null;
				GC.SuppressFinalize(this);
			}
		}
	}
}
