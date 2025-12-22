using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IDS.Portable.Common.Utils;

namespace IDS.Portable.Common.ObservableCollection
{
	public class CollectionSyncContainer<TCollection, TDataModel, TViewModel> : CommonDisposable where TCollection : ICollection<TViewModel>, new()
	{
		public const int MaxSyncTimeWarningMs = 150;

		private const string LogTag = "CollectionSyncContainer";

		private IContainerDataSourceBase? _dataSource;

		private Func<TDataModel, bool> _dataSourceFilter = (TDataModel datamodel) => true;

		private Func<TDataModel, TViewModel> _viewModelFactory = (TDataModel datamodel) => default(TViewModel);

		private Dictionary<TDataModel, TViewModel> _viewModelDict = new Dictionary<TDataModel, TViewModel>();

		private TCollection _collection;

		protected const int ContainerDataSourceNotifyEventBatchDelayMs = 250;

		protected const int ContainerDataSourceNotifyEventBatchMaxDelayMs = 1000;

		private Watchdog? _containerDataSourceNotifyEventBatchWatchdog;

		protected virtual Func<TDataModel, bool> CurrentDataSourceFilter => _dataSourceFilter;

		protected virtual Func<TDataModel, TViewModel> CurrentViewModelFactory => _viewModelFactory;

		public TCollection Collection => _collection;

		protected virtual bool AutoDataSourceSyncOnConstruction => true;

		protected virtual Watchdog ContainerDataSourceNotifyEventBatchWatchdog
		{
			get
			{
				return _containerDataSourceNotifyEventBatchWatchdog ?? (_containerDataSourceNotifyEventBatchWatchdog = new Watchdog(250, 1000, DataSourceSync, autoStartOnFirstPet: true));
			}
			private set
			{
				_containerDataSourceNotifyEventBatchWatchdog = value;
			}
		}

		public CollectionSyncContainer(TCollection collection, IContainerDataSource<TDataModel> dataSource, Func<TDataModel, TViewModel> viewModelFactory, Func<TDataModel, bool> dataSourceFilter)
			: this(collection, (IContainerDataSourceBase)dataSource, viewModelFactory, dataSourceFilter)
		{
		}

		public CollectionSyncContainer(IContainerDataSource<TDataModel> dataSource, Func<TDataModel, TViewModel> viewModelFactory, Func<TDataModel, bool> dataSourceFilter)
			: this(new TCollection(), dataSource, viewModelFactory, dataSourceFilter)
		{
		}

		public CollectionSyncContainer(TCollection collection, IContainerDataSource dataSource, Func<TDataModel, TViewModel> viewModelFactory, Func<TDataModel, bool> dataSourceFilter)
			: this(collection, (IContainerDataSourceBase)dataSource, viewModelFactory, dataSourceFilter)
		{
		}

		public CollectionSyncContainer(IContainerDataSource dataSource, Func<TDataModel, TViewModel> viewModelFactory, Func<TDataModel, bool> dataSourceFilter)
			: this(new TCollection(), dataSource, viewModelFactory, dataSourceFilter)
		{
		}

		private CollectionSyncContainer(TCollection collection, IContainerDataSourceBase dataSource, Func<TDataModel, TViewModel> viewModelFactory, Func<TDataModel, bool> dataSourceFilter)
		{
			_collection = collection;
			_dataSource = dataSource;
			_viewModelFactory = viewModelFactory;
			_dataSourceFilter = dataSourceFilter;
			dataSource.ContainerDataSourceNotifyEvent += OnContainerDataSourceNotifyEvent;
			if (AutoDataSourceSyncOnConstruction)
			{
				DataSourceSync();
			}
		}

		protected CollectionSyncContainer(TCollection collection, IContainerDataSource dataSource, Func<TDataModel, TViewModel> viewModelFactory)
			: this(collection, (IContainerDataSourceBase)dataSource, viewModelFactory)
		{
		}

		protected CollectionSyncContainer(IContainerDataSource dataSource, Func<TDataModel, TViewModel> viewModelFactory)
			: this(new TCollection(), dataSource, viewModelFactory)
		{
		}

		protected CollectionSyncContainer(TCollection collection, IContainerDataSource<TDataModel> dataSource, Func<TDataModel, TViewModel> viewModelFactory)
			: this(collection, (IContainerDataSourceBase)dataSource, viewModelFactory)
		{
		}

		protected CollectionSyncContainer(IContainerDataSource<TDataModel> dataSource, Func<TDataModel, TViewModel> viewModelFactory)
			: this(new TCollection(), dataSource, viewModelFactory)
		{
		}

		private CollectionSyncContainer(TCollection collection, IContainerDataSourceBase dataSource, Func<TDataModel, TViewModel> viewModelFactory)
		{
			_collection = collection;
			_dataSource = dataSource;
			_viewModelFactory = viewModelFactory;
			dataSource.ContainerDataSourceNotifyEvent += OnContainerDataSourceNotifyEvent;
			if (AutoDataSourceSyncOnConstruction)
			{
				DataSourceSync();
			}
		}

		protected CollectionSyncContainer(TCollection collection, IContainerDataSource dataSource)
			: this(collection, (IContainerDataSourceBase)dataSource)
		{
		}

		protected CollectionSyncContainer(IContainerDataSource dataSource)
			: this(new TCollection(), dataSource)
		{
		}

		protected CollectionSyncContainer(TCollection collection, IContainerDataSource<TDataModel> dataSource)
			: this(collection, (IContainerDataSourceBase)dataSource)
		{
		}

		protected CollectionSyncContainer(IContainerDataSource<TDataModel> dataSource)
			: this(new TCollection(), dataSource)
		{
		}

		private CollectionSyncContainer(TCollection collection, IContainerDataSourceBase dataSource)
		{
			_collection = collection;
			_dataSource = dataSource;
			dataSource.ContainerDataSourceNotifyEvent += OnContainerDataSourceNotifyEvent;
			if (AutoDataSourceSyncOnConstruction)
			{
				DataSourceSync();
			}
		}

		public virtual void OnSyncStart(TCollection collection)
		{
		}

		public virtual void OnSyncEnd(TCollection collection)
		{
		}

		public virtual void OnViewModelAdded(TDataModel dataModel, TViewModel viewModel)
		{
		}

		public virtual void OnViewModelRemoved(TDataModel dataModel, TViewModel viewModel)
		{
		}

		public virtual void OnDataModelAssociated(TDataModel dataModel, TViewModel viewModel)
		{
		}

		public virtual void OnDataModelDissociated(TDataModel dataModel, TViewModel viewModel)
		{
		}

		public void DataSourceSync()
		{
			if (base.IsDisposed)
			{
				ClearAll();
			}
			else
			{
				if (CurrentDataSourceFilter == null)
				{
					return;
				}
				List<TDataModel> list = null;
				IContainerDataSourceBase dataSource = _dataSource;
				if (!(dataSource is IContainerDataSource<TDataModel> containerDataSource))
				{
					if (!(dataSource is IContainerDataSource containerDataSource2))
					{
						TaggedLog.Error("CollectionSyncContainer", "Error: Unexpected _dataSource type '{0}'", _dataSource?.GetType());
						return;
					}
					list = containerDataSource2.FindContainerDataMatchingFilter(CurrentDataSourceFilter);
				}
				else
				{
					list = containerDataSource.FindContainerDataMatchingFilter(CurrentDataSourceFilter);
				}
				DataSourceSync(list);
			}
		}

		public void ClearAll()
		{
			DataSourceSync(new List<TDataModel>());
		}

		private void DataSourceSync(List<TDataModel> dataModelList)
		{
			if (dataModelList == null)
			{
				return;
			}
			MainThread.RequestMainThreadAction(delegate
			{
				PerformanceTimer performanceTimer = new PerformanceTimer("CollectionSyncContainer", GetType().Name + ".DataSourceSync", TimeSpan.FromMilliseconds(150.0), PerformanceTimerOption.AutoStartOnCreate);
				using (new PerformanceTimer("CollectionSyncContainer", GetType().Name + ".OnSyncStart", TimeSpan.FromMilliseconds(150.0), PerformanceTimerOption.AutoStartOnCreate))
				{
					OnSyncStart(_collection);
				}
				using ((_collection as BaseObservableCollection<TViewModel>)?.SuppressEvents())
				{
					List<TDataModel> list = null;
					foreach (TDataModel key in _viewModelDict.Keys)
					{
						if (!dataModelList.Contains(key))
						{
							if (list == null)
							{
								list = new List<TDataModel>();
							}
							list.Add(key);
						}
					}
					if (list != null)
					{
						foreach (TDataModel item in list)
						{
							TViewModel val = _viewModelDict[item];
							_viewModelDict.Remove(item);
							OnDataModelDissociated(item, val);
							if (!_viewModelDict.ContainsValue(val))
							{
								_collection.TryRemove(val);
								OnViewModelRemoved(item, val);
							}
						}
					}
					foreach (TDataModel dataModel in dataModelList)
					{
						if (!_viewModelDict.ContainsKey(dataModel))
						{
							TViewModel val2 = CurrentViewModelFactory(dataModel);
							if (val2 != null)
							{
								_viewModelDict[dataModel] = val2;
								OnDataModelAssociated(dataModel, val2);
								if (!_collection.Contains(val2))
								{
									DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 3);
									defaultInterpolatedStringHandler.AppendFormatted(GetType().Name);
									defaultInterpolatedStringHandler.AppendLiteral(".");
									defaultInterpolatedStringHandler.AppendFormatted("OnViewModelAdded");
									defaultInterpolatedStringHandler.AppendLiteral(".");
									defaultInterpolatedStringHandler.AppendFormatted(dataModel?.GetType().Name);
									using (new PerformanceTimer("CollectionSyncContainer", defaultInterpolatedStringHandler.ToStringAndClear(), TimeSpan.FromMilliseconds(150.0), PerformanceTimerOption.AutoStartOnCreate))
									{
										_collection.Add(val2);
										OnViewModelAdded(dataModel, val2);
									}
								}
							}
						}
					}
					using (new PerformanceTimer("CollectionSyncContainer", GetType().Name + ".OnSyncEnd", TimeSpan.FromMilliseconds(150.0), PerformanceTimerOption.AutoStartOnCreate))
					{
						OnSyncEnd(_collection);
					}
					performanceTimer.TryDispose();
				}
			});
		}

		protected virtual void OnContainerDataSourceNotifyEvent(object sender, EventArgs args)
		{
			if (args is IContainerDataSourceNotifyRefreshBatchable containerDataSourceNotifyRefreshBatchable)
			{
				if (containerDataSourceNotifyRefreshBatchable.IsBatchRequested)
				{
					ContainerDataSourceNotifyEventBatchWatchdog?.TryPet(autoReset: true);
				}
				else
				{
					DataSourceSync();
				}
			}
		}

		public override void Dispose(bool disposing)
		{
			try
			{
				if (_dataSource != null)
				{
					_dataSource!.ContainerDataSourceNotifyEvent -= OnContainerDataSourceNotifyEvent;
				}
			}
			catch
			{
			}
			_containerDataSourceNotifyEventBatchWatchdog?.TryDispose();
			_containerDataSourceNotifyEventBatchWatchdog = null;
			ClearAll();
			_dataSource = null;
		}
	}
}
