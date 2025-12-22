using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.ObservableCollection;

namespace IDS.Portable.LogicalDevice
{
	public class LogicalDeviceProductDtcDataSource : CommonDisposable, IContainerDataSource<ProductDtc>, IContainerDataSourceBase
	{
		public const string LogTag = "LogicalDeviceProductDtcDataSource";

		public const int DtcRefreshIntervalMs = 4000;

		private readonly object _lock = new object();

		private readonly Dictionary<DTC_ID, ProductDtc> _dtcDictionary = new Dictionary<DTC_ID, ProductDtc>();

		private Task<IReadOnlyDictionary<DTC_ID, DtcValue>>? _refreshDtc;

		public DateTime RefreshedDtcTime { get; private set; } = DateTime.MinValue;


		public ILogicalDeviceProduct Product { get; }

		public event ContainerDataSourceNotifyEventHandler? ContainerDataSourceNotifyEvent;

		public LogicalDeviceProductDtcDataSource(ILogicalDeviceProduct product)
		{
			Product = product;
		}

		public void RefreshDtcList(LogicalDeviceDtcFilter dtcFilter)
		{
			if (base.IsDisposed || Product.IsDisposed || DateTime.Now < RefreshedDtcTime + TimeSpan.FromMilliseconds(4000.0))
			{
				return;
			}
			lock (_lock)
			{
				if (_refreshDtc == null || _refreshDtc!.IsCompleted)
				{
					TaggedLog.Debug("LogicalDeviceProductDtcDataSource", string.Format("{0} Refresh DTC for {1}", "RefreshDtcList", this));
					Task<IReadOnlyDictionary<DTC_ID, DtcValue>> refreshDtc = Product.GetProductDtcDictAsync(dtcFilter, CancellationToken.None);
					refreshDtc.ContinueWith(delegate
					{
						UpdateWithDtcList(refreshDtc.Result);
					}, TaskContinuationOptions.OnlyOnRanToCompletion);
					_refreshDtc = refreshDtc;
				}
			}
		}

		private void UpdateWithDtcList(IReadOnlyDictionary<DTC_ID, DtcValue> dtcList)
		{
			bool flag = false;
			lock (_dtcDictionary)
			{
				if (base.IsDisposed || dtcList == null)
				{
					return;
				}
				RefreshedDtcTime = DateTime.Now;
				foreach (KeyValuePair<DTC_ID, DtcValue> dtc in dtcList)
				{
					if (!_dtcDictionary.TryGetValue(dtc.Key, out var value))
					{
						TaggedLog.Debug("LogicalDeviceProductDtcDataSource", string.Format("{0} Updated {1} IsActive={2} Count={3} ADDED", "UpdateWithDtcList", dtc.Key, dtc.Value.IsActive, dtc.Value.PowerCyclesCounter));
						_dtcDictionary[dtc.Key] = new ProductDtc(dtc.Key, dtc.Value);
						flag = true;
					}
					else if (value.IsActive != dtc.Value.IsActive || value.IsStored != dtc.Value.IsStored || value.PowerCyclesCounter != dtc.Value.PowerCyclesCounter)
					{
						TaggedLog.Debug("LogicalDeviceProductDtcDataSource", string.Format("{0} Updated {1} IsActive={2} Count={3} UPDATED", "UpdateWithDtcList", dtc.Key, dtc.Value.IsActive, dtc.Value.PowerCyclesCounter));
						value.UpdateState(dtc.Value);
						flag = true;
					}
				}
			}
			if (flag)
			{
				MainThread.RequestMainThreadAction(delegate
				{
					ContainerDataSourceNotify(this, ContainerDataSourceNotifyRefresh.Default);
				});
			}
		}

		public List<ProductDtc> FindContainerDataMatchingFilter(Func<ProductDtc, bool> filter)
		{
			Func<ProductDtc, bool> filter2 = filter;
			List<ProductDtc> list = new List<ProductDtc>();
			lock (_dtcDictionary)
			{
				list.AddRange(Enumerable.Where(_dtcDictionary.Values, (ProductDtc foundDtc) => filter2 == null || filter2(foundDtc)));
				return list;
			}
		}

		public void ContainerDataSourceNotify(object sender, EventArgs args)
		{
			this.ContainerDataSourceNotifyEvent?.Invoke(sender, args);
		}

		public override void Dispose(bool disposing)
		{
			this.ContainerDataSourceNotifyEvent = null;
			lock (_dtcDictionary)
			{
				foreach (ProductDtc value in _dtcDictionary.Values)
				{
					value.TryDispose();
				}
				_dtcDictionary.Clear();
			}
		}
	}
}
