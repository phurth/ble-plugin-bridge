using System;

namespace IDS.Portable.Common.ObservableCollection
{
	public class ContainerDataSourceNotifyRefresh : EventArgs, IContainerDataSourceNotifyRefreshBatchable
	{
		public static readonly ContainerDataSourceNotifyRefresh Default = new ContainerDataSourceNotifyRefresh();

		public static readonly ContainerDataSourceNotifyRefresh DefaultBatched = new ContainerDataSourceNotifyRefresh(batchRequest: true);

		public bool IsBatchRequested { get; }

		protected ContainerDataSourceNotifyRefresh(bool batchRequest = false)
		{
			IsBatchRequested = batchRequest;
		}
	}
}
