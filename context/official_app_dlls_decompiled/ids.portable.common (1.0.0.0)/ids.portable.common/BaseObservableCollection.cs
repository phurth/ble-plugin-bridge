using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace IDS.Portable.Common
{
	public class BaseObservableCollection<T> : ObservableCollection<T>
	{
		public readonly struct SuppressEventsDisposable : IDisposable
		{
			private readonly BaseObservableCollection<T> _collection;

			private readonly bool _forceRefresh;

			public SuppressEventsDisposable(BaseObservableCollection<T> collection, bool forceRefresh)
			{
				_forceRefresh = forceRefresh;
				_collection = collection;
				collection._suppressEvents++;
			}

			public void Dispose()
			{
				_collection._suppressEvents--;
				if (!_collection.EventsAreSuppressed && (_forceRefresh || _collection._suppressedEventCount != 0L))
				{
					_collection._suppressedEventCount = 0uL;
					_collection.SendOnCollectionChangedReset();
				}
			}
		}

		private const string LogTag = "BaseObservableCollection";

		internal static readonly NotifyCollectionChangedEventArgs ResetEventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

		private int _suppressEvents;

		private ulong _suppressedEventCount;

		public bool EventsAreSuppressed => _suppressEvents > 0;

		public SuppressEventsDisposable SuppressEvents(bool forceRefresh = false)
		{
			return new SuppressEventsDisposable(this, forceRefresh);
		}

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (EventsAreSuppressed)
			{
				_suppressedEventCount++;
				return;
			}
			InvokeOnMainThread(delegate
			{
				base.OnCollectionChanged(e);
			});
		}

		protected void InvokeOnMainThread(Action action)
		{
			MainThread.RequestMainThreadAction(action);
		}

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			InvokeOnMainThread(delegate
			{
				base.OnPropertyChanged(e);
			});
		}

		private void SendOnCollectionChangedReset()
		{
			InvokeOnMainThread(delegate
			{
				base.OnCollectionChanged(ResetEventArgs);
			});
		}

		public void TryRemoveItemLast()
		{
			if (base.Count > 0)
			{
				RemoveAt(base.Count - 1);
			}
		}
	}
}
