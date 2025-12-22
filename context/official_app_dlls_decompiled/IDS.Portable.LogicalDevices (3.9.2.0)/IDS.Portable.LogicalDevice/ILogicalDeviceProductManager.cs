using System;
using System.Collections.Generic;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.ObservableCollection;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceProductManager : ICommonDisposable, IDisposable, IContainerDataSource, IContainerDataSourceBase
	{
		event LogicalDeviceProductStatusChangedEventHandler DeviceProductStatusChanged;

		ILogicalDeviceProduct? FindProduct(PRODUCT_ID productId, MAC? macAddress);

		List<TLogicalDeviceProduct> FindProducts<TLogicalDeviceProduct>(Func<TLogicalDeviceProduct, bool> filter);

		ILogicalDeviceProduct? AddProduct(PRODUCT_ID productId, MAC macAddress);

		List<TLogicalDevice> FindLogicalDevices<TLogicalDevice>(PRODUCT_ID productId, MAC macAddress) where TLogicalDevice : class, ILogicalDevice;
	}
}
