using System;

namespace IDS.Portable.Common.ObservableCollection
{
	public interface IContainerDataSourceBase
	{
		event ContainerDataSourceNotifyEventHandler ContainerDataSourceNotifyEvent;

		void ContainerDataSourceNotify(object sender, EventArgs args);
	}
}
