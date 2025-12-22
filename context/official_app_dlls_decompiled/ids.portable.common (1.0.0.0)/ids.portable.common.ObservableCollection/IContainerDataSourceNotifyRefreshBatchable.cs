namespace IDS.Portable.Common.ObservableCollection
{
	public interface IContainerDataSourceNotifyRefreshBatchable
	{
		bool IsBatchRequested { get; }
	}
}
