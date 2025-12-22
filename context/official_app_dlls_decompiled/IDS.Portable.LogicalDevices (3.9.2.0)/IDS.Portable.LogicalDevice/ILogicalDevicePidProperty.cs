using System;
using System.ComponentModel;
using IDS.Core.Types;
using IDS.Portable.Common;

namespace IDS.Portable.LogicalDevice
{
	public interface ILogicalDevicePidProperty : ILogicalDevicePid, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
		UInt48 ValueRaw { get; set; }

		AsyncValueCachedState ValueState { get; }
	}
	public interface ILogicalDevicePidProperty<TValue> : ILogicalDevicePid<TValue>, ILogicalDevicePid, ILogicalDevicePidProperty, ICommonDisposable, IDisposable, INotifyPropertyChanged
	{
	}
}
