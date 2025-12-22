using System;
using System.Collections.Generic;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDeviceSourceDirectManager
	{
		IEnumerable<ILogicalDeviceSourceDirect> DeviceSources { get; }

		void SetDeviceSource(ILogicalDeviceSourceDirect? logicalDeviceSource);

		void SetDeviceSourceList(List<ILogicalDeviceSourceDirect>? logicalDeviceSources);

		void ClearDeviceSource();

		TLogicalDeviceSource? GetPrimaryDeviceSource<TLogicalDeviceSource>(ILogicalDevice device) where TLogicalDeviceSource : class, ILogicalDeviceSourceDirect;

		List<TLogicalDeviceSource> FindDeviceSources<TLogicalDeviceSource>(Func<TLogicalDeviceSource, bool>? filter) where TLogicalDeviceSource : ILogicalDeviceSourceDirect;

		TLogicalDeviceSource? FindFirstDeviceSource<TLogicalDeviceSource>(Func<TLogicalDeviceSource, bool>? filter) where TLogicalDeviceSource : class, ILogicalDeviceSourceDirect;

		void ForeachDeviceSource<TLogicalDeviceSource>(Action<TLogicalDeviceSource> action, Func<TLogicalDeviceSource, bool>? filter) where TLogicalDeviceSource : class, ILogicalDeviceSourceDirect;

		void ForeachDeviceSource<TLogicalDeviceSource>(Action<TLogicalDeviceSource> action) where TLogicalDeviceSource : class, ILogicalDeviceSourceDirect;
	}
}
