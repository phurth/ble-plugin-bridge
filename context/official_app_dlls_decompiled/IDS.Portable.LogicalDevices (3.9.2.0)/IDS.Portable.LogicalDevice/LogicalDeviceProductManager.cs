using System;
using System.Collections.Generic;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.ObservableCollection;

namespace IDS.Portable.LogicalDevice
{
	internal class LogicalDeviceProductManager : CommonDisposable, ILogicalDeviceProductManager, ICommonDisposable, IDisposable, IContainerDataSource, IContainerDataSourceBase
	{
		private const string LogTag = "LogicalDeviceProductManager";

		private readonly List<ILogicalDeviceProduct> _productList = new List<ILogicalDeviceProduct>();

		public ILogicalDeviceService DeviceService { get; }

		public event LogicalDeviceProductStatusChangedEventHandler? DeviceProductStatusChanged;

		public event ContainerDataSourceNotifyEventHandler? ContainerDataSourceNotifyEvent;

		public LogicalDeviceProductManager(ILogicalDeviceService deviceService)
		{
			DeviceService = deviceService ?? throw new ArgumentNullException("deviceService");
		}

		public List<TLogicalDevice> FindLogicalDevices<TLogicalDevice>(PRODUCT_ID productId, MAC macAddress) where TLogicalDevice : class, ILogicalDevice
		{
			PRODUCT_ID productId2 = productId;
			MAC macAddress2 = macAddress;
			return DeviceService.DeviceManager?.FindLogicalDevices((TLogicalDevice foundLogicalDevice) => foundLogicalDevice.LogicalId.ProductId == productId2 && foundLogicalDevice.LogicalId.ProductMacAddress?.ToLong() == macAddress2.ToLong()) ?? new List<TLogicalDevice>();
		}

		public ILogicalDeviceProduct? FindProduct(PRODUCT_ID productId, MAC? macAddress)
		{
			if (productId == PRODUCT_ID.UNKNOWN || macAddress == null)
			{
				return null;
			}
			lock (this)
			{
				foreach (ILogicalDeviceProduct product in _productList)
				{
					if (productId == product.ProductId && macAddress.ToLong() == product.MacAddress.ToLong())
					{
						return product;
					}
				}
			}
			return null;
		}

		public List<TLogicalDeviceProduct> FindProducts<TLogicalDeviceProduct>(Func<TLogicalDeviceProduct, bool> filter)
		{
			List<TLogicalDeviceProduct> list = new List<TLogicalDeviceProduct>();
			lock (this)
			{
				foreach (ILogicalDeviceProduct product in _productList)
				{
					if (product is TLogicalDeviceProduct val && (filter == null || filter(val)))
					{
						list.Add(val);
					}
				}
				return list;
			}
		}

		public ILogicalDeviceProduct? AddProduct(PRODUCT_ID productId, MAC macAddress)
		{
			if (productId == PRODUCT_ID.UNKNOWN || macAddress == null)
			{
				TaggedLog.Debug("LogicalDeviceProductManager", $"AddProduct: Unable to add product as productId {productId} or MAC {macAddress} is invalid!");
				return null;
			}
			ILogicalDeviceProduct logicalDeviceProduct = null;
			lock (this)
			{
				logicalDeviceProduct = FindProduct(productId, macAddress);
				if (logicalDeviceProduct == null)
				{
					logicalDeviceProduct = new LogicalDeviceProduct(productId, macAddress, DeviceService);
					_productList.Add(logicalDeviceProduct);
					TaggedLog.Information("LogicalDeviceProductManager", $"AddProduct {logicalDeviceProduct}");
					logicalDeviceProduct.DeviceProductStatusChanged += OnDeviceProductStatusChanged;
					MainThread.RequestMainThreadAction(ContainerDataSourceSync);
					return logicalDeviceProduct;
				}
				return logicalDeviceProduct;
			}
		}

		private void OnDeviceProductStatusChanged(ILogicalDeviceProduct logicalDeviceProduct)
		{
			this.DeviceProductStatusChanged?.Invoke(logicalDeviceProduct);
			MainThread.RequestMainThreadAction(ContainerDataSourceSync);
		}

		private void ContainerDataSourceSync()
		{
			this.ContainerDataSourceNotifyEvent?.Invoke(this, ContainerDataSourceNotifyRefresh.DefaultBatched);
		}

		public void ContainerDataSourceNotify(object sender, EventArgs args)
		{
			this.ContainerDataSourceNotifyEvent?.Invoke(sender, args);
		}

		public List<TDataModel> FindContainerDataMatchingFilter<TDataModel>(Func<TDataModel, bool> filter)
		{
			List<TDataModel> list = new List<TDataModel>();
			lock (this)
			{
				foreach (ILogicalDeviceProduct product in _productList)
				{
					if (product is TDataModel val && (filter == null || filter(val)))
					{
						list.Add(val);
					}
				}
				return list;
			}
		}

		public override void Dispose(bool disposing)
		{
			this.ContainerDataSourceNotifyEvent = null;
			this.DeviceProductStatusChanged = null;
			lock (this)
			{
				foreach (ILogicalDeviceProduct product in _productList)
				{
					try
					{
						product.DeviceProductStatusChanged -= OnDeviceProductStatusChanged;
					}
					catch
					{
					}
					product.TryDispose();
				}
				_productList.Clear();
			}
		}
	}
}
