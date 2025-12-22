using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace IDS.Portable.Common
{
	public class CollectionTransformer<TSource, TDestination> : CommonDisposable
	{
		private const string LogTag = "CollectionTransformer";

		private readonly Func<TSource, TDestination> _transform;

		private readonly Action? _syncCompleted;

		private readonly ObservableCollection<TSource> _sourceCollection;

		private readonly ICollection<TDestination> _destinationCollection;

		private readonly Dictionary<TSource, TDestination> _conversionDict = new Dictionary<TSource, TDestination>();

		private readonly object lockObject = new object();

		public CollectionTransformer(ObservableCollection<TSource> sourceCollection, ICollection<TDestination> destinationCollection, Func<TSource, TDestination> transform, Action? syncCompleted = null)
		{
			_sourceCollection = sourceCollection;
			_destinationCollection = destinationCollection;
			_transform = transform;
			_syncCompleted = syncCompleted;
			sourceCollection.CollectionChanged += OnSourceCollectionChanged;
			foreach (TSource item in sourceCollection)
			{
				AddToDestination(item);
			}
			_syncCompleted?.Invoke();
		}

		private void AddToDestination(TSource sourceItem)
		{
			lock (lockObject)
			{
				if (!_conversionDict.ContainsKey(sourceItem))
				{
					TDestination val = _transform(sourceItem);
					_conversionDict[sourceItem] = val;
					_destinationCollection.Add(val);
				}
			}
		}

		private void RemoveFromDestination(TSource sourceItem)
		{
			lock (lockObject)
			{
				if (_conversionDict.ContainsKey(sourceItem) && _conversionDict.TryGetValue(sourceItem, out var value))
				{
					_destinationCollection.TryRemove(value);
					_conversionDict.TryRemove(sourceItem);
				}
			}
		}

		private void RemoveAllItems()
		{
			lock (lockObject)
			{
				using ((_destinationCollection as BaseObservableCollection<TDestination>)?.SuppressEvents())
				{
					foreach (TSource item in new List<TSource>(_conversionDict.Keys))
					{
						RemoveFromDestination(item);
					}
				}
			}
		}

		private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
		{
			switch (eventArgs.Action)
			{
			case NotifyCollectionChangedAction.Add:
			case NotifyCollectionChangedAction.Remove:
			case NotifyCollectionChangedAction.Replace:
				lock (lockObject)
				{
					if (eventArgs.OldItems != null)
					{
						foreach (object oldItem in eventArgs.OldItems)
						{
							if (oldItem is TSource sourceItem)
							{
								RemoveFromDestination(sourceItem);
							}
						}
					}
					if (eventArgs.NewItems != null)
					{
						foreach (object newItem in eventArgs.NewItems)
						{
							if (newItem is TSource sourceItem2)
							{
								AddToDestination(sourceItem2);
							}
						}
					}
				}
				_syncCompleted?.Invoke();
				break;
			case NotifyCollectionChangedAction.Reset:
				Sync();
				break;
			case NotifyCollectionChangedAction.Move:
				break;
			}
		}

		public void Sync()
		{
			lock (lockObject)
			{
				try
				{
					HashSet<TSource> hashSet = new HashSet<TSource>(_conversionDict.Keys);
					foreach (TSource item in _sourceCollection)
					{
						if (_conversionDict.TryGetValue(item) == null)
						{
							AddToDestination(item);
						}
						else
						{
							hashSet.TryRemove(item);
						}
					}
					foreach (TSource item2 in hashSet)
					{
						RemoveFromDestination(item2);
					}
				}
				catch (Exception ex)
				{
					TaggedLog.Warning("CollectionTransformer", "Sync failed because {0}", ex.Message);
				}
			}
			_syncCompleted?.Invoke();
		}

		public override void Dispose(bool disposing)
		{
			try
			{
				_sourceCollection.CollectionChanged -= OnSourceCollectionChanged;
			}
			catch
			{
			}
			RemoveAllItems();
			_syncCompleted?.Invoke();
		}
	}
}
