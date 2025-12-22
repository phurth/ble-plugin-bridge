using System;
using System.Collections.Generic;

namespace IDS.Portable.Common.ObservableCollection
{
	public interface IContainerDataSource : IContainerDataSourceBase
	{
		List<TDataModel> FindContainerDataMatchingFilter<TDataModel>(Func<TDataModel, bool> filter);
	}
	public interface IContainerDataSource<TDataModel> : IContainerDataSourceBase
	{
		List<TDataModel> FindContainerDataMatchingFilter(Func<TDataModel, bool> filter);
	}
}
